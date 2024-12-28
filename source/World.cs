﻿using Collections;
using System;
using System.Diagnostics;
using Unmanaged;
using Worlds.Unsafe;
using IEnumerableUInt = System.Collections.Generic.IEnumerable<uint>;

namespace Worlds
{
    /// <summary>
    /// Contains arbitrary data sorted into groups of entities for processing.
    /// </summary>
    public unsafe struct World : IDisposable, IEquatable<World>, ISerializable
    {
        internal UnsafeWorld* value;

        /// <summary>
        /// Native address of the world.
        /// </summary>
        public readonly nint Address => (nint)value;

        /// <summary>
        /// Amount of entities that exist in the world.
        /// </summary>
        public readonly uint Count => Slots.Count - Free.Count;

        /// <summary>
        /// The current maximum amount of referrable entities.
        /// <para>Collections of this size + 1 are guaranteed to
        /// be able to store all entity values/positions.</para>
        /// </summary>
        public readonly uint MaxEntityValue => Slots.Count;

        /// <summary>
        /// Checks if the world has been disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// All entity slots in the world.
        /// </summary>
        public readonly List<EntitySlot> Slots => UnsafeWorld.GetEntitySlots(value);

        /// <summary>
        /// All previously used entities that are now free.
        /// </summary>
        public readonly List<uint> Free => UnsafeWorld.GetFreeEntities(value);

        /// <summary>
        /// All component chunks in the world.
        /// </summary>
        public readonly Dictionary<BitSet, ComponentChunk> ComponentChunks => UnsafeWorld.GetComponentChunks(value);

        /// <summary>
        /// The schema containing all component and array types.
        /// </summary>
        public readonly Schema Schema => UnsafeWorld.GetSchema(value);

        /// <summary>
        /// All entities that exist in the world.
        /// </summary>
        public readonly IEnumerableUInt Entities
        {
            get
            {
                List<EntitySlot> slots = Slots;
                List<uint> free = Free;
                for (uint i = 0; i < slots.Count; i++)
                {
                    ref EntitySlot description = ref slots[i];
                    if (!free.Contains(description.entity) && description.entity > 0) //todo: why does this check if its not default?
                    {
                        yield return description.entity;
                    }
                }
            }
        }

        /// <summary>
        /// Indexer for accessing entities by their index.
        /// </summary>
        public readonly uint this[uint index]
        {
            get
            {
                uint i = 0;
                for (uint s = 0; s < Slots.Count; s++)
                {
                    EntitySlot description = Slots[s];
                    if (!Free.Contains(description.entity))
                    {
                        if (i == index)
                        {
                            return description.entity;
                        }

                        i++;
                    }
                }

                throw new IndexOutOfRangeException($"Index {index} is out of range");
            }
        }

#if NET
        /// <summary>
        /// Creates a new disposable world.
        /// </summary>
        public World()
        {
            value = UnsafeWorld.Allocate();
        }
#endif

        /// <summary>
        /// Initializes an existing world from the given address.
        /// </summary>
        public World(nint existingAddress)
        {
            value = (UnsafeWorld*)existingAddress;
        }

        /// <summary>
        /// Initializes an existing world from the given pointer.
        /// </summary>
        public World(UnsafeWorld* value)
        {
            this.value = value;
        }

        /// <summary>
        /// Disposes everything in the world and the world itself.
        /// </summary>
        public void Dispose()
        {
            UnsafeWorld.Free(ref value);
        }

        /// <summary>
        /// Resets the world to default state.
        /// </summary>
        public readonly void Clear()
        {
            UnsafeWorld.ClearEntities(value);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            if (value == default)
            {
                return "World (disposed)";
            }

            return $"World {Address} (count: {Count})";
        }

        /// <summary>
        /// Checks if the given world is equal to this world.
        /// </summary>
        public readonly override bool Equals(object? obj)
        {
            return obj is World world && Equals(world);
        }

        /// <inheritdoc/>
        public readonly bool Equals(World other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }
            else if (IsDisposed != other.IsDisposed)
            {
                return false;
            }

            return Address == other.Address;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            List<EntitySlot> slots = Slots;
            Schema schema = Schema;

            writer.WriteObject(schema);
            writer.WriteValue(Count);
            for (uint s = 0; s < slots.Count; s++)
            {
                EntitySlot slot = slots[s];
                uint entity = slot.entity;
                if (!Free.Contains(entity))
                {
                    writer.WriteValue(entity);
                    writer.WriteValue(slot.parent);

                    //write components
                    ComponentChunk chunk = slot.componentChunk;
                    BitSet componentTypes = chunk.TypesMask;
                    writer.WriteValue(componentTypes.Count); //todo: why not serialize the bitset directly?
                    for (byte c = 0; c < BitSet.Capacity; c++)
                    {
                        if (componentTypes == c)
                        {
                            ComponentType componentType = new(c);
                            ushort componentSize = schema.GetComponentSize(componentType);
                            writer.WriteValue(componentType);
                            void* component = chunk.GetComponent(chunk.Entities.IndexOf(entity), componentType, componentSize);
                            writer.Write(component, componentSize);
                        }
                    }

                    //write arrays
                    writer.WriteValue(slot.arrayTypes.Count);
                    for (byte a = 0; a < BitSet.Capacity; a++)
                    {
                        if (slot.arrayTypes == a)
                        {
                            void* array = slot.arrays[a];
                            uint arrayLength = slot.arrayLengths[a];
                            ArrayType arrayElementType = new(a);
                            writer.WriteValue(arrayElementType);
                            writer.WriteValue(arrayLength);
                            if (arrayLength > 0)
                            {
                                ushort arrayElementSize = schema.GetArrayElementSize(arrayElementType);
                                writer.Write(array, arrayLength * arrayElementSize);
                            }
                        }
                    }

                    //write references
                    writer.WriteValue(slot.referenceCount);
                    for (uint r = 0; r < slot.referenceCount; r++)
                    {
                        uint referencedEntity = slot.references[r];
                        writer.WriteValue(referencedEntity);
                    }
                }
            }
        }

