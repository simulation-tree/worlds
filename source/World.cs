using Collections;
using Simulation.Unsafe;
using System;
using System.Diagnostics;
using Unmanaged;
using IEnumerableUInt = System.Collections.Generic.IEnumerable<uint>;

namespace Simulation
{
    /// <summary>
    /// Contains arbitrary data sorted into groups of entities for processing.
    /// </summary>
    public unsafe struct World : IDisposable, IEquatable<World>, ISerializable
    {
        internal UnsafeWorld* value;

        public readonly nint Address => (nint)value;

        /// <summary>
        /// Amount of entities that exist in the world.
        /// </summary>
        public readonly uint Count => Slots.Count - Free.Count;

        /// <summary>
        /// The current maximum amount of referrable entities.
        /// <para>Collections of this size are guaranteed to
        /// be able to store all entity values/positions.</para>
        /// </summary>
        public readonly uint MaxEntityValue => Slots.Count;

        public readonly bool IsDisposed => UnsafeWorld.IsDisposed(value);
        public readonly List<EntityDescription> Slots => UnsafeWorld.GetEntitySlots(value);
        public readonly List<uint> Free => UnsafeWorld.GetFreeEntities(value);
        public readonly Dictionary<int, ComponentChunk> ComponentChunks => UnsafeWorld.GetComponentChunks(value);

        /// <summary>
        /// All entities that exist in the world.
        /// </summary>
        public readonly IEnumerableUInt Entities
        {
            get
            {
                List<EntityDescription> slots = Slots;
                List<uint> free = Free;
                for (uint i = 0; i < slots.Count; i++)
                {
                    EntityDescription description = slots[i];
                    if (!free.Contains(description.entity))
                    {
                        yield return description.entity;
                    }
                }
            }
        }

        public readonly uint this[uint index]
        {
            get
            {
                uint i = 0;
                for (uint j = 0; j < Slots.Count; j++)
                {
                    EntityDescription description = Slots[j];
                    if (!Free.Contains(description.entity))
                    {
                        if (i == index)
                        {
                            return description.entity;
                        }

                        i++;
                    }
                }

                throw new IndexOutOfRangeException();
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
        public World(nint existingAddress)
        {
            value = (UnsafeWorld*)existingAddress;
        }

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

        public readonly override string ToString()
        {
            if (value == default)
            {
                return "World (disposed)";
            }

            return $"World {Address} (count: {Count})";
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is World world && Equals(world);
        }

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

        public readonly override int GetHashCode()
        {
            if (value is null)
            {
                return 0;
            }

            return value->GetHashCode();
        }

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            //collect info about all types referenced
            using List<RuntimeType> uniqueTypes = new(4);
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                foreach (RuntimeType type in chunk.Types)
                {
                    if (!uniqueTypes.Contains(type))
                    {
                        uniqueTypes.Add(type);
                    }
                }
            }

            for (uint i = 0; i < Slots.Count; i++)
            {
                EntityDescription slot = Slots[i];
                foreach (RuntimeType type in slot.arrayTypes)
                {
                    if (!uniqueTypes.Contains(type))
                    {
                        uniqueTypes.Add(type);
                    }
                }
            }

            //write info about the type tree
            writer.WriteValue(uniqueTypes.Count);
            for (uint a = 0; a < uniqueTypes.Count; a++)
            {
                RuntimeType type = uniqueTypes[a];
                writer.WriteValue(type);
            }

            //write each entity and its components
            writer.WriteValue(Count);
            for (uint i = 0; i < Slots.Count; i++)
            {
                EntityDescription slot = Slots[i];
                uint entity = slot.entity;
                if (!Free.Contains(entity))
                {
                    writer.WriteValue(entity);
                    writer.WriteValue(slot.parent);

                    //write component
                    ComponentChunk chunk = chunks[slot.componentsKey];
                    writer.WriteValue(chunk.Types.Length);
                    foreach (RuntimeType type in chunk.Types)
                    {
                        writer.WriteValue(uniqueTypes.IndexOf(type));
                        USpan<byte> componentBytes = chunk.GetComponentBytes(chunk.Entities.IndexOf(entity), type);
                        writer.WriteSpan(componentBytes);
                    }

                    //write arrays
                    writer.WriteValue(slot.arrayTypes.Count);
                    for (uint t = 0; t < slot.arrayTypes.Count; t++)
                    {
                        RuntimeType type = slot.arrayTypes[t];
                        void* array = slot.arrays[t];
                        uint arrayLength = slot.arrayLengths[t];
                        writer.WriteValue(uniqueTypes.IndexOf(type));
                        writer.WriteValue(arrayLength);
                        if (arrayLength > 0)
                        {
                            writer.WriteSpan(new USpan<byte>(array, arrayLength * type.Size));
                        }
                    }

                    //write references
                    writer.WriteValue(slot.references.Count);
                    foreach (uint referencedEntity in slot.references)
                    {
                        writer.WriteValue(referencedEntity);
                    }
                }
            }
        }

