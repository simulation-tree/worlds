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

        public readonly bool IsDisposed => UnsafeWorld.IsDisposed(value);
        public readonly UnmanagedList<EntityDescription> Slots => UnsafeWorld.GetEntitySlots(value);
        public readonly UnmanagedList<EntityID> Free => UnsafeWorld.GetFreeEntities(value);
        public readonly UnmanagedDictionary<uint, ComponentChunk> ComponentChunks => UnsafeWorld.GetComponentChunks(value);

        /// <summary>
        /// All entities that exist in the world.
        /// </summary>
        public readonly IEnumerable<EntityID> Entities
        {
            get
            {
                for (uint i = 0; i < Slots.Count; i++)
                {
                    EntityDescription description = Slots[i];
                    if (!Free.Contains(description.entity))
                    {
                        yield return new(description.entity);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new disposable world.
        /// </summary>
        public World()
        {
            value = UnsafeWorld.Allocate();
        }

        internal World(UnsafeWorld* pointer)
        {
            this.value = pointer;
        }

        public void Dispose()
        {
            UnsafeWorld.Free(ref value);
        }

        /// <summary>
        /// Resets the world to default state.
        /// </summary>
        public readonly void Clear()
        {
            UnsafeWorld.Clear(value);
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

        void ISerializable.Write(BinaryWriter writer)
        {
            using UnmanagedList<RuntimeType> uniqueTypes = new();
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
                Type systemType = type.Type;
                ReadOnlySpan<char> aqn = systemType.AssemblyQualifiedName.AsSpan();
                writer.WriteValue((uint)aqn.Length);
                writer.WriteUTF8Span(aqn);
                writer.WriteValue(type);
            }

            writer.WriteValue(Count);
            for (uint i = 0; i < Slots.Count; i++)
            {
                EntityDescription slot = Slots[i];
                if (!Free.Contains(slot.entity))
                {
                    EntityID entity = new(slot.entity);
                    writer.WriteValue(entity.value);
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
            using UnmanagedList<RuntimeType> uniqueTypes = new();
            Span<char> buffer = stackalloc char[256];
            for (uint i = 0; i < typeCount; i++)
            {
                uint aqnLength = reader.ReadValue<uint>();
                reader.ReadUTF8Span(buffer[..(int)aqnLength]);
#pragma warning disable IL2057
                string aqn = new(buffer[..(int)aqnLength]);
                Type systemType = Type.GetType(aqn, true) ?? throw new InvalidOperationException($"Type {aqn} not found.");
#pragma warning restore IL2057
                RuntimeType type = reader.ReadValue<RuntimeType>();
                uniqueTypes.Add(type);
            }

            uint entityCount = reader.ReadValue<uint>();
            uint currentEntityId = 1;
            using UnmanagedList<EntityID> temporaryEntities = new();
            for (uint i = 0; i < entityCount; i++)
            {
                uint entityId = reader.ReadValue<uint>();
                uint catchup = entityId - currentEntityId;
                for (uint j = 0; j < catchup; j++)
                {
                    EntityID temporaryEntity = CreateEntity();
                    temporaryEntities.Add(temporaryEntity);
                }

                EntityID entity = CreateEntity();
                uint componentCount = reader.ReadValue<uint>();
                for (uint j = 0; j < componentCount; j++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    RuntimeType type = uniqueTypes[typeIndex];
                    ReadOnlySpan<byte> componentBytes = reader.ReadSpan<byte>(type.Size);
                    void* component = UnsafeWorld.AddComponent(value, entity, type);
                    Span<byte> destinationBytes = new(component, type.Size);
                    componentBytes.CopyTo(destinationBytes);
                }

                uint collectionCount = reader.ReadValue<uint>();
                for (uint j = 0; j < collectionCount; j++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    RuntimeType type = uniqueTypes[typeIndex];
                    uint listCount = reader.ReadValue<uint>();
                    uint byteCount = listCount * type.Size;
                    UnsafeList* list = UnsafeWorld.CreateCollection(value, entity, type, listCount);
                    UnsafeList.AddDefault(list, listCount);
                    nint address = UnsafeList.GetAddress(list);
                    Span<byte> destinationBytes = new((void*)address, (int)byteCount);
                    ReadOnlySpan<byte> sourceBytes = reader.ReadSpan<byte>(byteCount);
                    sourceBytes.CopyTo(destinationBytes);
                }

                currentEntityId = entityId + 1;
            }

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
            foreach (EntityDescription slot in world.Slots)
            {
                if (!world.Free.Contains(slot.entity))
                {
                    EntityID entity = CreateEntity();
                    ComponentChunk chunk = world.ComponentChunks[slot.componentsKey];
                    for (int j = 0; j < chunk.Types.Length; j++)
                    {
                        RuntimeType type = chunk.Types[j];
                        Span<byte> bytes = chunk.GetComponentBytes(new(slot.entity), type);
                        void* component = UnsafeWorld.AddComponent(value, entity, type);
                        Span<byte> destination = new(component, type.Size);
                        bytes.CopyTo(destination);
                    }

                    if (!slot.collections.IsDisposed)
                    {
                        for (int j = 0; j < slot.collections.Types.Length; j++)
                        {
                            RuntimeType type = slot.collections.Types[j];
                            UnsafeList* list = slot.collections.GetCollection(type);
                            uint count = UnsafeList.GetCountRef(list);
                            UnsafeList* destination = UnsafeWorld.CreateCollection(value, entity, type, count);
                            nint address = UnsafeList.GetAddress(destination);
                            Span<byte> destinationBytes = new((void*)address, (int)(count * type.Size));
                            nint sourceAddress = UnsafeList.GetAddress(list);
                            Span<byte> sourceBytes = new((void*)sourceAddress, (int)(count * type.Size));
                            sourceBytes.CopyTo(destinationBytes);
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

        /// <summary>
        /// Destroys the given entity assuming it exists.
        /// </summary>
        public readonly void DestroyEntity(EntityID entity, bool destroyChildren = true)
        {
            UnsafeWorld.DestroyEntity(value, entity, destroyChildren);
        }

        public readonly Listener CreateListener<T>(delegate* unmanaged<World, Container, void> callback) where T : unmanaged
        {
            return CreateListener(RuntimeType.Get<T>(), callback);
        }

        public readonly Listener CreateListener(RuntimeType eventType, delegate* unmanaged<World, Container, void> callback)
        {
            return UnsafeWorld.CreateListener(value, eventType, callback);
        }

        public readonly ReadOnlySpan<RuntimeType> GetComponents(EntityID entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity.value - 1];
            ComponentChunk chunk = ComponentChunks[slot.componentsKey];
            return chunk.Types;
        }

        public readonly bool IsEnabled(EntityID entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity.value - 1];
            return slot.IsEnabled;
        }

        public readonly void SetEnabledState(EntityID entity, bool value)
        {
            UnsafeWorld.ThrowIfEntityMissing(this.value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity.value - 1);
            slot.SetEnabledState(value);
        }

        public readonly bool TryGetFirst<T>(out T found) where T : unmanaged
        {
            return TryGetFirst(out _, out found);
        }

        public readonly bool TryGetFirst<T>(out EntityID entity) where T : unmanaged
        {
            return TryGetFirst<T>(out entity, out _);
        }

        public readonly bool TryGetFirst<T>(out EntityID entity, out T component) where T : unmanaged
        {
            foreach (EntityID e in GetAll(RuntimeType.Get<T>()))
            {
                entity = e;
                component = GetComponentRef<T>(e);
                return true;
            }

            entity = default;
            component = default;
            return false;
        }

        public readonly EntityID CreateEntity(EntityID parent = default)
        {
            EntityID entity = GetNextEntity();
            CreateEntity(entity, parent);
            return entity;
        }

        /// <summary>
        /// Returns the value for the next created entity.
        /// </summary>
        public readonly EntityID GetNextEntity()
        {
            return UnsafeWorld.GetNextEntity(value);
        }

        /// <summary>
        /// Creates an entity with the given value assuming its 
        /// not already in use.
        /// </summary>
        public readonly void CreateEntity(EntityID value, EntityID parent)
        {
            UnsafeWorld.InitializeEntity(this.value, value, parent, default);
        }

        /// <summary>
        /// Checks if the entity exists and is valid in this world.
        /// </summary>
        public readonly bool ContainsEntity(EntityID entity)
        {
            return UnsafeWorld.ContainsEntity(value, entity.value);
        }

        /// <summary>
        /// Retrieves the parent of the given entity, <c>default</c> if none
        /// is assigned.
        /// </summary>
        public readonly EntityID GetParent(EntityID entity)
        {
            return UnsafeWorld.GetParent(value, entity);
        }

        /// <summary>
        /// Assigns a new parent.
        /// </summary>
        /// <returns><c>true</c> if the given parent entity was found and assigned successfuly.</returns>
        public readonly bool SetParent(EntityID entity, EntityID parent)
        {
            return UnsafeWorld.SetParent(value, entity, parent);
        }

        /// <summary>
        /// Retreives all children of the given entity.
        /// </summary>
        public readonly ReadOnlySpan<EntityID> GetChildren(EntityID entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity.value - 1];
            if (!slot.children.IsDisposed)
            {
                return new((void*)slot.children.Address, (int)slot.children.Count);
            }
            else return [];
        }

        public readonly UnmanagedList<T> CreateCollection<T>(EntityID entity) where T : unmanaged
        {
            return CreateCollection<T>(entity, 1);
        }

        /// <summary>
        /// Creates a new collection of the given count and returns it as a span.
        /// </summary>
        public readonly UnmanagedList<T> CreateCollection<T>(EntityID entity, uint initialCapacity = 1) where T : unmanaged
        {
            UnmanagedList<T> list = new(UnsafeWorld.CreateCollection(value, entity, RuntimeType.Get<T>(), initialCapacity));
            return list;
        }

        public readonly void CreateCollection<T>(EntityID entity, ReadOnlySpan<T> values) where T : unmanaged
        {
            UnmanagedList<T> list = new(UnsafeWorld.CreateCollection(value, entity, RuntimeType.Get<T>()));
            list.AddRange(values);
        }

        public readonly void AddToCollection<T>(EntityID entity, T value) where T : unmanaged
        {
            UnmanagedList<T> list = new(UnsafeWorld.GetCollection(this.value, entity, RuntimeType.Get<T>()));
            list.Add(value);
        }

        public readonly bool ContainsCollection<T>(EntityID entity) where T : unmanaged
        {
            return UnsafeWorld.ContainsCollection<T>(value, entity);
        }

        public readonly void AddRangeToCollection<T>(EntityID entity, ReadOnlySpan<T> values) where T : unmanaged
        {
            UnmanagedList<T> list = new(UnsafeWorld.GetCollection(value, entity, RuntimeType.Get<T>()));
            list.AddRange(values);
        }

        /// <summary>
        /// Retrieves a collection of type <typeparamref name="T"/> on the given entity.
        /// </summary>
        public readonly UnmanagedList<T> GetCollection<T>(EntityID entity) where T : unmanaged
        {
            return new(UnsafeWorld.GetCollection(value, entity, RuntimeType.Get<T>()));
        }

        public readonly void RemoveAtCollection<T>(EntityID entity, uint index) where T : unmanaged
        {
            UnsafeList* list = UnsafeWorld.GetCollection(value, entity, RuntimeType.Get<T>());
            UnsafeList.RemoveAt(list, index);
        }

        public readonly void DestroyCollection<T>(EntityID entity) where T : unmanaged
        {
            UnsafeWorld.DestroyCollection<T>(value, entity);
        }

        public readonly void ClearCollection<T>(EntityID entity) where T : unmanaged
        {
            UnsafeList* list = UnsafeWorld.GetCollection(value, entity, RuntimeType.Get<T>());
            UnsafeList.Clear(list);
        }

        public readonly void AddComponent<T>(EntityID entity, T component) where T : unmanaged
        {
            UnsafeWorld.AddComponent(value, entity, RuntimeType.Get<T>());
            ref T target = ref UnsafeWorld.GetComponentRef<T>(value, entity);
            target = component;
        }

        /// <summary>
        /// Adds a <c>default</c> component value and returns it by reference.
        /// </summary>
        public readonly ref T AddComponentRef<T>(EntityID entity) where T : unmanaged
        {
            AddComponent<T>(entity, default);
            return ref GetComponentRef<T>(entity);
        }

        public readonly void RemoveComponent<T>(EntityID entity) where T : unmanaged
        {
            UnsafeWorld.RemoveComponent<T>(value, entity);
        }

        /// <summary>
        /// Returns <c>true</c> if any entity exists with the given component type.
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

        public readonly bool ContainsComponent<T>(EntityID entity) where T : unmanaged
        {
            return ContainsComponent(entity, RuntimeType.Get<T>());
        }

        public readonly bool ContainsComponent(EntityID entity, RuntimeType type)
        {
            return UnsafeWorld.ContainsComponent(value, entity, type);
        }

        public readonly ref T GetComponentRef<T>(EntityID entity) where T : unmanaged
        {
            return ref UnsafeWorld.GetComponentRef<T>(value, entity);
        }

        public readonly ref T GetComponentRef<T>(EntityID entity, out bool contains) where T : unmanaged
        {
            if (!ContainsComponent<T>(entity))
            {
                contains = false;
                return ref System.Runtime.CompilerServices.Unsafe.NullRef<T>();
            }

            contains = true;
            return ref UnsafeWorld.GetComponentRef<T>(value, entity);
        }

        /// <summary>
        /// Returns the component of the expected type if it exists, otherwise the default value
        /// is given.
        /// </summary>
        public readonly T GetComponent<T>(EntityID entity, T defaultValue) where T : unmanaged
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

        public readonly T GetComponent<T>(EntityID entity, out bool contains) where T : unmanaged
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

        public readonly T GetComponent<T>(EntityID entity) where T : unmanaged
        {
            return GetComponentRef<T>(entity);
        }

        public readonly Span<byte> GetComponentBytes(EntityID entity, RuntimeType type)
        {
            return UnsafeWorld.GetComponentBytes(value, entity, type);
        }

        public readonly bool TryGetComponent<T>(EntityID entity, out T found) where T : unmanaged
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

        public readonly ref T TryGetComponentRef<T>(EntityID entity, out bool found) where T : unmanaged
        {
            if (ContainsComponent<T>(entity))
            {
                found = true;
                return ref GetComponentRef<T>(entity);
            }
            else
            {
                found = false;
                return ref System.Runtime.CompilerServices.Unsafe.NullRef<T>();
            }
        }

        public readonly void SetComponent<T>(EntityID id, T component) where T : unmanaged
        {
            ref T existing = ref GetComponentRef<T>(id);
            existing = component;
        }

        /// <summary>
        /// Returns the main component chunk that contains the given entity.
        /// </summary>
        public readonly ComponentChunk GetComponentChunk(EntityID entity)
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
            uint key = RuntimeType.CalculateHash(componentTypes);
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
            uint key = RuntimeType.CalculateHash(componentTypes);
            return ComponentChunks.ContainsKey(key);
        }

        public readonly bool TryGetComponentChunk(ReadOnlySpan<RuntimeType> componentTypes, out ComponentChunk chunk)
        {
            uint key = RuntimeType.CalculateHash(componentTypes);
            return ComponentChunks.TryGetValue(key, out chunk);
        }

        /// <summary>
        /// Counts how many entities there are with component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly uint CountEntities<T>(Query.Option options = default) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            return CountEntities(type, options);
        }

        public readonly uint CountEntities(RuntimeType type, Query.Option options = default)
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
                            EntityID entity = chunk.Entities[e];
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
        /// Finds all entities that contain all of the given component types and
        /// adds them to the given list.
        /// </summary>
        public readonly void Fill(ReadOnlySpan<RuntimeType> componentTypes, UnmanagedList<EntityID> list, Query.Option options = default)
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
                            EntityID entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                list.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<T> list, Query.Option options = default) where T : unmanaged
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
                            EntityID entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                list.Add(chunk.GetComponentRef<T>(e));
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<EntityID> entities, Query.Option options = default) where T : unmanaged
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
                            EntityID entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<T> components, UnmanagedList<EntityID> entities, Query.Option options = default) where T : unmanaged
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
                            EntityID entity = chunk.Entities[e];
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

        public readonly void Fill(RuntimeType componentType, UnmanagedList<EntityID> entities, Query.Option options = default)
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
                            EntityID entity = chunk.Entities[e];
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
        public readonly IEnumerable<EntityID> GetAll(RuntimeType componentType, Query.Option options = default)
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
                            EntityID entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                yield return entity;
                            }
                        }
                    }
                }
            }
        }

        public readonly IEnumerable<EntityID> GetAll<T>(Query.Option options = default) where T : unmanaged
        {
            return GetAll(RuntimeType.Get<T>(), options);
        }

        public readonly void ForEach<T>(QueryCallback callback, Query.Option options = default) where T : unmanaged
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
                            EntityID entity = chunk.Entities[e];
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
        public readonly void ForEach(ReadOnlySpan<RuntimeType> componentTypes, QueryCallback callback, Query.Option options = default)
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
                            EntityID entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                callback(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void ForEach<T1>(QueryCallback<T1> callback, Query.Option options = default) where T1 : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T1>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(type))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                            callback(entities[e], ref t1);
                        }
                        else
                        {
                            EntityID entity = entities[e];
                            if (IsEnabled(entity))
                            {
                                ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                                callback(entity, ref t1);
                            }
                        }
                    }
                }
            }
        }

        public readonly void ForEach<T1, T2>(QueryCallback<T1, T2> callback, Query.Option options = default) where T1 : unmanaged where T2 : unmanaged
        {
            ReadOnlySpan<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>()];
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.ContainsTypes(types))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
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
                            EntityID entity = entities[e];
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

        public readonly void ForEach<T1, T2, T3>(QueryCallback<T1, T2, T3> callback, Query.Option options = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            ReadOnlySpan<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>()];
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.ContainsTypes(types))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
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
                            EntityID entity = entities[e];
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

        public readonly void ForEach<T1, T2, T3, T4>(QueryCallback<T1, T2, T3, T4> callback, Query.Option options = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            ReadOnlySpan<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>()];
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.ContainsTypes(types))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
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
                            EntityID entity = entities[e];
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

        public static bool operator ==(World left, World right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(World left, World right)
        {
            return !(left == right);
        }
    }

    public delegate void QueryCallback(in EntityID id);
    public delegate void QueryCallback<T1>(in EntityID id, ref T1 t1) where T1 : unmanaged;
    public delegate void QueryCallback<T1, T2>(in EntityID id, ref T1 t1, ref T2 t2) where T1 : unmanaged where T2 : unmanaged;
    public delegate void QueryCallback<T1, T2, T3>(in EntityID id, ref T1 t1, ref T2 t2, ref T3 t3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged;
    public delegate void QueryCallback<T1, T2, T3, T4>(in EntityID id, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged;

    public unsafe delegate void CreatedCallback(World world, EntityID id);
    public unsafe delegate void DestroyedCallback(World world, EntityID id);
}