        void ISerializable.Read(BinaryReader reader)
        {
            value = UnsafeWorld.Allocate();
            Schema schema = UnsafeWorld.GetSchema(value);
            List<EntitySlot> slots = UnsafeWorld.GetEntitySlots(value);
            using Schema loadedSchema = reader.ReadObject<Schema>();
            schema.CopyFrom(loadedSchema);

            //create entities and fill them with components and arrays
            uint entityCount = reader.ReadValue<uint>();
            uint currentEntityId = 1;
            using List<uint> temporaryEntities = new(4);
            for (uint e = 0; e < entityCount; e++)
            {
                uint entityId = reader.ReadValue<uint>();
                uint parentId = reader.ReadValue<uint>();

                //skip through the island of free entities
                uint catchup = entityId - currentEntityId;
                for (uint i = 0; i < catchup; i++)
                {
                    uint temporaryEntity = CreateEntity();
                    temporaryEntities.Add(temporaryEntity);
                }

                uint entity = CreateEntity();
                if (parentId != default)
                {
                    ref EntitySlot slot = ref slots[entity - 1];
                    slot.parent = parentId;
                    UnsafeWorld.NotifyParentChange(this, entity, parentId);
                }

                //read components
                byte componentCount = reader.ReadValue<byte>();
                for (byte c = 0; c < componentCount; c++)
                {
                    ComponentType componentType = reader.ReadValue<ComponentType>();
                    ushort componentSize = schema.GetComponentSize(componentType);
                    void* component = UnsafeWorld.AddComponent(value, entity, componentType, componentSize);
                    reader.ReadSpan<byte>(componentSize).CopyTo(component, componentSize);
                    UnsafeWorld.NotifyComponentAdded(this, entity, componentType);
                }

                //read arrays
                byte arrayCount = reader.ReadValue<byte>();
                for (uint a = 0; a < arrayCount; a++)
                {
                    ArrayType arrayElementType = reader.ReadValue<ArrayType>();
                    uint arrayLength = reader.ReadValue<uint>();
                    ushort arrayElementSize = schema.GetArrayElementSize(arrayElementType);
                    uint byteCount = arrayLength * arrayElementSize;
                    void* array = UnsafeWorld.CreateArray(value, entity, arrayElementType, arrayLength);
                    if (arrayLength > 0)
                    {
                        reader.ReadSpan<byte>(byteCount).CopyTo(array, byteCount);
                    }
                }

                //read references
                ushort referenceCount = reader.ReadValue<ushort>();
                for (uint j = 0; j < referenceCount; j++)
                {
                    uint referencedEntity = reader.ReadValue<uint>();
                    AddReference(entity, referencedEntity);
                }

                currentEntityId = entityId + 1;
            }

            //assign children
            foreach (uint entity in Entities)
            {
                uint parent = GetParent(entity);
                if (parent != default)
                {
                    ref EntitySlot parentSlot = ref slots[parent - 1];
                    if (parentSlot.childCount == 0)
                    {
                        parentSlot.children = new(4);
                    }

                    parentSlot.children.Add(entity);
                    parentSlot.childCount++;
                }
            }

            //destroy temporary entities
            for (uint i = 0; i < temporaryEntities.Count; i++)
            {
                UnsafeWorld.DestroyEntity(value, temporaryEntities[i]);
            }
        }

        /// <summary>
        /// Creates new entities with the data from the given world.
        /// </summary>
        public readonly void Append(World sourceWorld)
        {
            uint start = Slots.Count;
            uint entityIndex = 1;
            Schema schema = Schema;
            foreach (EntitySlot sourceSlot in sourceWorld.Slots)
            {
                uint sourceEntity = sourceSlot.entity;
                if (!sourceWorld.Free.Contains(sourceEntity))
                {
                    uint destinationEntity = start + entityIndex;
                    uint destinationParent = start + sourceSlot.parent;
                    InitializeEntity(default, destinationEntity);
                    SetParent(destinationEntity, destinationParent);
                    entityIndex++;

                    //add components
                    ComponentChunk sourceChunk = sourceSlot.componentChunk;
                    BitSet componentTypes = sourceChunk.TypesMask;
                    uint sourceIndex = sourceChunk.Entities.IndexOf(sourceEntity);
                    for (byte c = 0; c < BitSet.Capacity; c++)
                    {
                        if (componentTypes == c)
                        {
                            ComponentType componentType = new(c);
                            ushort componentSize = schema.GetComponentSize(componentType);
                            void* destinationComponent = UnsafeWorld.AddComponent(value, destinationEntity, componentType, componentSize);
                            void* sourceComponent = sourceChunk.GetComponent(sourceIndex, componentType, componentSize);
                            System.Runtime.CompilerServices.Unsafe.CopyBlock(destinationComponent, sourceComponent, componentSize);
                            UnsafeWorld.NotifyComponentAdded(this, destinationEntity, componentType);
                        }
                    }

                    //add arrays
                    for (byte a = 0; a < BitSet.Capacity; a++)
                    {
                        if (sourceSlot.arrayTypes == a)
                        {
                            ArrayType sourceArrayType = new(a);
                            uint sourceArrayLength = sourceSlot.arrayLengths[a];
                            void* sourceArray = sourceSlot.arrays[a];
                            void* destinationArray = UnsafeWorld.CreateArray(value, destinationEntity, sourceArrayType, sourceArrayLength);
                            if (sourceArrayLength > 0)
                            {
                                ushort sourceArrayElementSize = schema.GetArrayElementSize(sourceArrayType);
                                System.Runtime.CompilerServices.Unsafe.CopyBlock(destinationArray, sourceArray, sourceArrayLength * sourceArrayElementSize);
                            }
                        }
                    }
                }
            }

            //assign references last
            entityIndex = 1;
            foreach (EntitySlot sourceSlot in sourceWorld.Slots)
            {
                uint sourceEntity = sourceSlot.entity;
                if (!sourceWorld.Free.Contains(sourceEntity))
                {
                    uint destinationEntity = start + entityIndex;
                    for (uint r = 0; r < sourceSlot.referenceCount; r++)
                    {
                        uint referencedEntity = sourceSlot.references[r];
                        AddReference(destinationEntity, start + referencedEntity);
                    }
                }
            }
        }