        void ISerializable.Read(BinaryReader reader)
        {
            value = UnsafeWorld.Allocate();
            uint typeCount = reader.ReadValue<uint>();
            List<EntityDescription> slots = Slots;
            using List<RuntimeType> uniqueTypes = new(4);
            for (uint i = 0; i < typeCount; i++)
            {
                RuntimeType type = reader.ReadValue<RuntimeType>();
                uniqueTypes.Add(type);
            }

            //create entities and fill them with components and arrays
            uint entityCount = reader.ReadValue<uint>();
            uint currentEntityId = 1;
            using List<uint> temporaryEntities = new(4);
            for (uint i = 0; i < entityCount; i++)
            {
                uint entityId = reader.ReadValue<uint>();
                uint parentId = reader.ReadValue<uint>();

                //skip through the island of free entities
                uint catchup = entityId - currentEntityId;
                for (uint j = 0; j < catchup; j++)
                {
                    uint temporaryEntity = CreateEntity();
                    temporaryEntities.Add(temporaryEntity);
                }

                uint entity = CreateEntity();
                if (parentId != default)
                {
                    ref EntityDescription slot = ref slots[entity - 1];
                    slot.parent = parentId;
                    UnsafeWorld.NotifyParentChange(this, entity, parentId);
                }

                //read components
                uint componentCount = reader.ReadValue<uint>();
                for (uint j = 0; j < componentCount; j++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    RuntimeType type = uniqueTypes[typeIndex];
                    USpan<byte> bytes = UnsafeWorld.AddComponent(value, entity, type);
                    reader.ReadSpan<byte>(type.Size).CopyTo(bytes);
                    UnsafeWorld.NotifyComponentAdded(this, entity, type);
                }

                //read arrays
                uint arrayCount = reader.ReadValue<uint>();
                for (uint a = 0; a < arrayCount; a++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    uint arrayLength = reader.ReadValue<uint>();
                    RuntimeType arrayType = uniqueTypes[typeIndex];
                    uint byteCount = arrayLength * arrayType.Size;
                    void* array = UnsafeWorld.CreateArray(value, entity, arrayType, arrayLength);
                    if (arrayLength > 0)
                    {
                        reader.ReadSpan<byte>(byteCount).CopyTo(new USpan<byte>(array, byteCount));
                    }
                }

                //read references
                uint referenceCount = reader.ReadValue<uint>();
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
                    ref EntityDescription parentSlot = ref slots[parent - 1];
                    parentSlot.children.Add(entity);
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
            foreach (EntityDescription sourceSlot in sourceWorld.Slots)
            {
                uint sourceEntity = sourceSlot.entity;
                if (!sourceWorld.Free.Contains(sourceEntity))
                {
                    uint destinationEntity = start + entityIndex;
                    InitializeEntity(default, destinationEntity, start + sourceSlot.parent);
                    entityIndex++;

                    //add components
                    ComponentChunk sourceChunk = sourceWorld.ComponentChunks[sourceSlot.componentsKey];
                    uint sourceIndex = sourceChunk.Entities.IndexOf(sourceEntity);
                    foreach (RuntimeType componentType in sourceChunk.Types)
                    {
                        USpan<byte> bytes = UnsafeWorld.AddComponent(value, destinationEntity, componentType);
                        sourceChunk.GetComponentBytes(sourceIndex, componentType).CopyTo(bytes);
                        UnsafeWorld.NotifyComponentAdded(this, destinationEntity, componentType);
                    }

                    //add arrays
                    for (uint t = 0; t < sourceSlot.arrayTypes.Count; t++)
                    {
                        RuntimeType sourceArrayType = sourceSlot.arrayTypes[t];
                        uint sourceArrayLength = sourceSlot.arrayLengths[t];
                        void* sourceArray = sourceSlot.arrays[t];
                        void* destinationArray = UnsafeWorld.CreateArray(value, destinationEntity, sourceArrayType, sourceArrayLength);
                        if (sourceArrayLength > 0)
                        {
                            USpan<byte> sourceBytes = new(sourceArray, sourceArrayLength * sourceArrayType.Size);
                            sourceBytes.CopyTo(new USpan<byte>(destinationArray, sourceArrayLength * sourceArrayType.Size));
                        }
                    }
                }
            }

            //assign references last
            entityIndex = 1;
            foreach (EntityDescription sourceSlot in sourceWorld.Slots)
            {
                uint sourceEntity = sourceSlot.entity;
                if (!sourceWorld.Free.Contains(sourceEntity))
                {
                    uint destinationEntity = start + entityIndex;
                    foreach (uint referencedEntity in sourceSlot.references)
                    {
                        AddReference(destinationEntity, start + referencedEntity);
                    }
                }
            }
        }

        private readonly void Perform(Instruction instruction, List<uint> selection, List<uint> entities)
        {
            if (instruction.type == Instruction.Type.CreateEntity)
            {
                uint count = (uint)instruction.A;
                for (uint i = 0; i < count; i++)
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
                if (instruction.A == 0)
                {
                    uint relativeOffset = (uint)instruction.B;
                    uint entity = entities[(entities.Count - 1) - relativeOffset];
                    selection.Add(entity);
                }
                else if (instruction.A == 1)
                {
                    uint entity = (uint)instruction.B;
                    selection.Add(entity);
                }
            }
            else if (instruction.type == Instruction.Type.SetParent)
            {
                bool isRelative = instruction.A == 0;
                if (isRelative)
                {
                    uint relativeOffset = (uint)instruction.B;
                    uint parent = entities[(entities.Count - 1) - relativeOffset];
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
                    uint referencedEntity = entities[(entities.Count - 1) - relativeOffset];
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
                RuntimeType componentType = new((uint)instruction.A);
                Allocation allocation = new((void*)(nint)instruction.B);
                USpan<byte> componentData = allocation.AsSpan<byte>(0, componentType.Size);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    AddComponent(entity, componentType, componentData);
                }
            }
            else if (instruction.type == Instruction.Type.RemoveComponent)
            {
                RuntimeType componentType = new((uint)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    RemoveComponent(entity, componentType);
                }
            }
            else if (instruction.type == Instruction.Type.SetComponent)
            {
                RuntimeType componentType = new((uint)instruction.A);
                Allocation allocation = new((void*)(nint)instruction.B);
                USpan<byte> componentBytes = allocation.AsSpan<byte>(0, componentType.Size);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    SetComponent(entity, componentType, componentBytes);
                }
            }
            else if (instruction.type == Instruction.Type.CreateArray)
            {
                RuntimeType arrayType = new((uint)instruction.A);
                uint arrayTypeSize = arrayType.Size;
                Allocation allocation = new((void*)(nint)instruction.B);
                uint count = (uint)instruction.C;
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    Allocation newArray = CreateArray(entity, arrayType, count);
                    allocation.CopyTo(newArray, 0, 0, count * arrayTypeSize);
                }
            }
            else if (instruction.type == Instruction.Type.DestroyArray)
            {
                RuntimeType arrayType = new((uint)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    DestroyArray(entity, arrayType);
                }
            }
            else if (instruction.type == Instruction.Type.SetArrayElement)
            {
                RuntimeType arrayType = new((uint)instruction.A);
                uint arrayTypeSize = arrayType.Size;
                Allocation allocation = new((void*)(nint)instruction.B);
                uint elementCount = allocation.Read<uint>();
                uint start = (uint)instruction.C;
                USpan<byte> elementBytes = allocation.AsSpan(sizeof(uint), elementCount * arrayTypeSize);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    void* array = UnsafeWorld.GetArray(value, entity, arrayType, out uint entityArrayLength);
                    USpan<byte> entityArray = new(array, entityArrayLength * arrayTypeSize);
                    elementBytes.CopyTo(entityArray.Slice(start * arrayTypeSize, elementCount * arrayTypeSize));
                }
            }
            else if (instruction.type == Instruction.Type.ResizeArray)
            {
                RuntimeType arrayType = new((uint)instruction.A);
                uint newLength = (uint)instruction.B;
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    UnsafeWorld.ResizeArray(value, entity, arrayType, newLength);
                }
            }
            else
            {
                throw new NotImplementedException($"Unknown instruction: {instruction.type}");
            }
        }

        public readonly void Perform(USpan<Instruction> instructions)
        {
            using List<uint> selection = new(4);
            using List<uint> entities = new(4);
            foreach (Instruction instruction in instructions)
            {
                Perform(instruction, selection, entities);
            }
        }

        public readonly void Perform(List<Instruction> instructions)
        {
            Perform(instructions.AsSpan());
        }

        public readonly void Perform(Array<Instruction> instructions)
        {
            Perform(instructions.AsSpan());
        }

        /// <summary>
        /// Performs all instructions in the given operation.
        /// </summary>
        public readonly void Perform(Operation operation)
        {
            using List<uint> selection = new(4);
            using List<uint> entities = new(4);
            uint length = operation.Length;
            for (uint i = 0; i < length; i++)
            {
                Instruction instruction = operation[i];
                Perform(instruction, selection, entities);
            }
        }

        /// <summary>
        /// Destroys the given entity assuming it exists.
        /// </summary>
        public readonly void DestroyEntity(uint entity, bool destroyChildren = true)
        {
            UnsafeWorld.DestroyEntity(value, entity, destroyChildren);
        }

        public readonly USpan<RuntimeType> GetComponentTypes(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            ComponentChunk chunk = ComponentChunks[slot.componentsKey];
            return chunk.Types;
        }

        /// <summary>
        /// Checks if the entity is enabled with respect
        /// to ancestor entities.
        /// </summary>
        public readonly bool IsEnabled(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            return slot.state == EntityDescription.State.Enabled;
        }

        /// <summary>
        /// Checks if the given entity is enabled regardless
        /// of the entity hierarchy.
        /// </summary>
        public readonly bool IsSelfEnabled(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            return slot.state == EntityDescription.State.Enabled || slot.state == EntityDescription.State.EnabledButDisabledDueToAncestor;
        }

        public readonly void SetEnabled(uint entity, bool state)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            ref EntityDescription slot = ref Slots[entity - 1];
            if (slot.parent != default)
            {
                EntityDescription.State parentState = Slots[slot.parent - 1].state;
                if (parentState == EntityDescription.State.Disabled || parentState == EntityDescription.State.EnabledButDisabledDueToAncestor)
                {
                    slot.state = state ? EntityDescription.State.EnabledButDisabledDueToAncestor : EntityDescription.State.Disabled;
                }
                else
                {
                    slot.state = state ? EntityDescription.State.Enabled : EntityDescription.State.Disabled;
                }
            }
            else
            {
                slot.state = state ? EntityDescription.State.Enabled : EntityDescription.State.Disabled;
            }

            for (uint i = 0; i < slot.children.Count; i++)
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
            Entity.ThrowIfTypeLayoutMismatches<T>();
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            Definition definition = default(T).Definition;
            USpan<RuntimeType> componentTypes = stackalloc RuntimeType[definition.ComponentTypeCount];
            definition.CopyComponentTypes(componentTypes);
            if (definition.ArrayTypeCount > 0)
            {
                USpan<RuntimeType> arrayTypes = stackalloc RuntimeType[definition.ArrayTypeCount];
                definition.CopyArrayTypes(arrayTypes);
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entityValue = chunk.Entities[e];
                            USpan<RuntimeType> entityArrays = GetArrayTypes(entityValue);
                            if (ContainsArrays(arrayTypes, entityArrays))
                            {
                                if (onlyEnabled)
                                {
                                    if (IsEnabled(entityValue))
                                    {
                                        entity = new Entity(this, entityValue).As<T>();
                                        return true;
                                    }
                                }
                                else
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
                    foreach (int hash in chunks.Keys)
                    {
                        ComponentChunk chunk = chunks[hash];
                        if (chunk.Entities.Count > 0 && chunk.ContainsTypes(componentTypes))
                        {
                            uint entityValue = chunk.Entities[0];
                            entity = new Entity(this, entityValue).As<T>();
                            return true;
                        }
                    }
                }
                else
                {
                    foreach (int hash in chunks.Keys)
                    {
                        ComponentChunk chunk = chunks[hash];
                        if (chunk.ContainsTypes(componentTypes))
                        {
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

            static bool ContainsArrays(USpan<RuntimeType> arrayTypes, USpan<RuntimeType> entityArrays)
            {
                for (uint i = 0; i < arrayTypes.Length; i++)
                {
                    if (!entityArrays.Contains(arrayTypes[i]))
                    {
                        return false;
                    }
                }

                return true;
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
                throw new NullReferenceException($"No entity of type {typeof(T)} exists");
            }
        }

        public readonly bool TryGetFirstComponent<T>(out T found) where T : unmanaged
        {
            return TryGetFirstComponent(out _, out found);
        }

        public readonly bool TryGetFirstEntityWithComponent<T>(out uint entity) where T : unmanaged
        {
            return TryGetFirstComponent<T>(out entity, out _);
        }

        public readonly bool TryGetFirstComponent<T>(out uint entity, out T component) where T : unmanaged
        {
            foreach (uint e in GetAll(RuntimeType.Get<T>()))
            {
                entity = e;
                component = GetComponentRef<T>(e);
                return true;
            }

            entity = default;
            component = default;
            return false;
        }

        public readonly T GetFirstComponent<T>() where T : unmanaged
        {
            foreach (uint e in GetAll(RuntimeType.Get<T>()))
            {
                return GetComponent<T>(e);
            }

            throw new NullReferenceException($"No entity with component of type {typeof(T)} found.");
        }

        public readonly T GetFirstComponent<T>(out uint entity) where T : unmanaged
        {
            foreach (uint e in GetAll(RuntimeType.Get<T>()))
            {
                entity = e;
                return GetComponent<T>(e);
            }

            throw new NullReferenceException($"No entity with component of type {typeof(T)} found.");
        }

        public readonly ref T GetFirstComponentRef<T>() where T : unmanaged
        {
            foreach (uint e in GetAll(RuntimeType.Get<T>()))
            {
                return ref GetComponentRef<T>(e);
            }

            throw new NullReferenceException($"No entity with component of type {typeof(T)} found.");
        }

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        public readonly uint CreateEntity()
        {
            return CreateEntity(default, default);
        }

        /// <summary>
        /// Creates a new entity with an assigned parent.
        /// </summary>
        public readonly uint CreateEntity(uint parent)
        {
            return CreateEntity(default, parent);
        }

        public readonly uint CreateEntity(Definition definition)
        {
            return CreateEntity(definition, default);
        }

        public readonly uint CreateEntity(Definition definition, uint parent)
        {
            uint entity = GetNextEntity();
            InitializeEntity(definition, entity, parent);
            return entity;
        }

        public readonly uint CreateEntity<T1>(T1 component1) where T1 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new Definition().AddComponentType<T1>();
            InitializeEntity(definition, entity, default);
            SetComponent(entity, component1);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2>(T1 component1, T2 component2) where T1 : unmanaged where T2 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new Definition().AddComponentTypes<T1, T2>();
            InitializeEntity(definition, entity, default);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3>(T1 component1, T2 component2, T3 component3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new Definition().AddComponentTypes<T1, T2, T3>();
            InitializeEntity(definition, entity, default);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4>(T1 component1, T2 component2, T3 component3, T4 component4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new Definition().AddComponentTypes<T1, T2, T3, T4>();
            InitializeEntity(definition, entity, default);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new Definition().AddComponentTypes<T1, T2, T3, T4, T5>();
            InitializeEntity(definition, entity, default);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            return entity;
        }

        public readonly uint CreateEntity<T1, T2, T3, T4, T5, T6>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            uint entity = GetNextEntity();
            Definition definition = new Definition().AddComponentTypes<T1, T2, T3, T4, T5, T6>();
            InitializeEntity(definition, entity, default);
            SetComponent(entity, component1);
            SetComponent(entity, component2);
            SetComponent(entity, component3);
            SetComponent(entity, component4);
            SetComponent(entity, component5);
            SetComponent(entity, component6);
            return entity;
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
        /// not already in use (otherwise an <see cref="Exception"/> will be thrown).
        /// </summary>
        public readonly void InitializeEntity(Definition definition, uint newEntity, uint parent)
        {
            UnsafeWorld.InitializeEntity(value, definition, newEntity, parent);
        }

        /// <summary>
        /// Checks if the entity exists and is valid in this world.
        /// </summary>
        public readonly bool ContainsEntity(uint entity)
        {
            return UnsafeWorld.ContainsEntity(value, entity);
        }

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
        /// Assigns a new parent.
        /// </summary>
        /// <returns><c>true</c> if the given parent entity was found and assigned successfuly.</returns>
        public readonly bool SetParent(uint entity, uint parent)
        {
            return UnsafeWorld.SetParent(value, entity, parent);
        }

        /// <summary>
        /// Retreives all children of the given entity.
        /// </summary>
        public readonly USpan<uint> GetChildren(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            return slot.children.AsSpan<uint>();
        }

        /// <summary>
        /// Adds a new reference to the given entity.
        /// </summary>
        /// <returns>An index offset by 1 that refers to this entity.</returns>
        public readonly rint AddReference(uint entity, uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            //UnsafeWorld.ThrowIfEntityIsMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots[entity - 1];
            slot.references.Add(referencedEntity);
            return new(slot.references.Count);
        }

        public readonly rint AddReference<T>(uint entity, T referencedEntity) where T : unmanaged, IEntity
        {
            return AddReference(entity, referencedEntity.Value);
        }

        /// <summary>
        /// Updates an existing reference to point towards a different entity.
        /// </summary>
        public readonly void SetReference(uint entity, rint reference, uint referencedEntity)
        {
            if (reference == default)
            {
                throw new InvalidOperationException($"Attempting to assign entity `{entity}` into a default reference slot");
            }

            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            //UnsafeWorld.ThrowIfEntityIsMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots[entity - 1];
            slot.references[reference.value - 1] = referencedEntity;
        }

        public readonly void SetReference<T>(uint entity, rint reference, T referencedEntity) where T : unmanaged, IEntity
        {
            SetReference(entity, reference, referencedEntity.Value);
        }

        public readonly bool ContainsReference(uint entity, uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            //UnsafeWorld.ThrowIfEntityIsMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots[entity - 1];
            return slot.references.Contains(referencedEntity);
        }

        public readonly bool ContainsReference<T>(uint entity, T referencedEntity) where T : unmanaged, IEntity
        {
            return ContainsReference(entity, referencedEntity.Value);
        }

        public readonly bool ContainsReference(uint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            ref EntityDescription slot = ref Slots[entity - 1];
            return (reference.value - 1) < slot.references.Count;
        }

        //todo: polish: this is kinda like `rint GetLastReference(uint entity)` <-- should it be like this instead?
        public readonly uint GetReferenceCount(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            ref EntityDescription slot = ref Slots[entity - 1];
            return slot.references.Count;
        }

        public readonly uint GetReference(uint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            if (reference == default)
            {
                return default;
            }

            ref EntityDescription slot = ref Slots[entity - 1];
            return slot.references[(uint)reference - 1];
        }

        public readonly bool TryGetReference(uint entity, rint position, out uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            ref EntityDescription slot = ref Slots[entity - 1];
            uint index = position.value - 1;
            if (index < slot.references.Count)
            {
                referencedEntity = slot.references[index];
                return true;
            }

            referencedEntity = default;
            return false;
        }

        public readonly void RemoveReference(uint entity, uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            UnsafeWorld.ThrowIfEntityIsMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots[entity - 1];
            uint index = slot.references.IndexOf(referencedEntity);
            slot.references.RemoveAt(index);
        }

        public readonly void RemoveReference(uint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            ref EntityDescription slot = ref Slots[entity - 1];
            slot.references.RemoveAt(reference.value - 1);
        }

        /// <summary>
        /// Retrieves the types of all arrays on this entity.
        /// </summary>
        public readonly USpan<RuntimeType> GetArrayTypes(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            return slot.arrayTypes.AsSpan();
        }

        /// <summary>
        /// Creates a new uninitialized array with the given length and type.
        /// </summary>
        public readonly Allocation CreateArray(uint entity, RuntimeType arrayLength, uint length = 0)
        {
            return new(UnsafeWorld.CreateArray(value, entity, arrayLength, length));
        }

        /// <summary>
        /// Creates a new uninitialized array on this entity.
        /// </summary>
        public readonly USpan<T> CreateArray<T>(uint entity, uint length = 0) where T : unmanaged
        {
            Allocation array = CreateArray(entity, RuntimeType.Get<T>(), length);
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

        public readonly bool ContainsArray<T>(uint entity) where T : unmanaged
        {
            return ContainsArray(entity, RuntimeType.Get<T>());
        }

        public readonly bool ContainsArray(uint entity, RuntimeType arrayType)
        {
            return UnsafeWorld.ContainsArray(value, entity, arrayType);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> on the given entity.
        /// </summary>
        public readonly USpan<T> GetArray<T>(uint entity) where T : unmanaged
        {
            void* array = UnsafeWorld.GetArray(value, entity, RuntimeType.Get<T>(), out uint length);
            return new USpan<T>(array, length);
        }

        public readonly Allocation GetArray(uint entity, RuntimeType arrayType, out uint length)
        {
            return new(UnsafeWorld.GetArray(value, entity, arrayType, out length));
        }

        public readonly USpan<T> ResizeArray<T>(uint entity, uint newLength) where T : unmanaged
        {
            void* array = UnsafeWorld.ResizeArray(value, entity, RuntimeType.Get<T>(), newLength);
            return new USpan<T>(array, newLength);
        }

        public readonly Allocation ResizeArray(uint entity, RuntimeType arrayType, uint newLength)
        {
            return new(UnsafeWorld.ResizeArray(value, entity, arrayType, newLength));
        }

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
        public readonly ref T GetArrayElementRef<T>(uint entity, uint index) where T : unmanaged
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            RuntimeType arrayType = RuntimeType.Get<T>();
            UnsafeWorld.ThrowIfArrayIsMissing(value, entity, arrayType);
            void* array = UnsafeWorld.GetArray(value, entity, arrayType, out uint arrayLength);
            USpan<T> span = new(array, arrayLength);
            return ref span[index];
        }

        /// <summary>
        /// Retrieves the length of an existing array on this entity.
        /// </summary>
        public readonly uint GetArrayLength<T>(uint entity) where T : unmanaged
        {
            return UnsafeWorld.GetArrayLength(value, entity, RuntimeType.Get<T>());
        }

        public readonly void DestroyArray<T>(uint entity) where T : unmanaged
        {
            DestroyArray(entity, RuntimeType.Get<T>());
        }

        public readonly void DestroyArray(uint entity, RuntimeType arrayType)
        {
            UnsafeWorld.DestroyArray(value, entity, arrayType);
        }

        public readonly void AddComponent<T>(uint entity, T component) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            ref T target = ref UnsafeWorld.AddComponent<T>(value, entity);
            target = component;
            UnsafeWorld.NotifyComponentAdded(this, entity, type);
        }

        /// <summary>
        /// Adds a new component of the given type with uninitialized data.
        /// </summary>
        public readonly void AddComponent<T>(uint entity) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            UnsafeWorld.AddComponent(value, entity, type);
            UnsafeWorld.NotifyComponentAdded(this, entity, type);
        }

        /// <summary>
        /// Adds a new default component with the given type.
        /// </summary>
        public readonly void AddComponent(uint entity, RuntimeType componentType)
        {
            UnsafeWorld.AddComponent(value, entity, componentType);
            UnsafeWorld.NotifyComponentAdded(this, entity, componentType);
        }

        public readonly void AddComponent(uint entity, RuntimeType componentType, USpan<byte> componentData)
        {
            USpan<byte> bytes = UnsafeWorld.AddComponent(value, entity, componentType);
            componentData.CopyTo(bytes);
            UnsafeWorld.NotifyComponentAdded(this, entity, componentType);
        }

        /// <summary>
        /// Adds a <c>default</c> component value and returns it by reference.
        /// </summary>
        public readonly ref T AddComponentRef<T>(uint entity) where T : unmanaged
        {
            AddComponent<T>(entity, default);
            return ref GetComponentRef<T>(entity);
        }

        public readonly void RemoveComponent<T>(uint entity) where T : unmanaged
        {
            UnsafeWorld.RemoveComponent<T>(value, entity);
        }

        public readonly void RemoveComponent(uint entity, RuntimeType componentType)
        {
            UnsafeWorld.RemoveComponent(value, entity, componentType);
        }

        /// <summary>
        /// Returns <c>true</c> if any entity in the world contains this component.
        /// </summary>
        public readonly bool ContainsAnyComponent<T>() where T : unmanaged
        {
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            RuntimeType type = RuntimeType.Get<T>();
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.Types.Contains(type))
                {
                    if (chunk.Entities.Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public readonly bool ContainsComponent<T>(uint entity) where T : unmanaged
        {
            return ContainsComponent(entity, RuntimeType.Get<T>());
        }

        public readonly bool ContainsComponent(uint entity, RuntimeType type)
        {
            return UnsafeWorld.ContainsComponent(value, entity, type);
        }

        public readonly ref T GetComponentRef<T>(uint entity) where T : unmanaged
        {
            return ref UnsafeWorld.GetComponentRef<T>(value, entity);
        }

        /// <summary>
        /// Returns the component of the expected type if it exists, otherwise the given default
        /// value is used.
        /// </summary>
        public readonly T GetComponent<T>(uint entity, T defaultValue) where T : unmanaged
        {
            if (ContainsComponent<T>(entity))
            {
                return GetComponentRef<T>(entity);
            }
            else
            {
                return defaultValue;
            }
        }

        public readonly T GetComponent<T>(uint entity) where T : unmanaged
        {
            return GetComponentRef<T>(entity);
        }

        /// <summary>
        /// Fetches the component from this entity as a span of bytes.
        /// </summary>
        public readonly USpan<byte> GetComponentBytes(uint entity, RuntimeType type)
        {
            return UnsafeWorld.GetComponentBytes(value, entity, type);
        }

        public readonly bool TryGetComponent<T>(uint entity, out T found) where T : unmanaged
        {
            if (ContainsComponent<T>(entity))
            {
                found = GetComponentRef<T>(entity);
                return true;
            }
            else
            {
                found = default;
                return false;
            }
        }

        public readonly ref T TryGetComponentRef<T>(uint entity, out bool contains) where T : unmanaged
        {
            if (ContainsComponent<T>(entity))
            {
                contains = true;
                return ref GetComponentRef<T>(entity);
            }
            else
            {
                contains = false;
                return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>(null);
            }
        }

        public readonly void SetComponent<T>(uint entity, T component) where T : unmanaged
        {
            ref T existing = ref GetComponentRef<T>(entity);
            existing = component;
        }

        public readonly void SetComponent(uint entity, RuntimeType componentType, USpan<byte> componentData)
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
            EntityDescription slot = Slots[entity - 1];
            return ComponentChunks[slot.componentsKey];
        }

        /// <summary>
        /// Returns the main component chunk that contains all of the given component types.
        /// </summary>
        public readonly ComponentChunk GetComponentChunk(USpan<RuntimeType> componentTypes)
        {
            int key = RuntimeType.CombineHash(componentTypes);
            if (ComponentChunks.TryGetValue(key, out ComponentChunk chunk))
            {
                return chunk;
            }
            else
            {
                throw new NullReferenceException($"No components found for the given types.");
            }
        }

        public readonly bool ContainsComponentChunk(USpan<RuntimeType> componentTypes)
        {
            int key = RuntimeType.CombineHash(componentTypes);
            return ComponentChunks.ContainsKey(key);
        }

        public readonly bool TryGetComponentChunk(USpan<RuntimeType> componentTypes, out ComponentChunk chunk)
        {
            int key = RuntimeType.CombineHash(componentTypes);
            return ComponentChunks.TryGetValue(key, out chunk);
        }

        /// <summary>
        /// Counts how many entities there are with component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly uint CountEntitiesWithComponent<T>(bool onlyEnabled = false) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            return CountEntitiesWithComponent(type, onlyEnabled);
        }

        public readonly uint CountEntitiesWithComponent(RuntimeType type, bool onlyEnabled = false)
        {
            uint count = 0;
            foreach (int hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.Types.Contains(type))
                {
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
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            Definition definition = default(T).Definition;
            USpan<RuntimeType> componentTypes = stackalloc RuntimeType[definition.ComponentTypeCount];
            definition.CopyComponentTypes(componentTypes);
            USpan<RuntimeType> arrayTypes = stackalloc RuntimeType[definition.ArrayTypeCount];
            definition.CopyArrayTypes(arrayTypes);
            uint count = 0;
            if (arrayTypes.Length > 0)
            {
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            USpan<RuntimeType> entityArrays = GetArrayTypes(entity);
                            if (ContainsArrays(arrayTypes, entityArrays))
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
                    foreach (int hash in chunks.Keys)
                    {
                        ComponentChunk chunk = chunks[hash];
                        if (chunk.ContainsTypes(componentTypes))
                        {
                            count += chunk.Entities.Count;
                        }
                    }
                }
                else
                {
                    foreach (int hash in chunks.Keys)
                    {
                        ComponentChunk chunk = chunks[hash];
                        if (chunk.ContainsTypes(componentTypes))
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
            }

            static bool ContainsArrays(USpan<RuntimeType> arrayTypes, USpan<RuntimeType> entityArrays)
            {
                for (uint i = 0; i < arrayTypes.Length; i++)
                {
                    if (!entityArrays.Contains(arrayTypes[i]))
                    {
                        return false;
                    }
                }

                return true;
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
            foreach (RuntimeType type in GetComponentTypes(sourceEntity))
            {
                if (!destinationWorld.ContainsComponent(destinationEntity, type))
                {
                    destinationWorld.AddComponent(destinationEntity, type);
                }

                USpan<byte> sourceBytes = GetComponentBytes(sourceEntity, type);
                USpan<byte> destinationBytes = destinationWorld.GetComponentBytes(destinationEntity, type);
                sourceBytes.CopyTo(destinationBytes);
            }
        }

        /// <summary>
        /// Copies all arrays from the source entity onto the destination.
        /// <para>Arrays will be created if the destination doesn't already
        /// contain them. Data will be overwritten, and lengths will be changed.</para>
        /// </summary>
        public readonly void CopyArraysTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            foreach (RuntimeType sourceArrayType in GetArrayTypes(sourceEntity))
            {
                void* sourceArray = UnsafeWorld.GetArray(value, sourceEntity, sourceArrayType, out uint sourceLength);
                void* destinationArray;
                if (!destinationWorld.ContainsArray(destinationEntity, sourceArrayType))
                {
                    destinationArray = UnsafeWorld.CreateArray(destinationWorld.value, destinationEntity, sourceArrayType, sourceLength);
                }
                else
                {
                    destinationArray = UnsafeWorld.ResizeArray(destinationWorld.value, destinationEntity, sourceArrayType, sourceLength);
                }

                uint elementSize = sourceArrayType.Size;
                USpan<byte> sourceBytes = new(sourceArray, sourceLength * elementSize);
                USpan<byte> destinationBytes = new(destinationArray, sourceLength * elementSize);
                sourceBytes.CopyTo(destinationBytes);
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
        public readonly void Fill(USpan<RuntimeType> componentTypes, List<uint> list, bool onlyEnabled = false)
        {
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.ContainsTypes(componentTypes))
                {
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

        public readonly void Fill<T>(List<T> list, bool onlyEnabled = false) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.Types.Contains(type))
                {
                    if (!onlyEnabled)
                    {
                        list.AddRange(chunk.GetComponents<T>());
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                list.Add(chunk.GetComponentRef<T>(e));
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill<T>(List<uint> entities, bool onlyEnabled = false) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.Types.Contains(type))
                {
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

        public readonly void Fill<T>(List<T> components, List<uint> entities, bool onlyEnabled = false) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.Types.Contains(type))
                {
                    if (!onlyEnabled)
                    {
                        components.AddRange(chunk.GetComponents<T>());
                        entities.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                components.Add(chunk.GetComponentRef<T>(e));
                                entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill(RuntimeType componentType, List<uint> entities, bool onlyEnabled = false)
        {
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.Types.Contains(componentType))
                {
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
        /// Iterates over all entities that contain the given component type.
        /// </summary>
        public readonly IEnumerableUInt GetAll(RuntimeType componentType, bool onlyEnabled = false)
        {
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            uint chunkCount = chunks.Count;
            for (uint i = 0; i < chunkCount; i++)
            {
                int hash = chunks.GetKeyAtIndex(i);
                ComponentChunk chunk = chunks[hash];
                if (chunk.Types.Contains(componentType))
                {
                    for (uint e = 0; e < chunk.Entities.Count; e++)
                    {
                        if (!onlyEnabled)
                        {
                            yield return chunk.Entities[e];
                        }
                        else
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                yield return entity;
                            }
                        }
                    }
                }
            }
        }

        public readonly IEnumerableUInt GetAll<T>(bool onlyEnabled = false) where T : unmanaged
        {
            return GetAll(RuntimeType.Get<T>(), onlyEnabled);
        }

        public readonly void ForEach<T>(QueryCallback callback, bool onlyEnabled = false) where T : unmanaged
        {
            RuntimeType componentType = RuntimeType.Get<T>();
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.Types.Contains(componentType))
                {
                    for (uint e = 0; e < chunk.Entities.Count; e++)
                    {
                        if (!onlyEnabled)
                        {
                            callback(chunk.Entities[e]);
                        }
                        else
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                callback(entity);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds all entities that contain all of the given component types
        /// and invokes the callback for every entity found.
        /// <para>
        /// Destroying entities inside the callback is not recommended.
        /// </para>
        /// </summary>
        public readonly void ForEach(USpan<RuntimeType> componentTypes, QueryCallback callback, bool onlyEnabled = false)
        {
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.ContainsTypes(componentTypes))
                {
                    for (uint e = 0; e < chunk.Entities.Count; e++)
                    {
                        if (!onlyEnabled)
                        {
                            callback(chunk.Entities[e]);
                        }
                        else
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                callback(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void ForEach<T>(QueryCallback<T> callback, bool onlyEnabled = false) where T : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[1];
            types[0] = RuntimeType.Get<T>();
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.ContainsTypes(types))
                {
                    List<uint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (!onlyEnabled)
                        {
                            ref T t1 = ref chunk.GetComponentRef<T>(e);
                            callback(entities[e], ref t1);
                        }
                        else
                        {
                            uint entity = entities[e];
                            if (IsEnabled(entity))
                            {
                                ref T t1 = ref chunk.GetComponentRef<T>(e);
                                callback(entity, ref t1);
                            }
                        }
                    }
                }
            }
        }

        public readonly void ForEach<T1, T2>(QueryCallback<T1, T2> callback, bool onlyEnabled = false) where T1 : unmanaged where T2 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[2];
            types[0] = RuntimeType.Get<T1>();
            types[1] = RuntimeType.Get<T2>();
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.ContainsTypes(types))
                {
                    List<uint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (!onlyEnabled)
                        {
                            ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                            ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                            callback(entities[e], ref t1, ref t2);
                        }
                        else
                        {
                            uint entity = entities[e];
                            if (IsEnabled(entity))
                            {
                                ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                                ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                                callback(entity, ref t1, ref t2);
                            }
                        }
                    }
                }
            }
        }

        public readonly void ForEach<T1, T2, T3>(QueryCallback<T1, T2, T3> callback, bool onlyEnabled = false) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[3];
            types[0] = RuntimeType.Get<T1>();
            types[1] = RuntimeType.Get<T2>();
            types[2] = RuntimeType.Get<T3>();
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.ContainsTypes(types))
                {
                    List<uint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (!onlyEnabled)
                        {
                            ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                            ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                            ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                            callback(entities[e], ref t1, ref t2, ref t3);
                        }
                        else
                        {
                            uint entity = entities[e];
                            if (IsEnabled(entity))
                            {
                                ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                                ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                                ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                                callback(entity, ref t1, ref t2, ref t3);
                            }
                        }
                    }
                }
            }
        }

        public readonly void ForEach<T1, T2, T3, T4>(QueryCallback<T1, T2, T3, T4> callback, bool onlyEnabled = false) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[4];
            types[0] = RuntimeType.Get<T1>();
            types[1] = RuntimeType.Get<T2>();
            types[2] = RuntimeType.Get<T3>();
            types[3] = RuntimeType.Get<T4>();
            Dictionary<int, ComponentChunk> chunks = ComponentChunks;
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.ContainsTypes(types))
                {
                    List<uint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (!onlyEnabled)
                        {
                            ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                            ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                            ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                            ref T4 t4 = ref chunk.GetComponentRef<T4>(e);
                            callback(entities[e], ref t1, ref t2, ref t3, ref t4);
                        }
                        else
                        {
                            uint entity = entities[e];
                            if (IsEnabled(entity))
                            {
                                ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                                ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                                ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                                ref T4 t4 = ref chunk.GetComponentRef<T4>(e);
                                callback(entity, ref t1, ref t2, ref t3, ref t4);
                            }
                        }
                    }
                }
            }
        }

        public static World Create()
        {
            return new(UnsafeWorld.Allocate());
        }

        public static bool operator ==(World left, World right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(World left, World right)
        {
            return !(left == right);
        }
    }

    public delegate void QueryCallback(in uint id);
    public delegate void QueryCallback<T1>(in uint id, ref T1 t1) where T1 : unmanaged;
    public delegate void QueryCallback<T1, T2>(in uint id, ref T1 t1, ref T2 t2) where T1 : unmanaged where T2 : unmanaged;
    public delegate void QueryCallback<T1, T2, T3>(in uint id, ref T1 t1, ref T2 t2, ref T3 t3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged;
    public delegate void QueryCallback<T1, T2, T3, T4>(in uint id, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged;

    public delegate void EntityCreatedCallback(World world, uint entity);
    public delegate void EntityDestroyedCallback(World world, uint entity);
    public delegate void EntityParentChangedCallback(World world, uint entity, uint parent);
    public delegate void ComponentAddedCallback(World world, uint entity, RuntimeType componentType);
    public delegate void ComponentRemovedCallback(World world, uint entity, RuntimeType componentType);
}
