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
            //write info about the type tree
            using UnmanagedList<RuntimeType> uniqueTypes = UnmanagedList<RuntimeType>.Create();
            for (int a = 0; a < ComponentChunks.Count; a++)
            {
                ComponentChunk chunk = ComponentChunks.Values[a];
                for (int b = 0; b < chunk.Types.Length; b++)
                {
                    RuntimeType type = chunk.Types[b];
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
                    for (int j = 0; j < slot.collections.Types.Length; j++)
                    {
                        RuntimeType type = slot.collections.Types[j];
                        if (!uniqueTypes.Contains(type))
                        {
                            uniqueTypes.Add(type);
                        }
                    }
                }
            }

            writer.WriteValue(uniqueTypes.Count);
            for (uint a = 0; a < uniqueTypes.Count; a++)
            {
                RuntimeType type = uniqueTypes[a];
                writer.WriteValue(type);
            }

            writer.WriteValue(Count);
            for (uint i = 0; i < Slots.Count; i++)
            {
                EntityDescription slot = Slots[i];
                eint entity = new(slot.entity);
                if (!Free.Contains(entity))
                {
                    writer.WriteValue(entity);
                    writer.WriteValue(slot.parent);
                    ComponentChunk chunk = ComponentChunks[slot.componentsKey];
                    writer.WriteValue((uint)chunk.Types.Length);
                    for (int j = 0; j < chunk.Types.Length; j++)
                    {
                        RuntimeType type = chunk.Types[j];
                        writer.WriteValue(uniqueTypes.IndexOf(type));
                        Span<byte> componentBytes = chunk.GetComponentBytes(entity, type);
                        writer.WriteSpan<byte>(componentBytes);
                    }

                    if (slot.collections.IsDisposed)
                    {
                        writer.WriteValue(0u);
                    }
                    else
                    {
                        writer.WriteValue((uint)slot.collections.Types.Length);
                        for (int j = 0; j < slot.collections.Types.Length; j++)
                        {
                            RuntimeType type = slot.collections.Types[j];
                            writer.WriteValue(uniqueTypes.IndexOf(type));
                            UnsafeList* list = slot.collections.GetCollection(type);
                            uint listCount = UnsafeList.GetCountRef(list);
                            writer.WriteValue(listCount);
                            nint address = UnsafeList.GetAddress(list);
                            Span<byte> bytes = new((void*)address, (int)(listCount * type.Size));
                            writer.WriteSpan<byte>(bytes);
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
                    ref EntityDescription slot = ref Slots.GetRef(entity - 1);
                    slot.parent = parentId;
                    //todo: lifecycle fault: missing invokation of UnsafeWorld.ParentAssigned
                }

                uint componentCount = reader.ReadValue<uint>();
                for (uint j = 0; j < componentCount; j++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    RuntimeType type = uniqueTypes[typeIndex];
                    ReadOnlySpan<byte> componentBytes = reader.ReadSpan<byte>(type.Size);
                    void* component = UnsafeWorld.AddComponent(value, entity, type);
                    Span<byte> destinationBytes = new(component, type.Size);
                    componentBytes.CopyTo(destinationBytes);
                    UnsafeWorld.NotifyComponentAdded(this, entity, type);
                }

                uint collectionCount = reader.ReadValue<uint>();
                for (uint j = 0; j < collectionCount; j++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    RuntimeType type = uniqueTypes[typeIndex];
                    uint listCount = reader.ReadValue<uint>();
                    uint byteCount = listCount * type.Size;
                    UnsafeList* list = UnsafeWorld.CreateList(value, entity, type, listCount == 0 ? 1 : listCount);
                    UnsafeList.AddDefault(list, listCount);
                    nint address = UnsafeList.GetAddress(list);
                    Span<byte> destinationBytes = new((void*)address, (int)byteCount);
                    ReadOnlySpan<byte> sourceBytes = reader.ReadSpan<byte>(byteCount);
                    sourceBytes.CopyTo(destinationBytes);
                }

                currentEntityId = entityId + 1;
            }

            //assign children
            foreach (eint entity in Entities)
            {
                eint parent = GetParent(entity);
                if (parent != default)
                {
                    ref EntityDescription parentSlot = ref Slots.GetRef(parent - 1);
                    if (parentSlot.children == default)
                    {
                        parentSlot.children = UnmanagedList<uint>.Create();
                    }

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
        public readonly void Append(World world)
        {
            foreach (EntityDescription sourceSlot in world.Slots)
            {
                eint sourceEntity = new(sourceSlot.entity);
                if (!world.Free.Contains(sourceEntity))
                {
                    eint destinationEntity = CreateEntity();
                    ComponentChunk chunk = world.ComponentChunks[sourceSlot.componentsKey];
                    for (int j = 0; j < chunk.Types.Length; j++)
                    {
                        RuntimeType type = chunk.Types[j];
                        Span<byte> bytes = chunk.GetComponentBytes(sourceEntity, type);
                        void* component = UnsafeWorld.AddComponent(value, destinationEntity, type);
                        Span<byte> destination = new(component, type.Size);
                        bytes.CopyTo(destination);
                        UnsafeWorld.NotifyComponentAdded(this, destinationEntity, type);
                    }

                    if (!sourceSlot.collections.IsDisposed)
                    {
                        for (int j = 0; j < sourceSlot.collections.Types.Length; j++)
                        {
                            RuntimeType type = sourceSlot.collections.Types[j];
                            UnsafeList* sourceList = sourceSlot.collections.GetCollection(type);
                            uint count = UnsafeList.GetCountRef(sourceList);
                            UnsafeList* destinationList = UnsafeWorld.CreateList(value, destinationEntity, type, count + 1);
                            for (uint e = 0; e < count; e++)
                            {
                                UnsafeList.CopyElementTo(sourceList, e, destinationList, e);
                            }
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

        public readonly void Perform(ReadOnlySpan<Command> commands)
        {
            using UnmanagedList<eint> selection = UnmanagedList<eint>.Create();
            using UnmanagedList<eint> entities = UnmanagedList<eint>.Create();
            for (int c = 0; c < commands.Length; c++)
            {
                Command command = commands[c];
                if (command.Operation == CommandOperation.CreateEntity)
                {
                    uint count = (uint)command.A;
                    selection.Clear();
                    for (uint i = 0; i < count; i++)
                    {
                        eint newEntity = CreateEntity();
                        selection.Add(newEntity);
                        entities.Add(newEntity);
                    }
                }
                else if (command.Operation == CommandOperation.DestroyEntities)
                {
                    uint start = (uint)command.A;
                    uint count = (uint)command.B;
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
                else if (command.Operation == CommandOperation.ClearSelection)
                {
                    selection.Clear();
                }
                else if (command.Operation == CommandOperation.AddToSelection)
                {
                    if (command.A == 0)
                    {
                        uint relativeOffset = (uint)command.B;
                        eint entity = entities[(entities.Count - 1) - relativeOffset];
                        selection.Add(entity);
                    }
                    else if (command.A == 1)
                    {
                        eint entity = new((uint)command.B);
                        selection.Add(entity);
                    }
                }
                else if (command.Operation == CommandOperation.SelectEntity)
                {
                    if (command.A == 0)
                    {
                        uint relativeOffset = (uint)command.B;
                        eint entity = entities[(entities.Count - 1) - relativeOffset];
                        selection.Clear();
                        selection.Add(entity);
                    }
                    else if (command.A == 1)
                    {
                        eint entity = new((uint)command.B);
                        selection.Clear();
                        selection.Add(entity);
                    }
                }
                else if (command.Operation == CommandOperation.SetParent)
                {
                    if (command.A == 0)
                    {
                        uint relativeOffset = (uint)command.B;
                        eint parent = entities[(entities.Count - 1) - relativeOffset];
                        for (uint i = 0; i < selection.Count; i++)
                        {
                            eint entity = selection[i];
                            SetParent(entity, parent);
                        }
                    }
                    else if (command.A == 1)
                    {
                        eint parent = new((uint)command.B);
                        for (uint i = 0; i < selection.Count; i++)
                        {
                            eint entity = selection[i];
                            SetParent(entity, parent);
                        }
                    }
                }
                else if (command.Operation == CommandOperation.AddComponent)
                {
                    RuntimeType componentType = new((uint)command.A);
                    Allocation allocation = new((void*)(nint)command.B);
                    Span<byte> componentData = allocation.AsSpan<byte>(0, componentType.Size);
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        AddComponent(entity, componentType, componentData);
                    }

                    allocation.Dispose();
                }
                else if (command.Operation == CommandOperation.RemoveComponent)
                {
                    RuntimeType componentType = new((uint)command.A);
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        RemoveComponent(entity, componentType);
                    }
                }
                else if (command.Operation == CommandOperation.SetComponent)
                {
                    RuntimeType componentType = new((uint)command.A);
                    Allocation allocation = new((void*)(nint)command.B);
                    Span<byte> componentBytes = allocation.AsSpan<byte>(0, componentType.Size);
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        SetComponent(entity, componentType, componentBytes);
                    }

                    allocation.Dispose();
                }
                else if (command.Operation == CommandOperation.CreateList)
                {
                    RuntimeType listType = new((uint)command.A);
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        CreateList(entity, listType);
                    }
                }
                else if (command.Operation == CommandOperation.DestroyList)
                {
                    RuntimeType listType = new((uint)command.A);
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        DestroyList(entity, listType);
                    }
                }
                else if (command.Operation == CommandOperation.ClearList)
                {
                    RuntimeType listType = new((uint)command.A);
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        ClearList(entity, listType);
                    }
                }
                else if (command.Operation == CommandOperation.InsertElement)
                {
                    RuntimeType listType = new((uint)command.A);
                    Allocation allocation = new((void*)(nint)command.B);
                    uint index = (uint)command.C;
                    Span<byte> elementBytes = allocation.AsSpan<byte>(0, listType.Size);
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        UnsafeList* list = GetList(entity, listType);
                        if (index == uint.MaxValue)
                        {
                            UnsafeList.Add(list, elementBytes);
                        }
                        else
                        {
                            UnsafeList.Insert(list, index, elementBytes);
                        }
                    }

                    allocation.Dispose();
                }
                else if (command.Operation == CommandOperation.RemoveElement)
                {
                    RuntimeType listType = new((uint)command.A);
                    uint index = (uint)command.B;
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        UnsafeList* list = GetList(entity, listType);
                        UnsafeList.RemoveAt(list, index);
                    }
                }
                else if (command.Operation == CommandOperation.ModifyElement)
                {
                    RuntimeType listType = new((uint)command.A);
                    Allocation allocation = new((void*)(nint)command.B);
                    uint index = (uint)command.C;
                    Span<byte> elementBytes = allocation.AsSpan<byte>(0, listType.Size);
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        eint entity = selection[i];
                        UnsafeList* list = GetList(entity, listType);
                        Span<byte> slotBytes = UnsafeList.GetElementBytes(list, index);
                        elementBytes.CopyTo(slotBytes);
                    }

                    allocation.Dispose();
                }
                else
                {
                    throw new NotImplementedException($"Unknown command instruction: {command.Operation}");
                }
            }
        }

        public readonly void Perform(Span<Command> commands)
        {
            ReadOnlySpan<Command> commandsReadOnly = commands;
            Perform(commandsReadOnly);
        }

        public readonly void Perform(IEnumerable<Command> commands)
        {
            using UnmanagedList<Command> list = UnmanagedList<Command>.Create();
            foreach (Command command in commands)
            {
                list.Add(command);
            }

            Perform(list.AsSpan());
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
            EntityDescription slot = Slots[entity - 1];
            ComponentChunk chunk = ComponentChunks[slot.componentsKey];
            return chunk.Types;
        }

        /// <summary>
        /// Retrieves the types for all lists on this entity.
        /// </summary>
        public readonly ReadOnlySpan<RuntimeType> GetListTypes(eint entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            return slot.collections.Types;
        }

        public readonly bool IsEnabled(eint entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            return slot.IsEnabled;
        }

        public readonly void SetEnabledState(eint entity, bool value)
        {
            UnsafeWorld.ThrowIfEntityMissing(this.value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity - 1);
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
            CreateEntity(entity, parent);
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
        public readonly void CreateEntity(eint value, eint parent)
        {
            UnsafeWorld.InitializeEntity(this.value, value, parent, Array.Empty<RuntimeType>());
        }

        /// <summary>
        /// Checks if the entity exists and is valid in this world.
        /// </summary>
        public readonly bool ContainsEntity(uint entity)
        {
            return UnsafeWorld.ContainsEntity(value, entity);
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
            EntityDescription slot = Slots[entity - 1];
            if (!slot.children.IsDisposed)
            {
                return new((void*)slot.children.Address, (int)slot.children.Count);
            }
            else return Array.Empty<eint>();
        }

        public readonly UnmanagedList<T> CreateList<T>(eint entity) where T : unmanaged
        {
            return CreateList<T>(entity, 1);
        }

        public readonly UnsafeList* CreateList(eint entity, RuntimeType listType)
        {
            return UnsafeWorld.CreateList(value, entity, listType);
        }

        /// <summary>
        /// Creates a new collection of the given count and returns it as a span.
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

        public readonly UnsafeList* GetList(eint entity, RuntimeType listType)
        {
            return UnsafeWorld.GetList(value, entity, listType);
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
            UnsafeWorld.AddComponent(value, entity, componentType);
            Span<byte> bytes = UnsafeWorld.GetComponentBytes(value, entity, componentType);
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

        public readonly void SetComponent<T>(eint id, T component) where T : unmanaged
        {
            ref T existing = ref GetComponentRef<T>(id);
            existing = component;
        }

        public readonly void SetComponent(eint id, RuntimeType componentType, ReadOnlySpan<byte> componentData)
        {
            Span<byte> bytes = GetComponentBytes(id, componentType);
            componentData.CopyTo(bytes);
        }

        /// <summary>
        /// Returns the main component chunk that contains the given entity.
        /// </summary>
        public readonly ComponentChunk GetComponentChunk(eint entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
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