        private readonly void Perform(Instruction instruction, List<uint> selection, List<uint> entities)
        {
            Schema schema = Schema;
            if (instruction.type == Instruction.Type.CreateEntity)
            {
                selection.Clear();
                uint createCount = (uint)instruction.A;
                for (uint i = 0; i < createCount; i++)
                {
                    uint newEntity = CreateEntity();
                    selection.Add(newEntity);
                    entities.Add(newEntity);
                }
            }
            else if (instruction.type == Instruction.Type.DestroyEntities)
            {
                uint start = (uint)instruction.A;
                uint count = (uint)instruction.B;
                if (start == 0 && count == 0)
                {
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        uint entity = selection[i];
                        DestroyEntity(entity);
                    }
                }
                else
                {
                    uint end = start + count;
                    for (uint i = start; i < end; i++)
                    {
                        uint entity = entities[i];
                        DestroyEntity(entity);
                    }
                }
            }
            else if (instruction.type == Instruction.Type.ClearSelection)
            {
                selection.Clear();
            }
            else if (instruction.type == Instruction.Type.SelectEntity)
            {
                bool isRelative = instruction.A == 0;
                if (isRelative)
                {
                    uint relativeOffset = (uint)instruction.B;
                    uint entity = entities[entities.Count - 1 - relativeOffset];
                    selection.TryAdd(entity);
                }
                else
                {
                    uint entity = (uint)instruction.B;
                    selection.TryAdd(entity);
                }
            }
            else if (instruction.type == Instruction.Type.SetParent)
            {
                bool isRelative = instruction.A == 0;
                if (isRelative)
                {
                    uint relativeOffset = (uint)instruction.B;
                    uint parent = entities[entities.Count - 1 - relativeOffset];
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        uint entity = selection[i];
                        SetParent(entity, parent);
                    }
                }
                else
                {
                    uint parent = (uint)instruction.B;
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        uint entity = selection[i];
                        SetParent(entity, parent);
                    }
                }
            }
            else if (instruction.type == Instruction.Type.AddReference)
            {
                bool isRelative = instruction.A == 0;
                if (isRelative)
                {
                    uint relativeOffset = (uint)instruction.B;
                    uint referencedEntity = entities[entities.Count - 1 - relativeOffset];
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        uint entity = selection[i];
                        AddReference(entity, referencedEntity);
                    }
                }
                else
                {
                    uint referencedEntity = (uint)instruction.B;
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        uint entity = selection[i];
                        AddReference(entity, referencedEntity);
                    }
                }
            }
            else if (instruction.type == Instruction.Type.RemoveReference)
            {
                rint reference = new((uint)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    RemoveReference(entity, reference);
                }
            }
            else if (instruction.type == Instruction.Type.AddComponent)
            {
                ComponentType componentType = new((byte)instruction.A);
                Allocation allocation = new((void*)(nint)instruction.B);
                ushort componentSize = schema.GetComponentSize(componentType);
                USpan<byte> componentData = allocation.AsSpan<byte>(0, componentSize);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    AddComponent(entity, componentType, componentData);
                }
            }
            else if (instruction.type == Instruction.Type.RemoveComponent)
            {
                ComponentType componentType = new((byte)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    RemoveComponent(entity, componentType);
                }
            }
            else if (instruction.type == Instruction.Type.SetComponent)
            {
                ComponentType componentType = new((byte)instruction.A);
                Allocation allocation = new((void*)(nint)instruction.B);
                ushort componentSize = schema.GetComponentSize(componentType);
                USpan<byte> componentBytes = allocation.AsSpan<byte>(0, componentSize);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    SetComponent(entity, componentType, componentBytes);
                }
            }
            else if (instruction.type == Instruction.Type.CreateArray)
            {
                ArrayType arrayType = new((byte)instruction.A);
                ushort arrayElementSize = schema.GetArrayElementSize(arrayType);
                Allocation allocation = new((void*)(nint)instruction.B);
                uint count = (uint)instruction.C;
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    Allocation newArray = CreateArray(entity, arrayType, count);
                    allocation.CopyTo(newArray, 0, 0, count * arrayElementSize);
                }
            }
            else if (instruction.type == Instruction.Type.DestroyArray)
            {
                ArrayType arrayType = new((byte)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    DestroyArray(entity, arrayType);
                }
            }
            else if (instruction.type == Instruction.Type.SetArrayElement)
            {
                ArrayType arrayType = new((byte)instruction.A);
                ushort arrayElementSize = schema.GetArrayElementSize(arrayType);
                Allocation allocation = new((void*)(nint)instruction.B);
                uint elementCount = allocation.Read<uint>();
                uint start = (uint)instruction.C;
                void* source = (void*)(allocation + sizeof(uint));
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    void* array = UnsafeWorld.GetArray(value, entity, arrayType, out uint entityArrayLength);
                    void* arrayStart = (void*)((nint)array + arrayElementSize * start);
                    System.Runtime.CompilerServices.Unsafe.CopyBlock(arrayStart, source, elementCount * arrayElementSize);
                }
            }
            else if (instruction.type == Instruction.Type.ResizeArray)
            {
                ArrayType arrayType = new((byte)instruction.A);
                uint newLength = (uint)instruction.B;
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    UnsafeWorld.ResizeArray(value, entity, arrayType, newLength);
                }
            }
            else
            {
                throw new NotImplementedException($"Unknown instruction type `{instruction.type}`");
            }
        }

        /// <summary>
        /// Performs all given <paramref name="instructions"/>.
        /// </summary>
        public readonly void Perform(USpan<Instruction> instructions)
        {
            using List<uint> selection = new(4);
            using List<uint> entities = new(4);
            foreach (Instruction instruction in instructions)
            {
                Perform(instruction, selection, entities);
            }
        }

        /// <summary>
        /// Performs all given <paramref name="instructions"/>.
        /// </summary>
        public readonly void Perform(List<Instruction> instructions)
        {
            Perform(instructions.AsSpan());
        }

        /// <summary>
        /// Performs all given <paramref name="instructions"/>.
        /// </summary>
        public readonly void Perform(Array<Instruction> instructions)
        {
            Perform(instructions.AsSpan());
        }

        /// <summary>
        /// Performs all instructions in the given <paramref name="operation"/>.
        /// </summary>
        public readonly void Perform(Operation operation)
        {
            using List<uint> selection = new(4);
            using List<uint> entities = new(4);
            uint length = operation.Count;
            for (uint i = 0; i < length; i++)
            {
                Instruction instruction = operation[i];
                Perform(instruction, selection, entities);
            }
        }

        /// <summary>
        /// Destroys the given <paramref name="entity"/> assuming it exists.
        /// </summary>
        public readonly void DestroyEntity(uint entity, bool destroyChildren = true)
        {
            UnsafeWorld.DestroyEntity(value, entity, destroyChildren);
        }

        /// <summary>
        /// Copies component types from the given <paramref name="entity"/> to the destination <paramref name="buffer"/>.
        /// </summary>
        public readonly byte CopyComponentTypesTo(uint entity, USpan<ComponentType> buffer)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            ComponentChunk chunk = slot.componentChunk;
            return chunk.CopyTypesTo(buffer);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> is enabled with respect
        /// to ancestor entities.
        /// </summary>
        public readonly bool IsEnabled(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            return slot.state == EntitySlot.State.Enabled;
        }

        /// <summary>
        /// Checks if the given entity is enabled regardless
        /// of the entity hierarchy.
        /// </summary>
        public readonly bool IsSelfEnabled(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            return slot.state == EntitySlot.State.Enabled || slot.state == EntitySlot.State.EnabledButDisabledDueToAncestor;
        }

        /// <summary>
        /// Assigns the enabled state of the given <paramref name="entity"/>.
        /// </summary>
        public readonly void SetEnabled(uint entity, bool state)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            if (slot.parent != default)
            {
                EntitySlot.State parentState = Slots[slot.parent - 1].state;
                if (parentState == EntitySlot.State.Disabled || parentState == EntitySlot.State.EnabledButDisabledDueToAncestor)
                {
                    slot.state = state ? EntitySlot.State.EnabledButDisabledDueToAncestor : EntitySlot.State.Disabled;
                }
                else
                {
                    slot.state = state ? EntitySlot.State.Enabled : EntitySlot.State.Disabled;
                }
            }
            else
            {
                slot.state = state ? EntitySlot.State.Enabled : EntitySlot.State.Disabled;
            }

            for (uint i = 0; i < slot.childCount; i++)
            {
                uint child = slot.children[i];
                SetEnabled(child, state);
            }
        }

        /// <summary>
        /// Retrieves the first entity that complies with type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool TryGetFirst<T>(out T entity, bool onlyEnabled = false) where T : unmanaged, IEntity
        {
            EntityExtensions.ThrowIfTypeLayoutMismatches<T>();

            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            Definition definition = default(T).GetDefinition(Schema);
            if (definition.ArrayTypesMask != default(BitSet))
            {
                if (onlyEnabled)
                {
                    foreach (BitSet key in chunks.Keys)
                    {
                        if ((key & definition.ComponentTypesMask) == definition.ComponentTypesMask)
                        {
                            ComponentChunk chunk = chunks[key];
                            for (uint e = 0; e < chunk.Entities.Count; e++)
                            {
                                uint entityValue = chunk.Entities[e];
                                BitSet arrayTypes = GetArrayTypesMask(entityValue);
                                if ((definition.ArrayTypesMask & arrayTypes) == arrayTypes)
                                {
                                    if (IsEnabled(entityValue))
                                    {
                                        entity = new Entity(this, entityValue).As<T>();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (BitSet key in chunks.Keys)
                    {
                        if ((key & definition.ComponentTypesMask) == definition.ComponentTypesMask)
                        {
                            ComponentChunk chunk = chunks[key];
                            for (uint e = 0; e < chunk.Entities.Count; e++)
                            {
                                uint entityValue = chunk.Entities[e];
                                BitSet arrayTypes = GetArrayTypesMask(entityValue);
                                if ((definition.ArrayTypesMask & arrayTypes) == arrayTypes)
                                {
                                    entity = new Entity(this, entityValue).As<T>();
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (!onlyEnabled)
                {
                    foreach (BitSet key in chunks.Keys)
                    {
                        ComponentChunk chunk = chunks[key];
                        if (chunk.Entities.Count > 0 && (key & definition.ComponentTypesMask) == definition.ComponentTypesMask)
                        {
                            uint entityValue = chunk.Entities[0];
                            entity = new Entity(this, entityValue).As<T>();
                            return true;
                        }
                    }
                }
                else
                {
                    foreach (BitSet key in chunks.Keys)
                    {
                        if ((key & definition.ComponentTypesMask) == definition.ComponentTypesMask)
                        {
                            ComponentChunk chunk = chunks[key];
                            for (uint e = 0; e < chunk.Entities.Count; e++)
                            {
                                uint entityValue = chunk.Entities[e];
                                if (IsEnabled(entityValue))
                                {
                                    entity = new Entity(this, entityValue).As<T>();
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            entity = default;
            return false;
        }

        /// <summary>
        /// Retrieves the first entity that complies with the type.
        /// </summary>
        public readonly T GetFirst<T>(bool onlyEnabled = false) where T : unmanaged, IEntity
        {
            ThrowIfEntityDoesntExist<T>(onlyEnabled);

            TryGetFirst(out T entity, onlyEnabled);
            return entity;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfEntityDoesntExist<T>(bool onlyEnabled) where T : unmanaged, IEntity
        {
            if (!TryGetFirst(out T _, onlyEnabled))
            {
                throw new NullReferenceException($"No entity of type `{typeof(T)}` exists");
            }
        }

        /// <summary>
        /// Attempts to retrieve the first found component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T TryGetFirstComponent<T>(out bool contains) where T : unmanaged
        {
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            foreach (BitSet key in chunks.Keys)
            {
                if (key == componentType)
                {
                    ComponentChunk chunk = chunks[key];
                    if (chunk.Count > 0)
                    {
                        contains = true;
                        return ref chunk.GetComponent<T>(0, componentType);
                    }
                }
            }

            contains = false;
            return ref *(T*)default(nint);
        }

        /// <summary>
        /// Attempts to retrieve the first entity found with a component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool TryGetFirstEntityContainingComponent<T>(out uint entity) where T : unmanaged
        {
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            ComponentType type = Schema.GetComponent<T>();
            foreach (BitSet key in chunks.Keys)
            {
                if (key == type)
                {
                    ComponentChunk chunk = chunks[key];
                    if (chunk.Count > 0)
                    {
                        entity = chunk.Entities[0];
                        return true;
                    }
                }
            }

            entity = default;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the first found entity and component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T TryGetFirstEntityContainingComponent<T>(out uint entity, out bool contains) where T : unmanaged
        {
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            foreach (BitSet key in chunks.Keys)
            {
                if (key == componentType)
                {
                    ComponentChunk chunk = chunks[key];
                    if (chunk.Count > 0)
                    {
                        entity = chunk.Entities[0];
                        contains = true;
                        return ref chunk.GetComponent<T>(0, componentType);
                    }
                }
            }

            entity = default;
            contains = false;
            return ref *(T*)default(nint);
        }

        /// <summary>
        /// Retrieves the first found entity and component of type <typeparamref name="T"/>.
        /// <para>
        /// May throw a <see cref="NullReferenceException"/> if no entity with the component is found.
        /// </para>
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>"
        public readonly ref T GetFirstComponent<T>() where T : unmanaged
        {
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            foreach (BitSet key in chunks.Keys)
            {
                if (key == componentType)
                {
                    ComponentChunk chunk = chunks[key];
                    if (chunk.Count > 0)
                    {
                        return ref chunk.GetComponent<T>(0, componentType);
                    }
                }
            }

            throw new NullReferenceException($"No entity with component of type `{typeof(T)}` was found");
        }

        /// <summary>
        /// Retrieves the first found entity and component of type <typeparamref name="T"/>.
        /// <para>
        /// May throw a <see cref="NullReferenceException"/> if no entity with the component is found.
        /// </para>
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public readonly ref T GetFirstEntityContainingComponent<T>(out uint entity) where T : unmanaged
        {
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            foreach (BitSet key in chunks.Keys)
            {
                if (key == componentType)
                {
                    ComponentChunk chunk = chunks[key];
                    if (chunk.Count > 0)
                    {
                        entity = chunk.Entities[0];
                        return ref chunk.GetComponent<T>(0, componentType);
                    }
                }
            }

            throw new NullReferenceException($"No entity with component of type `{typeof(T)}` found");
        }

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        public readonly uint CreateEntity()
        {
            return CreateEntity(default);
        }

        /// <summary>
        /// Creates a new entity with the given <paramref name="definition"/>.
        /// </summary>
        public readonly uint CreateEntity(Definition definition)
        {
            uint entity = GetNextEntity();
            InitializeEntity(definition, entity);
            return entity;
        }

        /// <summary>
        /// Creates a new entity with the given <typeparamref name="T1"/> component.
        /// </summary>
        public readonly uint CreateEntity<T1>(T1 component1) where T1 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            return entity;
        }

        /// <summary>
        /// Creates a new entity with the given <typeparamref name="T1"/> and <typeparamref name="T2"/> components.
        /// </summary>
        public readonly uint CreateEntity<T1, T2>(T1 component1, T2 component2) where T1 : unmanaged where T2 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            return entity;
        }

        /// <summary>
        /// Creates a new entity with the given <typeparamref name="T1"/>, <typeparamref name="T2"/> and <typeparamref name="T3"/> components.
        /// </summary>
        public readonly uint CreateEntity<T1, T2, T3>(T1 component1, T2 component2, T3 component3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            return entity;
        }

        /// <summary>
        /// Creates a new entity with the given <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/> and <typeparamref name="T4"/> components.
        /// </summary>
        public readonly uint CreateEntity<T1, T2, T3, T4>(T1 component1, T2 component2, T3 component3, T4 component4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            return entity;
        }

        /// <summary>
        /// Creates a new entity with the given <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/> and <typeparamref name="T5"/> components.
        /// </summary>
        public readonly uint CreateEntity<T1, T2, T3, T4, T5>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            return entity;
        }

        /// <summary>
        /// Creates a new entity with the given <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/> and <typeparamref name="T6"/> components.
        /// </summary>
        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5, T6>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6, T7>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5, T6, T7>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            SetComponent(entity, component7);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5, T6, T7, T8>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            SetComponent(entity, component7);
            SetComponent(entity, component8);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            SetComponent(entity, component7);
            SetComponent(entity, component8);
            SetComponent(entity, component9);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            SetComponent(entity, component7);
            SetComponent(entity, component8);
            SetComponent(entity, component9);
            SetComponent(entity, component10);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            SetComponent(entity, component7);
            SetComponent(entity, component8);
            SetComponent(entity, component9);
            SetComponent(entity, component10);
            SetComponent(entity, component11);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            SetComponent(entity, component7);
            SetComponent(entity, component8);
            SetComponent(entity, component9);
            SetComponent(entity, component10);
            SetComponent(entity, component11);
            SetComponent(entity, component12);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            SetComponent(entity, component7);
            SetComponent(entity, component8);
            SetComponent(entity, component9);
            SetComponent(entity, component10);
            SetComponent(entity, component11);
            SetComponent(entity, component12);
            SetComponent(entity, component13);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            SetComponent(entity, component7);
            SetComponent(entity, component8);
            SetComponent(entity, component9);
            SetComponent(entity, component10);
            SetComponent(entity, component11);
            SetComponent(entity, component12);
            SetComponent(entity, component13);
            SetComponent(entity, component14);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            SetComponent(entity, component7);
            SetComponent(entity, component8);
            SetComponent(entity, component9);
            SetComponent(entity, component10);
            SetComponent(entity, component11);
            SetComponent(entity, component12);
            SetComponent(entity, component13);
            SetComponent(entity, component14);
            SetComponent(entity, component15);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15, T16 component16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new(Schema.GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(), default);
            InitializeEntity(definition, entity);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            SetComponent(entity, component7);
            SetComponent(entity, component8);
            SetComponent(entity, component9);
            SetComponent(entity, component10);
            SetComponent(entity, component11);
            SetComponent(entity, component12);
            SetComponent(entity, component13);
            SetComponent(entity, component14);
            SetComponent(entity, component15);
            SetComponent(entity, component16);
            return entity;
        }

        /// <summary>
        /// Creates entities to fill the given <paramref name="buffer"/>.
        /// </summary>
        public readonly void CreateEntities(USpan<uint> buffer)
        {
            for (uint i = 0; i < buffer.Length; i++)
            {
                buffer[i] = CreateEntity();
            }
        }

        /// <summary>
        /// Returns the value for the next created entity.
        /// </summary>
        public readonly uint GetNextEntity()
        {
            return UnsafeWorld.GetNextEntity(value);
        }

        /// <summary>
        /// Creates an entity with the given value assuming its 
        /// not already in use.
        /// <para>
        /// May throw <see cref="Exception"/> if the entity is already in use.
        /// </para>
        /// </summary>
        public readonly void InitializeEntity(Definition definition, uint newEntity)
        {
            UnsafeWorld.InitializeEntity(value, definition, newEntity);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> exists and is valid in this world.
        /// </summary>
        public readonly bool ContainsEntity(uint entity)
        {
            return UnsafeWorld.ContainsEntity(value, entity);
        }

        /// <summary>
        /// Checks if the <paramref name="entity"/> exists and is valid in this world.
        /// </summary>
        public readonly bool ContainsEntity<T>(T entity) where T : unmanaged, IEntity
        {
            return ContainsEntity(entity.Value);
        }

        /// <summary>
        /// Retrieves the parent of the given entity, <c>default</c> if none
        /// is assigned.
        /// </summary>
        public readonly uint GetParent(uint entity)
        {
            return UnsafeWorld.GetParent(value, entity);
        }

        /// <summary>
        /// Assigns the given <paramref name="parent"/> entity to the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><c>true</c> if the given parent entity was found and assigned successfuly.</returns>
        public readonly bool SetParent(uint entity, uint parent)
        {
            return UnsafeWorld.SetParent(value, entity, parent);
        }

        /// <summary>
        /// Retreives all children of the <paramref name="entity"/> entity.
        /// </summary>
        public readonly USpan<uint> GetChildren(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            if (slot.childCount > 0)
            {
                return slot.children.AsSpan<uint>();
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Retrieves the number of children the given <paramref name="entity"/> has.
        /// </summary>
        public readonly uint GetChildCount(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            return Slots[entity - 1].childCount;
        }

        /// <summary>
        /// Adds a new reference to the given entity.
        /// </summary>
        /// <returns>An index offset by 1 that refers to this entity.</returns>
        public readonly rint AddReference(uint entity, uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            if (slot.referenceCount == 0)
            {
                slot.references = new(4);
            }

            slot.references.Add(referencedEntity);
            slot.referenceCount++;
            return new(slot.referenceCount);
        }

        /// <summary>
        /// Adds a new reference to the given entity.
        /// </summary>
        public readonly rint AddReference<T>(uint entity, T referencedEntity) where T : unmanaged, IEntity
        {
            return AddReference(entity, referencedEntity.Value);
        }

        /// <summary>
        /// Updates an existing reference to point towards a different entity.
        /// </summary>
        public readonly void SetReference(uint entity, rint reference, uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            UnsafeWorld.ThrowIfReferenceIsMissing(value, entity, reference);

            ref EntitySlot slot = ref Slots[entity - 1];
            slot.references[reference.value - 1] = referencedEntity;
        }

        /// <summary>
        /// Assigns a new entity to an existing reference.
        /// </summary>
        public readonly void SetReference<T>(uint entity, rint reference, T referencedEntity) where T : unmanaged, IEntity
        {
            SetReference(entity, reference, referencedEntity.Value);
        }

        /// <summary>
        /// Checks if the given entity contains a reference to the given referenced entity.
        /// </summary>
        public readonly bool ContainsReference(uint entity, uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            return slot.referenceCount > 0 && slot.references.Contains(referencedEntity);
        }

        /// <summary>
        /// Checks if the given entity contains a reference to the given referenced entity.
        /// </summary>
        public readonly bool ContainsReference<T>(uint entity, T referencedEntity) where T : unmanaged, IEntity
        {
            return ContainsReference(entity, referencedEntity.Value);
        }

        /// <summary>
        /// Checks if the given entity contains the given local <paramref name="reference"/>.
        /// </summary>
        public readonly bool ContainsReference(uint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            return reference.value > 0 && reference.value <= slot.referenceCount;
        }

        /// <summary>
        /// Retrieves the number of references the given <paramref name="entity"/> has.
        /// </summary>
        public readonly uint GetReferenceCount(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            return slot.referenceCount;
        }

        /// <summary>
        /// Retrieves the referenced entity at the given <paramref name="reference"/> index on <paramref name="entity"/>.
        /// </summary>
        public readonly uint GetReference(uint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            UnsafeWorld.ThrowIfReferenceIsMissing(value, entity, reference);

            ref EntitySlot slot = ref Slots[entity - 1];
            return slot.references[reference.value - 1];
        }

        /// <summary>
        /// Retrieves the local reference that points to the given <paramref name="referencedEntity"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly rint GetReference(uint entity, uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            UnsafeWorld.ThrowIfReferenceIsMissing(value, entity, referencedEntity);

            ref EntitySlot slot = ref Slots[entity - 1];
            uint index = slot.references.IndexOf(referencedEntity);
            return new(index + 1);
        }

        /// <summary>
        /// Attempts to retrieve the referenced entity at the given <paramref name="position"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetReference(uint entity, rint position, out uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            uint index = position.value - 1;
            if (index < slot.referenceCount)
            {
                referencedEntity = slot.references[index];
                return true;
            }
            else
            {
                referencedEntity = default;
                return false;
            }
        }

        /// <summary>
        /// Removes the reference at the given <paramref name="reference"/> index on <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveReference(uint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            UnsafeWorld.ThrowIfReferenceIsMissing(value, entity, reference);

            ref EntitySlot slot = ref Slots[entity - 1];
            slot.references.RemoveAt(reference.value - 1);
            slot.referenceCount--;

            if (slot.referenceCount == 0)
            {
                slot.references.Dispose();
            }
        }

        /// <summary>
        /// Retrieves the types of all arrays on this entity.
        /// </summary>
        public readonly byte CopyArrayTypesTo(uint entity, USpan<ArrayType> buffer)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            byte count = 0;
            for (byte a = 0; a < BitSet.Capacity; a++)
            {
                if (slot.arrayTypes == a)
                {
                    buffer[count++] = new(a);
                }
            }

            return count;
        }

        /// <summary>
        /// Retrieves the types of all arrays on this entity.
        /// </summary>
        public readonly BitSet GetArrayTypesMask(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            return slot.arrayTypes;
        }

        /// <summary>
        /// Creates a new uninitialized array with the given <paramref name="length"/> and <paramref name="arrayType"/>.
        /// </summary>
        public readonly Allocation CreateArray(uint entity, ArrayType arrayType, uint length = 0)
        {
            return new(UnsafeWorld.CreateArray(value, entity, arrayType, length));
        }

        /// <summary>
        /// Creates a new uninitialized array on this <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<T> CreateArray<T>(uint entity, uint length = 0) where T : unmanaged
        {
            ArrayType arrayElementType = Schema.GetArrayElement<T>();
            Allocation array = CreateArray(entity, arrayElementType, length);
            return array.AsSpan<T>(0, length);
        }

        /// <summary>
        /// Creates a new array containing the given span.
        /// </summary>
        public readonly void CreateArray<T>(uint entity, USpan<T> values) where T : unmanaged
        {
            USpan<T> array = CreateArray<T>(entity, values.Length);
            values.CopyTo(array);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsArray<T>(uint entity) where T : unmanaged
        {
            ArrayType arrayElementType = Schema.GetArrayElement<T>();
            return ContainsArray(entity, arrayElementType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of the given <paramref name="arrayElementType"/>.
        /// </summary>
        public readonly bool ContainsArray(uint entity, ArrayType arrayElementType)
        {
            return UnsafeWorld.ContainsArray(value, entity, arrayElementType);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<T> GetArray<T>(uint entity) where T : unmanaged
        {
            ArrayType arrayElementType = Schema.GetArrayElement<T>();
            void* array = UnsafeWorld.GetArray(value, entity, arrayElementType, out uint length);
            return new USpan<T>(array, length);
        }

        /// <summary>
        /// Retrieves the array of the given <paramref name="arrayElementType"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Allocation GetArray(uint entity, ArrayType arrayElementType, out uint length)
        {
            return new(UnsafeWorld.GetArray(value, entity, arrayElementType, out length));
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<T> ResizeArray<T>(uint entity, uint newLength) where T : unmanaged
        {
            ArrayType arrayElementType = Schema.GetArrayElement<T>();
            void* array = UnsafeWorld.ResizeArray(value, entity, arrayElementType, newLength);
            return new USpan<T>(array, newLength);
        }

        /// <summary>
        /// Resizes the array of type <paramref name="arrayType"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly Allocation ResizeArray(uint entity, ArrayType arrayType, uint newLength)
        {
            return new(UnsafeWorld.ResizeArray(value, entity, arrayType, newLength));
        }

        /// <summary>
        /// Attempts to retrieve an array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetArray<T>(uint entity, out USpan<T> array) where T : unmanaged
        {
            if (ContainsArray<T>(entity))
            {
                array = GetArray<T>(entity);
                return true;
            }
            else
            {
                array = default;
                return false;
            }
        }

        /// <summary>
        /// Retrieves the element at the index from an existing array on this entity.
        /// </summary>
        public readonly ref T GetArrayElement<T>(uint entity, uint index) where T : unmanaged
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ArrayType arrayElementType = Schema.GetArrayElement<T>();
            UnsafeWorld.ThrowIfArrayIsMissing(value, entity, arrayElementType);
            void* array = UnsafeWorld.GetArray(value, entity, arrayElementType, out uint arrayLength);
            void* element = (void*)((nint)array + index * sizeof(T));
            return ref *(T*)element;
        }

        /// <summary>
        /// Retrieves the length of an existing array on this entity.
        /// </summary>
        public readonly uint GetArrayLength<T>(uint entity) where T : unmanaged
        {
            ArrayType arrayElementType = Schema.GetArrayElement<T>();
            return UnsafeWorld.GetArrayLength(value, entity, arrayElementType);
        }

        /// <summary>
        /// Destroys the array of type <typeparamref name="T"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly void DestroyArray<T>(uint entity) where T : unmanaged
        {
            ArrayType arrayElementType = Schema.GetArrayElement<T>();
            DestroyArray(entity, arrayElementType);
        }

        /// <summary>
        /// Destroys the array of the given <paramref name="arrayType"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly void DestroyArray(uint entity, ArrayType arrayType)
        {
            UnsafeWorld.DestroyArray(value, entity, arrayType);
        }

        /// <summary>
        /// Adds a new <typeparamref name="T"/> component to the given <paramref name="entity"/>.
        /// </summary>
        public readonly ref T AddComponent<T>(uint entity, T component) where T : unmanaged
        {
            ComponentType componentType = Schema.GetComponent<T>();
            ushort componentSize = (ushort)sizeof(T);
            void* destination = UnsafeWorld.AddComponent(value, entity, componentType, componentSize);
            System.Runtime.CompilerServices.Unsafe.CopyBlock(destination, &component, componentSize);
            UnsafeWorld.NotifyComponentAdded(this, entity, componentType);
            return ref *(T*)destination;
        }

        /// <summary>
        /// Adds a new default component with the given type.
        /// </summary>
        public readonly void AddComponent(uint entity, ComponentType componentType)
        {
            ushort componentSize = Schema.GetComponentSize(componentType);
            UnsafeWorld.AddComponent(value, entity, componentType, componentSize);
            UnsafeWorld.NotifyComponentAdded(this, entity, componentType);
        }

        /// <summary>
        /// Adds a new component of the given <paramref name="componentType"/> with <paramref name="source"/> bytes
        /// to <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponent(uint entity, ComponentType componentType, USpan<byte> source)
        {
            ushort componentSize = Schema.GetComponentSize(componentType);
            void* component = UnsafeWorld.AddComponent(value, entity, componentType, componentSize);
            source.CopyTo(component, source.Length);
            UnsafeWorld.NotifyComponentAdded(this, entity, componentType);
        }

        /// <summary>
        /// Adds a <c>default</c> component to <paramref name="entity"/> and returns it by reference.
        /// </summary>
        public readonly ref T AddComponent<T>(uint entity) where T : unmanaged
        {
            ComponentType componentType = Schema.GetComponent<T>();
            ushort componentSize = (ushort)sizeof(T);
            void* destination = UnsafeWorld.AddComponent(value, entity, componentType, componentSize);
            UnsafeWorld.NotifyComponentAdded(this, entity, componentType);
            return ref *(T*)destination;
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveComponent<T>(uint entity) where T : unmanaged
        {
            UnsafeWorld.RemoveComponent<T>(value, entity);
        }

        /// <summary>
        /// Removes the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveComponent(uint entity, ComponentType componentType)
        {
            UnsafeWorld.RemoveComponent(value, entity, componentType);
        }

        /// <summary>
        /// Checks if any entity in this world contains a component
        /// of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsAnyComponent<T>() where T : unmanaged
        {
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            ComponentType type = Schema.GetComponent<T>();
            foreach (BitSet key in chunks.Keys)
            {
                if (key == type)
                {
                    ComponentChunk chunk = chunks[key];
                    if (chunk.Entities.Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains a component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsComponent<T>(uint entity) where T : unmanaged
        {
            return ContainsComponent(entity, Schema.GetComponent<T>());
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains a component of <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(uint entity, ComponentType componentType)
        {
            return UnsafeWorld.ContainsComponent(value, entity, componentType);
        }

        /// <summary>
        /// Retrieves a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint entity) where T : unmanaged
        {
            ComponentType componentType = Schema.GetComponent<T>();
            ushort componentSize = Schema.GetComponentSize(componentType);
            void* component = UnsafeWorld.GetComponent(value, entity, componentType, componentSize);
            return ref *(T*)component;
        }

        /// <summary>
        /// Returns the component of the expected type if it exists, otherwise the given default
        /// value is used.
        /// </summary>
        public readonly T GetComponent<T>(uint entity, T defaultValue) where T : unmanaged
        {
            if (ContainsComponent<T>(entity))
            {
                return GetComponent<T>(entity);
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Retrieves the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>
        /// as a pointer.
        /// </summary>
        public readonly void* GetComponent(uint entity, ComponentType componentType)
        {
            ushort componentSize = Schema.GetComponentSize(componentType);
            return UnsafeWorld.GetComponent(value, entity, componentType, componentSize);
        }

        /// <summary>
        /// Fetches the component from this entity as a span of bytes.
        /// </summary>
        public readonly USpan<byte> GetComponentBytes(uint entity, ComponentType componentType)
        {
            ushort componentSize = Schema.GetComponentSize(componentType);
            void* component = UnsafeWorld.GetComponent(value, entity, componentType, componentSize);
            return new(component, componentSize);
        }

        /// <summary>
        /// Attempts to retrieve a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><c>true</c> if the component is found.</returns>
        public readonly ref T TryGetComponent<T>(uint entity, out bool contains) where T : unmanaged
        {
            ref EntitySlot slot = ref Slots[entity - 1];
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            ComponentChunk chunk = slot.componentChunk;
            contains = chunk.TypesMask == componentType;
            if (contains)
            {
                uint index = chunk.Entities.IndexOf(entity);
                return ref chunk.GetComponent<T>(index, componentType);
            }
            else
            {
                return ref *(T*)default(nint);
            }
        }

        /// <summary>
        /// Attempts to retrieve the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryGetComponent<T>(uint entity, out T component) where T : unmanaged
        {
            ref EntitySlot slot = ref Slots[entity - 1];
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            ComponentChunk chunk = slot.componentChunk;
            bool contains = chunk.TypesMask == componentType;
            if (contains)
            {
                uint index = chunk.Entities.IndexOf(entity);
                component = chunk.GetComponent<T>(index, componentType);
            }
            else
            {
                component = default;
            }

            return contains;
        }

        /// <summary>
        /// Assigns the given <paramref name="component"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void SetComponent<T>(uint entity, T component) where T : unmanaged
        {
            ref T existing = ref GetComponent<T>(entity);
            existing = component;
        }

        /// <summary>
        /// Assigns the given <paramref name="componentData"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void SetComponent(uint entity, ComponentType componentType, USpan<byte> componentData)
        {
            USpan<byte> bytes = GetComponentBytes(entity, componentType);
            componentData.CopyTo(bytes);
        }

        /// <summary>
        /// Returns the main component chunk that contains the given entity.
        /// </summary>
        public readonly ComponentChunk GetComponentChunk(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);

            ref EntitySlot slot = ref Slots[entity - 1];
            return slot.componentChunk;
        }

        /// <summary>
        /// Returns the main component chunk that contains all of the given component types.
        /// </summary>
        public readonly ComponentChunk GetComponentChunk(USpan<ComponentType> componentTypes)
        {
            BitSet bitSet = new();
            for (uint i = 0; i < componentTypes.Length; i++)
            {
                bitSet |= componentTypes[i];
            }

            if (ComponentChunks.TryGetValue(bitSet, out ComponentChunk chunk))
            {
                return chunk;
            }
            else
            {
                throw new NullReferenceException($"No components found for the given types");
            }
        }

        /// <summary>
        /// Checks if this world contains a chunk with the given <paramref name="componentTypes"/>.
        /// </summary>
        public readonly bool ContainsComponentChunk(USpan<ComponentType> componentTypes)
        {
            BitSet bitSet = new();
            for (uint i = 0; i < componentTypes.Length; i++)
            {
                bitSet |= componentTypes[i];
            }

            return ComponentChunks.ContainsKey(bitSet);
        }

        /// <summary>
        /// Attempts to retrieve the component chunk that is all of the <paramref name="componentTypes"/>.
        /// </summary>
        public readonly bool TryGetComponentChunk(USpan<ComponentType> componentTypes, out ComponentChunk chunk)
        {
            BitSet bitSet = new();
            for (uint i = 0; i < componentTypes.Length; i++)
            {
                bitSet |= componentTypes[i];
            }

            return ComponentChunks.TryGetValue(bitSet, out chunk);
        }

        /// <summary>
        /// Counts how many entities there are with component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly uint CountEntitiesWithComponent<T>(bool onlyEnabled = false) where T : unmanaged
        {
            ComponentType type = Schema.GetComponent<T>();
            return CountEntitiesWithComponent(type, onlyEnabled);
        }

        /// <summary>
        /// Counts how many entities there are with component of the given <paramref name="componentType"/>.
        /// </summary>
        public readonly uint CountEntitiesWithComponent(ComponentType componentType, bool onlyEnabled = false)
        {
            uint count = 0;
            foreach (BitSet key in ComponentChunks.Keys)
            {
                if (key == componentType)
                {
                    ComponentChunk chunk = ComponentChunks[key];
                    if (!onlyEnabled)
                    {
                        count += chunk.Entities.Count;
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many entities comply with type <typeparamref name="T"/>.
        /// </summary>
        public readonly uint CountEntities<T>(bool onlyEnabled = false) where T : unmanaged, IEntity
        {
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            Definition definition = default(T).GetDefinition(Schema);
            uint count = 0;
            if (definition.ArrayTypesMask != default(BitSet))
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if ((key & definition.ComponentTypesMask) == definition.ComponentTypesMask)
                    {
                        ComponentChunk chunk = chunks[key];
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            BitSet arrayTypesMask = GetArrayTypesMask(entity);
                            if ((definition.ArrayTypesMask & arrayTypesMask) == arrayTypesMask)
                            {
                                if (onlyEnabled)
                                {
                                    if (IsEnabled(entity))
                                    {
                                        count++;
                                    }
                                }
                                else
                                {
                                    count++;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (!onlyEnabled)
                {
                    foreach (BitSet key in chunks.Keys)
                    {
                        if ((key & definition.ComponentTypesMask) == definition.ComponentTypesMask)
                        {
                            ComponentChunk chunk = chunks[key];
                            count += chunk.Entities.Count;
                        }
                    }
                }
                else
                {
                    foreach (BitSet key in chunks.Keys)
                    {
                        if ((key & definition.ComponentTypesMask) == definition.ComponentTypesMask)
                        {
                            ComponentChunk chunk = chunks[key];
                            for (uint e = 0; e < chunk.Entities.Count; e++)
                            {
                                uint entity = chunk.Entities[e];
                                if (IsEnabled(entity))
                                {
                                    count++;
                                }
                            }
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Copies components from the source entity onto the destination.
        /// <para>Components will be added if the destination entity doesnt
        /// contain them. Existing component data will be overwritten.</para>
        /// </summary>
        public readonly void CopyComponentsTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, sourceEntity);

            EntitySlot sourceSlot = Slots[sourceEntity - 1];
            ComponentChunk sourceChunk = sourceSlot.componentChunk;
            BitSet sourceComponentTypes = sourceChunk.TypesMask;
            uint sourceIndex = sourceChunk.Entities.IndexOf(sourceEntity);
            Schema schema = Schema;
            for (byte c = 0; c < BitSet.Capacity; c++)
            {
                if (sourceComponentTypes == c)
                {
                    ComponentType componentType = new(c);
                    if (!destinationWorld.ContainsComponent(destinationEntity, componentType))
                    {
                        destinationWorld.AddComponent(destinationEntity, componentType);
                    }

                    ushort componentSize = schema.GetComponentSize(componentType);
                    void* sourceComponent = sourceChunk.GetComponent(sourceIndex, componentType, componentSize);
                    void* destinationComponent = destinationWorld.GetComponent(destinationEntity, componentType);
                    System.Runtime.CompilerServices.Unsafe.CopyBlock(destinationComponent, sourceComponent, componentSize);
                }
            }
        }

        /// <summary>
        /// Copies all arrays from the source entity onto the destination.
        /// <para>Arrays will be created if the destination doesn't already
        /// contain them. Data will be overwritten, and lengths will be changed.</para>
        /// </summary>
        public readonly void CopyArraysTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            BitSet arrayTypesMask = GetArrayTypesMask(sourceEntity);
            for (byte a = 0; a < BitSet.Capacity; a++)
            {
                if (arrayTypesMask == a)
                {
                    ArrayType arrayType = new(a);
                    void* sourceArray = UnsafeWorld.GetArray(value, sourceEntity, arrayType, out uint sourceLength);
                    void* destinationArray;
                    if (!destinationWorld.ContainsArray(destinationEntity, arrayType))
                    {
                        destinationArray = UnsafeWorld.CreateArray(destinationWorld.value, destinationEntity, arrayType, sourceLength);
                    }
                    else
                    {
                        destinationArray = UnsafeWorld.ResizeArray(destinationWorld.value, destinationEntity, arrayType, sourceLength);
                    }

                    ushort arrayElementSize = Schema.GetArrayElementSize(arrayType);
                    System.Runtime.CompilerServices.Unsafe.CopyBlock(destinationArray, sourceArray, sourceLength * arrayElementSize);
                }
            }
        }

        /// <summary>
        /// Creates a perfect replica of this entity.
        /// </summary>
        public readonly uint CloneEntity(uint entity)
        {
            uint clone = CreateEntity();
            CopyComponentsTo(entity, this, clone);
            CopyArraysTo(entity, this, clone);
            return clone;
        }

        /// <summary>
        /// Finds all entities that contain all of the given component types and
        /// adds them to the given list.
        /// </summary>
        public readonly void Fill(USpan<ComponentType> componentTypes, List<uint> list, bool onlyEnabled = false)
        {
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            BitSet componentTypesMask = new();
            foreach (ComponentType type in componentTypes)
            {
                componentTypesMask |= type;
            }

            foreach (BitSet key in chunks.Keys)
            {
                if ((key & componentTypesMask) == componentTypesMask)
                {
                    ComponentChunk chunk = chunks[key];
                    if (!onlyEnabled)
                    {
                        list.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                list.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fills the given <paramref name="list"/> with all components of type <typeparamref name="T"/>.
        /// </summary>
        public readonly void Fill<T>(List<T> list, bool onlyEnabled = false) where T : unmanaged
        {
            ComponentType componentType = Schema.GetComponent<T>();
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            foreach (BitSet key in chunks.Keys)
            {
                if (key == componentType)
                {
                    ComponentChunk chunk = chunks[key];
                    if (!onlyEnabled)
                    {
                        list.AddRange(chunk.GetComponents<T>(componentType));
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                list.Add(chunk.GetComponent<T>(e, componentType));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fills the given <paramref name="entities"/> list with all entities that contain
        /// a component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly void Fill<T>(List<uint> entities, bool onlyEnabled = false) where T : unmanaged
        {
            ComponentType componentType = Schema.GetComponent<T>();
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            foreach (BitSet key in chunks.Keys)
            {
                if (key == componentType)
                {
                    ComponentChunk chunk = chunks[key];
                    if (!onlyEnabled)
                    {
                        entities.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fills <paramref name="components"/> and <paramref name="entities"/>.
        /// </summary>
        public readonly void Fill<T>(List<T> components, List<uint> entities, bool onlyEnabled = false) where T : unmanaged
        {
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            foreach (BitSet key in chunks.Keys)
            {
                if (key == componentType)
                {
                    ComponentChunk chunk = chunks[key];
                    if (!onlyEnabled)
                    {
                        components.AddRange(chunk.GetComponents<T>(componentType));
                        entities.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                components.Add(chunk.GetComponent<T>(e, componentType));
                                entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fills the given <paramref name="entities"/> list with all entities that have
        /// a component of type <paramref name="componentType"/>.
        /// </summary>
        public readonly void Fill(ComponentType componentType, List<uint> entities, bool onlyEnabled = false)
        {
            Dictionary<BitSet, ComponentChunk> chunks = ComponentChunks;
            foreach (BitSet key in chunks.Keys)
            {
                if (key == componentType)
                {
                    ComponentChunk chunk = chunks[key];
                    if (!onlyEnabled)
                    {
                        entities.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new world.
        /// </summary>
        public static World Create()
        {
            return new(UnsafeWorld.Allocate());
        }

        /// <inheritdoc/>
        public static bool operator ==(World left, World right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(World left, World right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Delegate for entities.
    /// </summary>
    public delegate void QueryCallback(in uint id);

    /// <summary>
    /// Delegate  for entities with a reference to <typeparamref name="T1"/> components.
    /// </summary>
    public delegate void QueryCallback<T1>(in uint id, ref T1 t1) where T1 : unmanaged;

    /// <summary>
    /// Delegate  for entities with references to <typeparamref name="T1"/> and <typeparamref name="T2"/> components.
    /// </summary>
    public delegate void QueryCallback<T1, T2>(in uint id, ref T1 t1, ref T2 t2) where T1 : unmanaged where T2 : unmanaged;

    /// <summary>
    /// Delegate  for entities with references to <typeparamref name="T1"/>, <typeparamref name="T2"/> and <typeparamref name="T3"/> components.
    /// </summary>
    public delegate void QueryCallback<T1, T2, T3>(in uint id, ref T1 t1, ref T2 t2, ref T3 t3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged;

    /// <summary>
    /// Delegate for entities with references to <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/> and <typeparamref name="T4"/> components.
    /// </summary>
    public delegate void QueryCallback<T1, T2, T3, T4>(in uint id, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged;

    /// <summary>
    /// Delegate for when an entity is created.
    /// </summary>
    public delegate void EntityCreatedCallback(World world, uint entity);

    /// <summary>
    /// Delegate for when an entity is destroyed.
    /// </summary>
    public delegate void EntityDestroyedCallback(World world, uint entity);

    /// <summary>
    /// Delegate for when an entity is enabled or disabled.
    /// </summary>
    public delegate void EntityParentChangedCallback(World world, uint entity, uint parent);

    /// <summary>
    /// Delegate for when a component is added to an entity.
    /// </summary>
    public delegate void ComponentAddedCallback(World world, uint entity, ComponentType componentType);

    /// <summary>
    /// Delegate for when a component is removed from an entity.
    /// </summary>
    public delegate void ComponentRemovedCallback(World world, uint entity, ComponentType componentType);
}
