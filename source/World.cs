using Simulation.Unsafe;
using System;
using System.Collections.Generic;
using Unmanaged;
using Unmanaged.Collections;

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
        /// <para>Creating an array of this size is guaranteed to
        /// be able to store all entity values.</para>
        /// </summary>
        public readonly uint MaxEntityValue => Slots.Count;

        public readonly bool IsDisposed => UnsafeWorld.IsDisposed(value);
        public readonly UnmanagedList<EntityDescription> Slots => UnsafeWorld.GetEntitySlots(value);
        public readonly UnmanagedList<eint> Free => UnsafeWorld.GetFreeEntities(value);
        public readonly UnmanagedDictionary<uint, ComponentChunk> ComponentChunks => UnsafeWorld.GetComponentChunks(value);

        /// <summary>
        /// All entities that exist in the world.
        /// </summary>
        public readonly IEnumerable<eint> Entities
        {
            get
            {
                UnmanagedList<EntityDescription> slots = Slots;
                UnmanagedList<eint> free = Free;
                for (uint i = 0; i < slots.Count; i++)
                {
                    EntityDescription description = slots[i];
                    if (!free.Contains(description.entity))
                    {
                        yield return new(description.entity);
                    }
                }
            }
        }

        public readonly eint this[uint index]
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
                            return new(description.entity);
                        }

                        i++;
                    }
                }

                throw new IndexOutOfRangeException();
            }
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Creates a new disposable world.
        /// </summary>
        public World()
        {
            value = UnsafeWorld.Allocate();
        }
#endif

        internal World(UnsafeWorld* value)
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
        public readonly void Clear(bool clearEvents = false, bool clearListeners = false)
        {
            if (clearEvents)
            {
                UnsafeWorld.ClearEvents(value);
            }

            if (clearListeners)
            {
                UnsafeWorld.ClearListeners(value);
            }

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
            using UnmanagedList<RuntimeType> uniqueTypes = UnmanagedList<RuntimeType>.Create();
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
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
                if (!slot.collections.IsDisposed)
                {
                    foreach (RuntimeType type in slot.collections.Types)
                    {
                        if (!uniqueTypes.Contains(type))
                        {
                            uniqueTypes.Add(type);
                        }
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
                eint entity = new(slot.entity);
                if (!Free.Contains(entity))
                {
                    writer.WriteValue(entity);
                    writer.WriteValue(slot.parent);

                    //write component
                    ComponentChunk chunk = ComponentChunks[slot.componentsKey];
                    writer.WriteValue((uint)chunk.Types.Length);
                    foreach (RuntimeType type in chunk.Types)
                    {
                        writer.WriteValue(uniqueTypes.IndexOf(type));
                        Span<byte> componentBytes = chunk.GetComponentBytes(entity, type);
                        writer.WriteSpan<byte>(componentBytes);
                    }

                    //write lists
                    if (slot.collections.IsDisposed)
                    {
                        writer.WriteValue(0u);
                    }
                    else
                    {
                        writer.WriteValue((uint)slot.collections.Types.Length);
                        foreach (RuntimeType type in slot.collections.Types)
                        {
                            writer.WriteValue(uniqueTypes.IndexOf(type));
                            UnsafeList* list = slot.collections.GetList(type);
                            uint listCount = UnsafeList.GetCountRef(list);
                            writer.WriteValue(listCount);
                            if (listCount > 0)
                            {
                                nint address = UnsafeList.GetAddress(list);
                                Span<byte> bytes = new((void*)address, (int)(listCount * type.Size));
                                writer.WriteSpan<byte>(bytes);
                            }
                        }
                    }

                    //write references
                    if (slot.references == default)
                    {
                        writer.WriteValue(0u);
                    }
                    else
                    {
                        writer.WriteValue(slot.references.Count);
                        foreach (uint referencedEntity in slot.references)
                        {
                            writer.WriteValue(referencedEntity);
                        }
                    }
                }
            }
        }

        void ISerializable.Read(BinaryReader reader)
        {
            value = UnsafeWorld.Allocate();
            uint typeCount = reader.ReadValue<uint>();
            using UnmanagedList<RuntimeType> uniqueTypes = UnmanagedList<RuntimeType>.Create();
            Span<char> buffer = stackalloc char[256];
            for (uint i = 0; i < typeCount; i++)
            {
                RuntimeType type = reader.ReadValue<RuntimeType>();
                uniqueTypes.Add(type);
            }

            //create entities and fill them with components and lists
            uint entityCount = reader.ReadValue<uint>();
            uint currentEntityId = 1;
            using UnmanagedList<eint> temporaryEntities = UnmanagedList<eint>.Create();
            for (uint i = 0; i < entityCount; i++)
            {
                uint entityId = reader.ReadValue<uint>();
                uint parentId = reader.ReadValue<uint>();

                //skip through the island of free entities
                uint catchup = entityId - currentEntityId;
                for (uint j = 0; j < catchup; j++)
                {
                    eint temporaryEntity = CreateEntity();
                    temporaryEntities.Add(temporaryEntity);
                }

                eint entity = CreateEntity();
                if (parentId != default)
                {
                    ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
                    slot.parent = parentId;
                    UnsafeWorld.NotifyParentChange(this, entity, new(parentId));
                }

                //read components
                uint componentCount = reader.ReadValue<uint>();
                for (uint j = 0; j < componentCount; j++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    RuntimeType type = uniqueTypes[typeIndex];
                    Span<byte> bytes = UnsafeWorld.AddComponent(value, entity, type);
                    reader.ReadSpan<byte>(type.Size).CopyTo(bytes);
                    UnsafeWorld.NotifyComponentAdded(this, entity, type);
                }

                //read lists
                uint listCount = reader.ReadValue<uint>();
                for (uint j = 0; j < listCount; j++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    RuntimeType type = uniqueTypes[typeIndex];
                    uint listLength = reader.ReadValue<uint>();
                    uint byteCount = listLength * type.Size;
                    UnsafeList* list = UnsafeWorld.CreateList(value, entity, type, listLength == 0 ? 1 : listLength);
                    if (listLength > 0)
                    {
                        UnsafeList.AddDefault(list, listLength);
                        nint address = UnsafeList.GetAddress(list);
                        Span<byte> destinationBytes = new((void*)address, (int)byteCount);
                        ReadOnlySpan<byte> sourceBytes = reader.ReadSpan<byte>(byteCount);
                        sourceBytes.CopyTo(destinationBytes);
                    }
                }

                //read references
                uint referenceCount = reader.ReadValue<uint>();
                for (uint j = 0; j < referenceCount; j++)
                {
                    eint referencedEntity = new(reader.ReadValue<uint>());
                    AddReference(entity, referencedEntity);
                }

                currentEntityId = entityId + 1;
            }

            //assign children
            foreach (eint entity in Entities)
            {
                eint parent = GetParent(entity);
                if (parent != default)
                {
                    ref EntityDescription parentSlot = ref Slots.GetRef(parent.value - 1);
                    if (parentSlot.children == default)
                    {
                        parentSlot.children = UnmanagedList<uint>.Create();
                    }

                    parentSlot.children.Add(entity.value);
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
                eint sourceEntity = new(sourceSlot.entity);
                if (!sourceWorld.Free.Contains(sourceEntity))
                {
                    eint destinationEntity = new(start + entityIndex);
                    InitializeEntity(destinationEntity, new(start + sourceSlot.parent));
                    entityIndex++;

                    //add components
                    ComponentChunk sourceChunk = sourceWorld.ComponentChunks[sourceSlot.componentsKey];
                    foreach (RuntimeType componentType in sourceChunk.Types)
                    {
                        Span<byte> bytes = UnsafeWorld.AddComponent(value, destinationEntity, componentType);
                        sourceChunk.GetComponentBytes(sourceEntity, componentType).CopyTo(bytes);
                        UnsafeWorld.NotifyComponentAdded(this, destinationEntity, componentType);
                    }

                    //add lists
                    if (!sourceSlot.collections.IsDisposed)
                    {
                        foreach (RuntimeType listType in sourceSlot.collections.Types)
                        {
                            UnsafeList* sourceList = sourceSlot.collections.GetList(listType);
                            uint count = UnsafeList.GetCountRef(sourceList);
                            UnsafeList* destinationList = UnsafeWorld.CreateList(value, destinationEntity, listType, count + 1);
                            UnsafeList.AddDefault(destinationList, count);
                            for (uint e = 0; e < count; e++)
                            {
                                UnsafeList.CopyElementTo(sourceList, e, destinationList, e);
                            }
                        }
                    }
                }
            }

            //assign references last
            entityIndex = 1;
            foreach (EntityDescription sourceSlot in sourceWorld.Slots)
            {
                eint sourceEntity = new(sourceSlot.entity);
                if (!sourceWorld.Free.Contains(sourceEntity))
                {
                    if (sourceSlot.references != default)
                    {
                        eint destinationEntity = new(start + entityIndex);
                        foreach (uint referencedEntity in sourceSlot.references)
                        {
                            AddReference(destinationEntity, new(start + referencedEntity));
                        }
                    }
                }
            }
        }

        public readonly void Submit<T>(T message) where T : unmanaged
        {
            UnsafeWorld.Submit(value, Container.Create(message));
        }

        public readonly void Submit(Container container)
        {
            UnsafeWorld.Submit(value, container);
        }

        /// <summary>
        /// Iterates over all events that we submitted with <see cref="Submit"/>,
        /// and notifies the registered listeners.
        /// </summary>
        public readonly void Poll()
        {
            UnsafeWorld.Poll(value);
        }

        private readonly void Perform(Instruction instruction, UnmanagedList<eint> selection, UnmanagedList<eint> entities)
        {
            if (instruction.type == Instruction.Type.CreateEntity)
            {
                uint count = (uint)instruction.A;
                for (uint i = 0; i < count; i++)
                {
                    eint newEntity = CreateEntity();
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
                        eint entity = selection[i];
                        DestroyEntity(entity);
                    }
                }
                else
                {
                    uint end = start + count;
                    for (uint i = start; i < end; i++)
                    {
                        eint entity = entities[i];
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
                    eint entity = entities[(entities.Count - 1) - relativeOffset];
                    selection.Clear();
                    selection.Add(entity);
                }
                else if (instruction.A == 1)
                {
                    eint entity = new((uint)instruction.B);
                    selection.Clear();
                    selection.Add(entity);
                }
            }
            else if (instruction.type == Instruction.Type.SetParent)
            {
                bool isRelative = instruction.A == 0;
                if (isRelative)
                {
                    uint relativeOffset = (uint)instruction.B;
                    eint parent = entities[(entities.Count - 1) - relativeOffset];
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        SetParent(entity, parent);
                    }
                }
                else
                {
                    eint parent = new((uint)instruction.B);
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
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
                    eint referencedEntity = entities[(entities.Count - 1) - relativeOffset];
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        AddReference(entity, referencedEntity);
                    }
                }
                else
                {
                    eint referencedEntity = new((uint)instruction.B);
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        AddReference(entity, referencedEntity);
                    }
                }
            }
            else if (instruction.type == Instruction.Type.RemoveReference)
            {
                rint reference = new((uint)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    eint entity = selection[i];
                    RemoveReference(entity, reference);
                }
            }
            else if (instruction.type == Instruction.Type.AddComponent)
            {
                RuntimeType componentType = new((uint)instruction.A);
                Allocation allocation = new((void*)(nint)instruction.B);
                Span<byte> componentData = allocation.AsSpan<byte>(0, componentType.Size);
                for (uint i = 0; i < selection.Count; i++)
                {
                    eint entity = selection[i];
                    AddComponent(entity, componentType, componentData);
                }
            }
            else if (instruction.type == Instruction.Type.RemoveComponent)
            {
                RuntimeType componentType = new((uint)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    eint entity = selection[i];
                    RemoveComponent(entity, componentType);
                }
            }
            else if (instruction.type == Instruction.Type.SetComponent)
            {
                RuntimeType componentType = new((uint)instruction.A);
                Allocation allocation = new((void*)(nint)instruction.B);
                Span<byte> componentBytes = allocation.AsSpan<byte>(0, componentType.Size);
                for (uint i = 0; i < selection.Count; i++)
                {
                    eint entity = selection[i];
                    SetComponent(entity, componentType, componentBytes);
                }
            }
            else if (instruction.type == Instruction.Type.CreateList)
            {
                RuntimeType listType = new((uint)instruction.A);
                uint count = (uint)instruction.B;
                uint initialCapacity = count == 0 ? 1 : count;
                for (uint i = 0; i < selection.Count; i++)
                {
                    eint entity = selection[i];
                    UnsafeList* list = CreateList(entity, listType, initialCapacity);
                    UnsafeList.AddDefault(list, count);
                }
            }
            else if (instruction.type == Instruction.Type.DestroyList)
            {
                RuntimeType listType = new((uint)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    eint entity = selection[i];
                    DestroyList(entity, listType);
                }
            }
            else if (instruction.type == Instruction.Type.ClearList)
            {
                RuntimeType listType = new((uint)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    eint entity = selection[i];
                    ClearList(entity, listType);
                }
            }
            else if (instruction.type == Instruction.Type.InsertElement)
            {
                RuntimeType listType = new((uint)instruction.A);
                uint elementSize = listType.Size;
                UnsafeArray* array = (UnsafeArray*)(nint)instruction.B;
                uint index = (uint)instruction.C;
                uint arrayLength = UnsafeArray.GetLength(array);
                int length = (int)(arrayLength * elementSize);
                Span<byte> elementBytes = new((void*)UnsafeArray.GetAddress(array), length);
                if (index == uint.MaxValue)
                {
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        UnsafeList* list = GetList(entity, listType);
                        for (uint e = 0; e < elementBytes.Length; e += elementSize)
                        {
                            UnsafeList.Add(list, elementBytes.Slice((int)e, (int)elementSize));
                        }
                    }
                }
                else
                {
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        UnsafeList* list = GetList(entity, listType);
                        for (uint e = 0; e < elementBytes.Length; e += elementSize)
                        {
                            UnsafeList.Insert(list, index, elementBytes.Slice((int)e, (int)elementSize));
                            index++;
                        }
                    }
                }
            }
            else if (instruction.type == Instruction.Type.RemoveElement)
            {
                RuntimeType listType = new((uint)instruction.A);
                uint index = (uint)instruction.B;
                for (uint i = 0; i < selection.Count; i++)
                {
                    eint entity = selection[i];
                    UnsafeList* list = GetList(entity, listType);
                    UnsafeList.RemoveAt(list, index);
                }
            }
            else if (instruction.type == Instruction.Type.ModifyElement)
            {
                RuntimeType listType = new((uint)instruction.A);
                Allocation allocation = new((void*)(nint)instruction.B);
                uint index = (uint)instruction.C;
                Span<byte> elementBytes = allocation.AsSpan<byte>(0, listType.Size);
                for (uint i = 0; i < selection.Count; i++)
                {
                    eint entity = selection[i];
                    UnsafeList* list = GetList(entity, listType);
                    Span<byte> slotBytes = UnsafeList.GetElementBytes(list, index);
                    elementBytes.CopyTo(slotBytes);
                }
            }
            else
            {
                throw new NotImplementedException($"Unknown instruction: {instruction.type}");
            }
        }

        public readonly void Perform(ReadOnlySpan<Instruction> instructions)
        {
            using UnmanagedList<eint> selection = UnmanagedList<eint>.Create();
            using UnmanagedList<eint> entities = UnmanagedList<eint>.Create();
            foreach (Instruction instruction in instructions)
            {
                Perform(instruction, selection, entities);
            }
        }

        public readonly void Perform(UnmanagedList<Instruction> instructions)
        {
            Perform(instructions.AsSpan());
        }

        public readonly void Perform(UnmanagedArray<Instruction> instructions)
        {
            Perform(instructions.AsSpan());
        }

        /// <summary>
        /// Performs all instructions in the given operation.
        /// </summary>
        public readonly void Perform(Operation operation)
        {
            using UnmanagedList<eint> selection = UnmanagedList<eint>.Create();
            using UnmanagedList<eint> entities = UnmanagedList<eint>.Create();
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
        public readonly void DestroyEntity(eint entity, bool destroyChildren = true)
        {
            UnsafeWorld.DestroyEntity(value, entity, destroyChildren);
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Creates a new listener for the given static callback.
        /// <para>Disposing the listener will unregister the callback.
        /// Their disposal is done automatically when the world is disposed.</para>
        /// </summary>
        public readonly Listener CreateListener<T>(delegate* unmanaged<World, Container, void> callback) where T : unmanaged
        {
            return CreateListener(RuntimeType.Get<T>(), callback);
        }

        /// <summary>
        /// Creates a new listener for the given static callback.
        /// <para>Disposing the listener will unregister the callback.
        /// Their disposal is done automatically when the world is disposed.</para>
        /// </summary>
        public readonly Listener CreateListener(RuntimeType eventType, delegate* unmanaged<World, Container, void> callback)
        {
            return UnsafeWorld.CreateListener(value, eventType, callback);
        }
#else
        /// <summary>
        /// Creates a new listener for the given static callback.
        /// <para>Disposing the listener will unregister the callback.
        /// Their disposal is done automatically when the world is disposed.</para>
        /// </summary>
        public readonly Listener CreateListener<T>(delegate*<World, Container, void> callback) where T : unmanaged
        {
            return CreateListener(RuntimeType.Get<T>(), callback);
        }

        /// <summary>
        /// Creates a new listener for the given static callback.
        /// <para>Disposing the listener will unregister the callback.
        /// Their disposal is done automatically when the world is disposed.</para>
        /// </summary>
        public readonly Listener CreateListener(RuntimeType eventType, delegate*<World, Container, void> callback)
        {
            return UnsafeWorld.CreateListener(value, eventType, callback);
        }
#endif

        public readonly ReadOnlySpan<RuntimeType> GetComponentTypes(eint entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity.value - 1];
            ComponentChunk chunk = ComponentChunks[slot.componentsKey];
            return chunk.Types;
        }

        /// <summary>
        /// Retrieves the types for all lists on this entity.
        /// </summary>
        public readonly ReadOnlySpan<RuntimeType> GetListTypes(eint entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity.value - 1];
            return slot.collections.Types;
        }

        public readonly bool IsEnabled(eint entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity.value - 1];
            return slot.IsEnabled;
        }

        public readonly void SetEnabled(eint entity, bool value)
        {
            UnsafeWorld.ThrowIfEntityMissing(this.value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
            slot.SetEnabledState(value);
        }

        public readonly bool TryGetFirst<T>(out T found) where T : unmanaged
        {
            return TryGetFirst(out _, out found);
        }

        public readonly bool TryGetFirst<T>(out eint entity) where T : unmanaged
        {
            return TryGetFirst<T>(out entity, out _);
        }

        public readonly bool TryGetFirst<T>(out eint entity, out T component) where T : unmanaged
        {
            foreach (eint e in GetAll(RuntimeType.Get<T>()))
            {
                entity = e;
                component = GetComponentRef<T>(e);
                return true;
            }

            entity = default;
            component = default;
            return false;
        }

        public readonly T GetFirst<T>() where T : unmanaged
        {
            foreach (eint e in GetAll(RuntimeType.Get<T>()))
            {
                return GetComponentRef<T>(e);
            }

            throw new NullReferenceException($"No entity with component of type {typeof(T)} found.");
        }

        public readonly T GetFirst<T>(out eint entity) where T : unmanaged
        {
            foreach (eint e in GetAll(RuntimeType.Get<T>()))
            {
                entity = e;
                return GetComponentRef<T>(e);
            }

            throw new NullReferenceException($"No entity with component of type {typeof(T)} found.");
        }

        public readonly ref T GetFirstRef<T>() where T : unmanaged
        {
            foreach (eint e in GetAll(RuntimeType.Get<T>()))
            {
                return ref GetComponentRef<T>(e);
            }

            throw new NullReferenceException($"No entity with component of type {typeof(T)} found.");
        }

        public readonly eint CreateEntity(eint parent = default)
        {
            eint entity = GetNextEntity();
            InitializeEntity(entity, parent);
            return entity;
        }

        /// <summary>
        /// Returns the value for the next created entity.
        /// </summary>
        public readonly eint GetNextEntity()
        {
            return UnsafeWorld.GetNextEntity(value);
        }

        /// <summary>
        /// Creates an entity with the given value assuming its 
        /// not already in use (otherwise an <see cref="Exception"/> will be thrown).
        /// </summary>
        public readonly void InitializeEntity(eint value, eint parent)
        {
            UnsafeWorld.InitializeEntity(this.value, value, parent, Array.Empty<RuntimeType>());
        }

        /// <summary>
        /// Checks if the entity exists and is valid in this world.
        /// </summary>
        public readonly bool ContainsEntity(eint entity)
        {
            return UnsafeWorld.ContainsEntity(value, entity.value);
        }

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
        public readonly eint GetParent(eint entity)
        {
            return UnsafeWorld.GetParent(value, entity);
        }

        /// <summary>
        /// Assigns a new parent.
        /// </summary>
        /// <returns><c>true</c> if the given parent entity was found and assigned successfuly.</returns>
        public readonly bool SetParent(eint entity, eint parent)
        {
            return UnsafeWorld.SetParent(value, entity, parent);
        }

        /// <summary>
        /// Retreives all children of the given entity.
        /// </summary>
        public readonly ReadOnlySpan<eint> GetChildren(eint entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity.value - 1];
            if (!slot.children.IsDisposed)
            {
                return slot.children.AsSpan<eint>();
            }
            else return Array.Empty<eint>();
        }

        /// <summary>
        /// Adds a new reference to the given entity.
        /// </summary>
        /// <returns>An index offset by 1 that refers to this entity.</returns>
        public readonly rint AddReference(eint entity, eint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            UnsafeWorld.ThrowIfEntityMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
            if (slot.references == default)
            {
                slot.references = UnmanagedList<uint>.Create();
            }

            slot.references.Add(referencedEntity.value);
            return new(slot.references.Count);
        }

        public readonly rint AddReference<T>(eint entity, T referencedEntity) where T : unmanaged, IEntity
        {
            return AddReference(entity, referencedEntity.Value);
        }

        public readonly void SetReference(eint entity, rint reference, eint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            UnsafeWorld.ThrowIfEntityMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
            if (slot.references == default)
            {
                throw new IndexOutOfRangeException($"No references found on entity `{entity.value}`.");
            }

            slot.references[reference.value - 1] = referencedEntity.value;
        }

        public readonly void SetReference<T>(eint entity, rint reference, T referencedEntity) where T : unmanaged, IEntity
        {
            SetReference(entity, reference, referencedEntity.Value);
        }

        public readonly bool ContainsReference(eint entity, eint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            UnsafeWorld.ThrowIfEntityMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
            if (slot.references == default)
            {
                return false;
            }

            return slot.references.Contains(referencedEntity);
        }

        public readonly bool ContainsReference<T>(eint entity, T referencedEntity) where T : unmanaged, IEntity
        {
            return ContainsReference(entity, referencedEntity.Value);
        }

        public readonly bool ContainsReference(eint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
            if (slot.references == default)
            {
                return false;
            }

            return (reference.value - 1) < slot.references.Count;
        }

        //todo: polish: this is kinda like `rint GetLastReference(eint entity)` <-- should it be like this instead?
        public readonly uint GetReferenceCount(eint entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
            if (slot.references == default)
            {
                return 0;
            }

            return slot.references.Count;
        }

        public readonly eint GetReference(eint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            if (reference == default)
            {
                return default;
            }

            ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
            if (slot.references == default)
            {
                throw new IndexOutOfRangeException($"No references found on entity `{entity.value}`.");
            }

            return new(slot.references[reference.value - 1]);
        }

        public readonly bool TryGetReference(eint entity, rint position, out eint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
            if (slot.references == default)
            {
                referencedEntity = default;
                return false;
            }

            uint index = position.value - 1;
            if (index < slot.references.Count)
            {
                referencedEntity = new(slot.references[index]);
                return true;
            }

            referencedEntity = default;
            return false;
        }

        public readonly void RemoveReference(eint entity, eint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            UnsafeWorld.ThrowIfEntityMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
            if (slot.references == default)
            {
                throw new IndexOutOfRangeException($"No references found on entity `{entity.value}`.");
            }

            uint index = slot.references.IndexOf(referencedEntity);
            slot.references.RemoveAt(index);
        }

        public readonly void RemoveReference(eint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
            if (slot.references == default)
            {
                throw new IndexOutOfRangeException($"No references found on entity `{entity.value}`.");
            }

            slot.references.RemoveAt(reference.value - 1);
        }

        public readonly UnsafeList* CreateList(eint entity, RuntimeType listType, uint initialCapacity = 1)
        {
            return UnsafeWorld.CreateList(value, entity, listType, initialCapacity);
        }

        /// <summary>
        /// Creates a new list on this entity.
        /// </summary>
        public readonly UnmanagedList<T> CreateList<T>(eint entity, uint initialCapacity = 1) where T : unmanaged
        {
            UnmanagedList<T> list = new(UnsafeWorld.CreateList(value, entity, RuntimeType.Get<T>(), initialCapacity));
            return list;
        }

        public readonly void CreateList<T>(eint entity, ReadOnlySpan<T> values) where T : unmanaged
        {
            UnmanagedList<T> list = new(UnsafeWorld.CreateList(value, entity, RuntimeType.Get<T>()));
            list.AddRange(values);
        }

        public readonly void AddToList<T>(eint entity, T value) where T : unmanaged
        {
            UnmanagedList<T> list = new(UnsafeWorld.GetList(this.value, entity, RuntimeType.Get<T>()));
            list.Add(value);
        }

        public readonly bool ContainsList<T>(eint entity) where T : unmanaged
        {
            return ContainsList(entity, RuntimeType.Get<T>());
        }

        public readonly bool ContainsList(eint entity, RuntimeType listType)
        {
            return UnsafeWorld.ContainsList(value, entity, listType);
        }

        public readonly void AddRangeToList<T>(eint entity, ReadOnlySpan<T> values) where T : unmanaged
        {
            UnmanagedList<T> list = new(UnsafeWorld.GetList(value, entity, RuntimeType.Get<T>()));
            list.AddRange(values);
        }

        /// <summary>
        /// Retrieves a list of type <typeparamref name="T"/> on the given entity.
        /// </summary>
        public readonly UnmanagedList<T> GetList<T>(eint entity) where T : unmanaged
        {
            return new(UnsafeWorld.GetList(value, entity, RuntimeType.Get<T>()));
        }

        public readonly bool TryGetList<T>(eint entity, out UnmanagedList<T> list) where T : unmanaged
        {
            if (ContainsList<T>(entity))
            {
                list = GetList<T>(entity);
                return true;
            }
            else
            {
                list = default;
                return false;
            }
        }

        public readonly UnsafeList* GetList(eint entity, RuntimeType listType)
        {
            return UnsafeWorld.GetList(value, entity, listType);
        }

        /// <summary>
        /// Retrieves the element at the index from an existing list on this entity.
        /// </summary>
        public readonly ref T GetListElement<T>(eint entity, uint index) where T : unmanaged
        {
            EntityDescription slot = UnsafeWorld.GetEntitySlotRef(value, entity);
            if (slot.collections.IsDisposed)
            {
                throw new InvalidOperationException($"No lists found on entity `{entity.value}`.");
            }

            RuntimeType listType = RuntimeType.Get<T>();
            if (!slot.collections.Types.Contains(listType))
            {
                throw new InvalidOperationException($"No list of type `{typeof(T)}` found on entity `{entity.value}`.");
            }

            UnsafeList* list = slot.collections.GetList(listType);
            return ref UnsafeList.GetRef<T>(list, index);
        }

        /// <summary>
        /// Retrieves the length of an existing list on this entity.
        /// </summary>
        public readonly uint GetListLength<T>(eint entity, out bool contains) where T : unmanaged
        {
            EntityDescription slot = UnsafeWorld.GetEntitySlotRef(value, entity);
            if (slot.collections.IsDisposed)
            {
                contains = false;
                return 0;
            }

            RuntimeType listType = RuntimeType.Get<T>();
            if (!slot.collections.Types.Contains(listType))
            {
                contains = false;
                return 0;
            }

            contains = true;
            return UnsafeList.GetCountRef(slot.collections.GetList(listType));
        }
        
        public readonly uint GetListLength<T>(eint entity) where T : unmanaged
        {
            return GetListLength<T>(entity, out _);
        }

        public readonly void RemoveAtList<T>(eint entity, uint index) where T : unmanaged
        {
            UnsafeList* list = UnsafeWorld.GetList(value, entity, RuntimeType.Get<T>());
            UnsafeList.RemoveAt(list, index);
        }

        public readonly void DestroyList<T>(eint entity) where T : unmanaged
        {
            UnsafeWorld.DestroyList<T>(value, entity);
        }

        public readonly void DestroyList(eint entity, RuntimeType type)
        {
            UnsafeWorld.DestroyList(value, entity, type);
        }

        public readonly void ClearList<T>(eint entity) where T : unmanaged
        {
            UnsafeList* list = UnsafeWorld.GetList(value, entity, RuntimeType.Get<T>());
            UnsafeList.Clear(list);
        }

        public readonly void ClearList(eint entity, RuntimeType listType)
        {
            UnsafeList* list = UnsafeWorld.GetList(value, entity, listType);
            UnsafeList.Clear(list);
        }

        public readonly void AddComponent<T>(eint entity, T component) where T : unmanaged
        {
            //todo: efficiency: polling component chunk twice here
            RuntimeType type = RuntimeType.Get<T>();
            UnsafeWorld.AddComponent(value, entity, type);
            ref T target = ref UnsafeWorld.GetComponentRef<T>(value, entity);
            target = component;
            UnsafeWorld.NotifyComponentAdded(this, entity, type);
        }

        /// <summary>
        /// Adds a new component of the given type with uninitialized data.
        /// </summary>
        public readonly void AddComponent<T>(eint entity) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            UnsafeWorld.AddComponent(value, entity, type);
            UnsafeWorld.NotifyComponentAdded(this, entity, type);
        }

        /// <summary>
        /// Adds a new default component with the given type.
        /// </summary>
        public readonly void AddComponent(eint entity, RuntimeType componentType)
        {
            UnsafeWorld.AddComponent(value, entity, componentType);
            UnsafeWorld.NotifyComponentAdded(this, entity, componentType);
        }

        public readonly void AddComponent(eint entity, RuntimeType componentType, ReadOnlySpan<byte> componentData)
        {
            Span<byte> bytes = UnsafeWorld.AddComponent(value, entity, componentType);
            componentData.CopyTo(bytes);
            UnsafeWorld.NotifyComponentAdded(this, entity, componentType);
        }

        /// <summary>
        /// Adds a <c>default</c> component value and returns it by reference.
        /// </summary>
        public readonly ref T AddComponentRef<T>(eint entity) where T : unmanaged
        {
            AddComponent<T>(entity, default);
            return ref GetComponentRef<T>(entity);
        }

        public readonly void RemoveComponent<T>(eint entity) where T : unmanaged
        {
            UnsafeWorld.RemoveComponent<T>(value, entity);
        }

        public readonly void RemoveComponent(eint entity, RuntimeType componentType)
        {
            UnsafeWorld.RemoveComponent(value, entity, componentType);
        }

        /// <summary>
        /// Returns <c>true</c> if any entity in the world contains this component.
        /// </summary>
        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            UnmanagedDictionary<uint, ComponentChunk> components = ComponentChunks;
            RuntimeType type = RuntimeType.Get<T>();
            for (int i = 0; i < components.Count; i++)
            {
                ComponentChunk chunk = components.Values[i];
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

        public readonly bool ContainsComponent<T>(eint entity) where T : unmanaged
        {
            return ContainsComponent(entity, RuntimeType.Get<T>());
        }

        public readonly bool ContainsComponent(eint entity, RuntimeType type)
        {
            return UnsafeWorld.ContainsComponent(value, entity, type);
        }

        public readonly ref T GetComponentRef<T>(eint entity) where T : unmanaged
        {
            return ref UnsafeWorld.GetComponentRef<T>(value, entity);
        }

        public readonly ref T GetComponentRef<T>(eint entity, out bool contains) where T : unmanaged
        {
            if (!ContainsComponent<T>(entity))
            {
                contains = false;
                return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>(null);
            }

            contains = true;
            return ref UnsafeWorld.GetComponentRef<T>(value, entity);
        }

        /// <summary>
        /// Returns the component of the expected type if it exists, otherwise the default value
        /// is given.
        /// </summary>
        public readonly T GetComponent<T>(eint entity, T defaultValue) where T : unmanaged
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

        public readonly T GetComponent<T>(eint entity, out bool contains) where T : unmanaged
        {
            if (ContainsComponent<T>(entity))
            {
                contains = true;
                return GetComponentRef<T>(entity);
            }
            else
            {
                contains = false;
                return default;
            }
        }

        public readonly T GetComponent<T>(eint entity) where T : unmanaged
        {
            return GetComponentRef<T>(entity);
        }

        /// <summary>
        /// Fetches the component from this entity as a span of bytes.
        /// </summary>
        public readonly Span<byte> GetComponentBytes(eint entity, RuntimeType type)
        {
            return UnsafeWorld.GetComponentBytes(value, entity, type);
        }

        public readonly bool TryGetComponent<T>(eint entity, out T found) where T : unmanaged
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

        public readonly ref T TryGetComponentRef<T>(eint entity, out bool found) where T : unmanaged
        {
            if (ContainsComponent<T>(entity))
            {
                found = true;
                return ref GetComponentRef<T>(entity);
            }
            else
            {
                found = false;
                return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>(null);
            }
        }

        public readonly void SetComponent<T>(eint entity, T component) where T : unmanaged
        {
            ref T existing = ref GetComponentRef<T>(entity);
            existing = component;
        }

        public readonly void SetComponent(eint entity, RuntimeType componentType, ReadOnlySpan<byte> componentData)
        {
            Span<byte> bytes = GetComponentBytes(entity, componentType);
            componentData.CopyTo(bytes);
        }

        /// <summary>
        /// Returns the main component chunk that contains the given entity.
        /// </summary>
        public readonly ComponentChunk GetComponentChunk(eint entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity.value - 1];
            return ComponentChunks[slot.componentsKey];
        }

        /// <summary>
        /// Returns the main component chunk that contains all of the given component types.
        /// </summary>
        public readonly ComponentChunk GetComponentChunk(ReadOnlySpan<RuntimeType> componentTypes)
        {
            uint key = RuntimeType.CombineHash(componentTypes);
            if (ComponentChunks.TryGetValue(key, out ComponentChunk chunk))
            {
                return chunk;
            }
            else
            {
                throw new NullReferenceException($"No components found for the given types.");
            }
        }

        public readonly bool ContainsComponentChunk(ReadOnlySpan<RuntimeType> componentTypes)
        {
            uint key = RuntimeType.CombineHash(componentTypes);
            return ComponentChunks.ContainsKey(key);
        }

        public readonly bool TryGetComponentChunk(ReadOnlySpan<RuntimeType> componentTypes, out ComponentChunk chunk)
        {
            uint key = RuntimeType.CombineHash(componentTypes);
            return ComponentChunks.TryGetValue(key, out chunk);
        }

        /// <summary>
        /// Counts how many entities there are with component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly uint CountEntities<T>(Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            return CountEntities(type, options);
        }

        public readonly uint CountEntities(RuntimeType type, Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            uint count = 0;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(type))
                {
                    if (includeDisabled)
                    {
                        count += chunk.Entities.Count;
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            eint entity = chunk.Entities[e];
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
        /// Copies components from the source entity onto the destination.
        /// <para>Components will be added if the destination entity doesnt
        /// contain them. Existing component data will be overwritten.</para>
        /// </summary>
        public readonly void CopyComponentsTo(eint sourceEntity, World destinationWorld, eint destinationEntity)
        {
            foreach (RuntimeType type in GetComponentTypes(sourceEntity))
            {
                if (!destinationWorld.ContainsComponent(destinationEntity, type))
                {
                    destinationWorld.AddComponent(destinationEntity, type);
                }

                Span<byte> sourceBytes = GetComponentBytes(sourceEntity, type);
                Span<byte> destinationBytes = destinationWorld.GetComponentBytes(destinationEntity, type);
                sourceBytes.CopyTo(destinationBytes);
            }
        }

        /// <summary>
        /// Copies all lists from the source entity onto the destination.
        /// <para>Lists will be added if the destination entity doesnt
        /// contain them. Existing list data will be overwritten.</para>
        /// </summary>
        public readonly void CopyListsTo(eint sourceEntity, World destinationWorld, eint destinationEntity)
        {
            foreach (RuntimeType sourceListType in GetListTypes(sourceEntity))
            {
                UnsafeList* sourceList = GetList(sourceEntity, sourceListType);
                uint sourceLength = UnsafeList.GetCountRef(sourceList);
                if (!destinationWorld.ContainsList(destinationEntity, sourceListType))
                {
                    UnsafeList* destinationList = destinationWorld.CreateList(destinationEntity, sourceListType);
                    UnsafeList.AddDefault(destinationList, UnsafeList.GetCountRef(sourceList));
                    for (uint i = 0; i < sourceLength; i++)
                    {
                        UnsafeList.CopyElementTo(sourceList, i, destinationList, i);
                    }
                }
                else
                {
                    UnsafeList* destinationList = destinationWorld.GetList(destinationEntity, sourceListType);
                    uint destinationIndex = UnsafeList.GetCountRef(destinationList);
                    UnsafeList.AddDefault(destinationList, sourceLength);
                    for (uint i = 0; i < sourceLength; i++)
                    {
                        UnsafeList.CopyElementTo(sourceList, i, destinationList, destinationIndex + i);
                    }
                }
            }
        }

        /// <summary>
        /// Finds all entities that contain all of the given component types and
        /// adds them to the given list.
        /// </summary>
        public readonly void Fill(ReadOnlySpan<RuntimeType> componentTypes, UnmanagedList<eint> list, Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            bool exact = (options & Query.Option.ExactComponentTypes) == Query.Option.ExactComponentTypes;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.ContainsTypes(componentTypes, exact))
                {
                    if (includeDisabled)
                    {
                        list.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            eint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                list.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<T> list, Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(type))
                {
                    if (includeDisabled)
                    {
                        list.AddRange(chunk.GetComponents<T>());
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            eint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                list.Add(chunk.GetComponentRef<T>(e));
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<eint> entities, Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(type))
                {
                    if (includeDisabled)
                    {
                        entities.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            eint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<T> components, UnmanagedList<eint> entities, Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(type))
                {
                    if (includeDisabled)
                    {
                        components.AddRange(chunk.GetComponents<T>());
                        entities.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            eint entity = chunk.Entities[e];
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

        public readonly void Fill(RuntimeType componentType, UnmanagedList<eint> entities, Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(componentType))
                {
                    if (includeDisabled)
                    {
                        entities.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            eint entity = chunk.Entities[e];
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
        public readonly IEnumerable<eint> GetAll(RuntimeType componentType, Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(componentType))
                {
                    for (uint e = 0; e < chunk.Entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            yield return chunk.Entities[e];
                        }
                        else
                        {
                            eint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                yield return entity;
                            }
                        }
                    }
                }
            }
        }

        public readonly IEnumerable<eint> GetAll<T>(Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            return GetAll(RuntimeType.Get<T>(), options);
        }

        public readonly void ForEach<T>(QueryCallback callback, Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            RuntimeType componentType = RuntimeType.Get<T>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(componentType))
                {
                    for (uint e = 0; e < chunk.Entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            callback(chunk.Entities[e]);
                        }
                        else
                        {
                            eint entity = chunk.Entities[e];
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
        public readonly void ForEach(ReadOnlySpan<RuntimeType> componentTypes, QueryCallback callback, Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            bool exact = (options & Query.Option.ExactComponentTypes) == Query.Option.ExactComponentTypes;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.ContainsTypes(componentTypes, exact))
                {
                    for (uint e = 0; e < chunk.Entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            callback(chunk.Entities[e]);
                        }
                        else
                        {
                            eint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                callback(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void ForEach<T>(QueryCallback<T> callback, Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            Span<RuntimeType> types = stackalloc RuntimeType[1];
            types[0] = RuntimeType.Get<T>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.ContainsTypes(types))
                {
                    UnmanagedList<eint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            ref T t1 = ref chunk.GetComponentRef<T>(e);
                            callback(entities[e], ref t1);
                        }
                        else
                        {
                            eint entity = entities[e];
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

        public readonly void ForEach<T1, T2>(QueryCallback<T1, T2> callback, Query.Option options = Query.Option.IncludeDisabledEntities) where T1 : unmanaged where T2 : unmanaged
        {
            Span<RuntimeType> types = stackalloc RuntimeType[2];
            types[0] = RuntimeType.Get<T1>();
            types[1] = RuntimeType.Get<T2>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.ContainsTypes(types))
                {
                    UnmanagedList<eint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                            ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                            callback(entities[e], ref t1, ref t2);
                        }
                        else
                        {
                            eint entity = entities[e];
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

        public readonly void ForEach<T1, T2, T3>(QueryCallback<T1, T2, T3> callback, Query.Option options = Query.Option.IncludeDisabledEntities) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            Span<RuntimeType> types = stackalloc RuntimeType[3];
            types[0] = RuntimeType.Get<T1>();
            types[1] = RuntimeType.Get<T2>();
            types[2] = RuntimeType.Get<T3>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.ContainsTypes(types))
                {
                    UnmanagedList<eint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                            ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                            ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                            callback(entities[e], ref t1, ref t2, ref t3);
                        }
                        else
                        {
                            eint entity = entities[e];
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

        public readonly void ForEach<T1, T2, T3, T4>(QueryCallback<T1, T2, T3, T4> callback, Query.Option options = Query.Option.IncludeDisabledEntities) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            Span<RuntimeType> types = stackalloc RuntimeType[4];
            types[0] = RuntimeType.Get<T1>();
            types[1] = RuntimeType.Get<T2>();
            types[2] = RuntimeType.Get<T3>();
            types[3] = RuntimeType.Get<T4>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.ContainsTypes(types))
                {
                    UnmanagedList<eint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                            ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                            ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                            ref T4 t4 = ref chunk.GetComponentRef<T4>(e);
                            callback(entities[e], ref t1, ref t2, ref t3, ref t4);
                        }
                        else
                        {
                            eint entity = entities[e];
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

        public void CreateListener<T>(object update)
        {
            throw new NotImplementedException();
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

    public delegate void QueryCallback(in eint id);
    public delegate void QueryCallback<T1>(in eint id, ref T1 t1) where T1 : unmanaged;
    public delegate void QueryCallback<T1, T2>(in eint id, ref T1 t1, ref T2 t2) where T1 : unmanaged where T2 : unmanaged;
    public delegate void QueryCallback<T1, T2, T3>(in eint id, ref T1 t1, ref T2 t2, ref T3 t3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged;
    public delegate void QueryCallback<T1, T2, T3, T4>(in eint id, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged;

    public delegate void EntityCreatedCallback(World world, eint entity);
    public delegate void EntityDestroyedCallback(World world, eint entity);
    public delegate void EntityParentChangedCallback(World world, eint entity, eint parent);
    public delegate void ComponentAddedCallback(World world, eint entity, RuntimeType componentType);
    public delegate void ComponentRemovedCallback(World world, eint entity, RuntimeType componentType);
}
