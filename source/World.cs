﻿using Game.Unsafe;
using System;
using System.Collections;
using System.Collections.Generic;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public unsafe struct World : IDisposable, IEquatable<World>, IBinaryObject
    {
        internal UnsafeWorld* value;

        public readonly uint ID => UnsafeWorld.GetID(value);

        /// <summary>
        /// Amount of entities that exist in the world.
        /// </summary>
        public readonly uint Count => Slots.Count - Free.Count;

        public readonly bool IsDisposed => UnsafeWorld.IsDisposed(value);
        public readonly UnmanagedList<EntityDescription> Slots => UnsafeWorld.GetEntitySlots(value);
        public readonly UnmanagedList<EntityID> Free => UnsafeWorld.GetFreeEntities(value);
        public readonly UnmanagedDictionary<int, ComponentChunk> ComponentChunks => UnsafeWorld.GetComponentChunks(value);

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
                        yield return description.entity;
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

        public readonly override string ToString()
        {
            return $"World {ID}";
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

            return ID == other.ID;
        }

        public readonly override int GetHashCode()
        {
            return value->GetHashCode();
        }

        void IBinaryObject.Write(BinaryWriter writer)
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
                for (uint b = 0; b < aqn.Length; b++)
                {
                    writer.WriteValue(aqn[(int)b]);
                }

                writer.WriteValue(type.AsRawValue());
            }

            writer.WriteValue(Count);
            for (uint i = 0; i < Slots.Count; i++)
            {
                EntityDescription slot = Slots[i];
                if (!Free.Contains(slot.entity))
                {
                    EntityID entity = slot.entity;
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
                            uint listCount = UnsafeList.GetCount(list);
                            writer.WriteValue(listCount);
                            nint address = UnsafeList.GetAddress(list);
                            Span<byte> bytes = new((void*)address, (int)(listCount * type.size));
                            writer.WriteSpan<byte>(bytes);
                        }
                    }
                }
            }
        }

        void IBinaryObject.Read(ref BinaryReader reader)
        {
            value = UnsafeWorld.Allocate();
            uint typeCount = reader.ReadValue<uint>();
            using UnmanagedList<RuntimeType> uniqueTypes = new();
            for (uint i = 0; i < typeCount; i++)
            {
                uint aqnLength = reader.ReadValue<uint>();
                ReadOnlySpan<char> aqn = reader.ReadSpan<char>(aqnLength);
#pragma warning disable IL2057
                Type systemType = Type.GetType(aqn.ToString(), true) ?? throw new InvalidOperationException($"Type {aqn.ToString()} not found.");
#pragma warning restore IL2057
                uint rawValue = reader.ReadValue<uint>();
                RuntimeType type = new(rawValue);
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
                    EntityID temporaryEntity = UnsafeWorld.CreateEntity(value, default);
                    temporaryEntities.Add(temporaryEntity);
                }

                EntityID entity = UnsafeWorld.CreateEntity(value, default);
                uint componentCount = reader.ReadValue<uint>();
                for (uint j = 0; j < componentCount; j++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    RuntimeType type = uniqueTypes[typeIndex];
                    ReadOnlySpan<byte> componentBytes = reader.ReadSpan<byte>(type.size);
                    void* component = UnsafeWorld.AddComponent(value, entity, type);
                    Span<byte> destinationBytes = new(component, type.size);
                    componentBytes.CopyTo(destinationBytes);
                }

                uint collectionCount = reader.ReadValue<uint>();
                for (uint j = 0; j < collectionCount; j++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    RuntimeType type = uniqueTypes[typeIndex];
                    uint listCount = reader.ReadValue<uint>();
                    uint byteCount = listCount * type.size;
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
                        Span<byte> bytes = chunk.GetComponentBytes(slot.entity, type);
                        void* component = UnsafeWorld.AddComponent(value, entity, type);
                        Span<byte> destination = new(component, type.size);
                        bytes.CopyTo(destination);
                    }

                    if (!slot.collections.IsDisposed)
                    {
                        for (int j = 0; j < slot.collections.Types.Length; j++)
                        {
                            RuntimeType type = slot.collections.Types[j];
                            UnsafeList* list = slot.collections.GetCollection(type);
                            uint count = UnsafeList.GetCount(list);
                            UnsafeList* destination = UnsafeWorld.CreateCollection(value, entity, type, count);
                            nint address = UnsafeList.GetAddress(destination);
                            Span<byte> destinationBytes = new((void*)address, (int)(count * type.size));
                            nint sourceAddress = UnsafeList.GetAddress(list);
                            Span<byte> sourceBytes = new((void*)sourceAddress, (int)(count * type.size));
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

        /// <summary>
        /// Iterates over all events that we submitted with <see cref="Submit"/>,
        /// and notifies all registered listeners.
        /// </summary>
        public readonly void Poll()
        {
            UnsafeWorld.Poll(value);
        }

        public readonly void DestroyEntity(IEntity entity)
        {
            DestroyEntity(entity.Value);
        }

        public readonly void DestroyEntity(EntityID id)
        {
            UnsafeWorld.DestroyEntity(value, id);
        }

        public readonly Listener Listen<T>(delegate* unmanaged<World, Container, void> callback) where T : unmanaged
        {
            return Listen(RuntimeType.Get<T>(), callback);
        }

        public readonly Listener Listen(RuntimeType eventType, delegate* unmanaged<World, Container, void> callback)
        {
            return UnsafeWorld.Listen(value, eventType, callback);
        }

        public readonly ListenerWithContext Listen(nint pointer, RuntimeType eventType, delegate* unmanaged<nint, World, Container, void> callback)
        {
            return UnsafeWorld.Listen(value, pointer, eventType, callback);
        }

        public readonly ReadOnlySpan<RuntimeType> GetComponents(IEntity entity)
        {
            return GetComponents(entity.Value);
        }

        public readonly ReadOnlySpan<RuntimeType> GetComponents(EntityID entity)
        {
            UnsafeWorld.ThrowIfEntityMissing(value, entity);
            EntityDescription slot = Slots[entity.value - 1];
            ComponentChunk chunk = ComponentChunks[slot.componentsKey];
            return chunk.Types;
        }

        public readonly bool TryGetFirst<T>(out T found) where T : unmanaged
        {
            return TryGetFirst(out _, out found);
        }

        public readonly bool TryGetFirst<T>(out EntityID entity, out T component) where T : unmanaged
        {
            foreach (EntityID e in Query(RuntimeType.Get<T>()))
            {
                entity = e;
                component = GetComponentRef<T>(e);
                return true;
            }

            entity = default;
            component = default;
            return false;
        }

        public readonly EntityID CreateEntity()
        {
            return UnsafeWorld.CreateEntity(value, default);
        }

        public readonly bool ContainsEntity(IEntity entity)
        {
            return ContainsEntity(entity.Value);
        }

        public readonly bool ContainsEntity(EntityID entity)
        {
            return UnsafeWorld.ContainsEntity(value, entity.value);
        }

        public readonly UnmanagedList<T> CreateCollection<T>(IEntity entity) where T : unmanaged
        {
            return CreateCollection<T>(entity.Value);
        }

        public readonly UnmanagedList<T> CreateCollection<T>(EntityID entity) where T : unmanaged
        {
            return new(UnsafeWorld.CreateCollection(value, entity, RuntimeType.Get<T>()));
        }

        public readonly Span<T> CreateCollection<T>(IEntity entity, uint initialCount) where T : unmanaged
        {
            return CreateCollection<T>(entity.Value, initialCount);
        }

        /// <summary>
        /// Creates a new collection of the given count and returns it as a span.
        /// </summary>
        public readonly Span<T> CreateCollection<T>(EntityID entity, uint initialCount) where T : unmanaged
        {
            UnmanagedList<T> list = new(UnsafeWorld.CreateCollection(value, entity, RuntimeType.Get<T>()));
            list.AddDefault(initialCount);
            return list.AsSpan();
        }

        public readonly void CreateCollection<T>(IEntity entity, ReadOnlySpan<T> values) where T : unmanaged
        {
            CreateCollection(entity.Value, values);
        }

        public readonly void CreateCollection<T>(EntityID entity, ReadOnlySpan<T> values) where T : unmanaged
        {
            UnmanagedList<T> list = new(UnsafeWorld.CreateCollection(value, entity, RuntimeType.Get<T>()));
            list.AddRange(values);
        }

        public readonly void AddToCollection<T>(IEntity entity, T value) where T : unmanaged
        {
            AddToCollection(entity.Value, value);
        }

        public readonly void AddToCollection<T>(EntityID entity, T value) where T : unmanaged
        {
            UnmanagedList<T> list = new(UnsafeWorld.GetCollection(this.value, entity, RuntimeType.Get<T>()));
            list.Add(value);
        }

        public readonly void AddRangeToCollection<T>(IEntity entity, ReadOnlySpan<T> values) where T : unmanaged
        {
            AddRangeToCollection(entity.Value, values);
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

        public readonly UnmanagedList<T> GetCollection<T>(EntityID entity) where T : unmanaged
        {
            return new(UnsafeWorld.GetCollection(value, entity, RuntimeType.Get<T>()));
        }

        public readonly void RemoveAtCollection<T>(IEntity entity, uint index) where T : unmanaged
        {
            RemoveAtCollection<T>(entity.Value, index);
        }

        public readonly void RemoveAtCollection<T>(EntityID entity, uint index) where T : unmanaged
        {
            UnsafeList* list = UnsafeWorld.GetCollection(value, entity, RuntimeType.Get<T>());
            UnsafeList.RemoveAt(list, index);
        }

        public readonly void DestroyCollection<T>(IEntity entity) where T : unmanaged
        {
            DestroyCollection<T>(entity.Value);
        }

        public readonly void DestroyCollection<T>(EntityID entity) where T : unmanaged
        {
            UnsafeWorld.DestroyCollection<T>(value, entity);
        }

        public readonly void ClearCollection<T>(IEntity entity) where T : unmanaged
        {
            ClearCollection<T>(entity.Value);
        }

        public readonly void ClearCollection<T>(EntityID entity) where T : unmanaged
        {
            UnsafeList* list = UnsafeWorld.GetCollection(value, entity, RuntimeType.Get<T>());
            UnsafeList.Clear(list);
        }

        public readonly void AddComponent<T>(IEntity entity, T component) where T : unmanaged
        {
            AddComponent(entity.Value, component);
        }

        public readonly void AddComponent<T>(EntityID entity, T component) where T : unmanaged
        {
            UnsafeWorld.AddComponent(value, entity, RuntimeType.Get<T>());
            ref T target = ref UnsafeWorld.GetComponentRef<T>(value, entity);
            target = component;
        }

        public readonly ref T AddComponentRef<T>(IEntity entity) where T : unmanaged
        {
            return ref AddComponentRef<T>(entity.Value);
        }

        /// <summary>
        /// Adds a <c>default</c> component value and returns it by reference.
        /// </summary>
        public readonly ref T AddComponentRef<T>(EntityID entity) where T : unmanaged
        {
            AddComponent<T>(entity, default);
            return ref GetComponentRef<T>(entity);
        }

        public readonly void RemoveComponent<T>(IEntity entity) where T : unmanaged
        {
            RemoveComponent<T>(entity.Value);
        }

        public readonly void RemoveComponent<T>(EntityID entity) where T : unmanaged
        {
            UnsafeWorld.RemoveComponent<T>(value, entity);
        }

        public readonly bool ContainsComponent<T>(IEntity entity) where T : unmanaged
        {
            return ContainsComponent<T>(entity.Value);
        }

        public readonly bool ContainsComponent<T>(EntityID entity) where T : unmanaged
        {
            return ContainsComponent(entity, RuntimeType.Get<T>());
        }

        public readonly bool ContainsComponent(IEntity entity, RuntimeType type)
        {
            return ContainsComponent(entity.Value, type);
        }

        public readonly bool ContainsComponent(EntityID entity, RuntimeType type)
        {
            return UnsafeWorld.ContainsComponent(value, entity, type);
        }

        public readonly ref T GetComponentRef<T>(IEntity entity) where T : unmanaged
        {
            return ref GetComponentRef<T>(entity.Value);
        }

        public readonly ref T GetComponentRef<T>(EntityID entity) where T : unmanaged
        {
            return ref UnsafeWorld.GetComponentRef<T>(value, entity);
        }

        public readonly T GetComponent<T>(IEntity entity, T defaultValue) where T : unmanaged
        {
            return GetComponent(entity.Value, defaultValue);
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

        public readonly T GetComponent<T>(EntityID entity) where T : unmanaged
        {
            return GetComponentRef<T>(entity);
        }

        public readonly Span<byte> GetComponentBytes(EntityID entity, RuntimeType type)
        {
            return UnsafeWorld.GetComponentBytes(value, entity, type);
        }

        public readonly bool TryGetComponent<T>(IEntity entity, out T found) where T : unmanaged
        {
            return TryGetComponent(entity.Value, out found);
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

        public readonly ref T TryGetComponentRef<T>(IEntity entity, out bool found) where T : unmanaged
        {
            return ref TryGetComponentRef<T>(entity.Value, out found);
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

        public readonly void SetComponent<T>(IEntity id, T component) where T : unmanaged
        {
            SetComponent(id.Value, component);
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
            int key = RuntimeType.CalculateHash(componentTypes);
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
            int key = RuntimeType.CalculateHash(componentTypes);
            return ComponentChunks.ContainsKey(key);
        }

        public readonly bool TryGetComponentChunk(ReadOnlySpan<RuntimeType> componentTypes, out ComponentChunk chunk)
        {
            int key = RuntimeType.CalculateHash(componentTypes);
            return ComponentChunks.TryGetValue(key, out chunk);
        }

        /// <summary>
        /// Finds all entities that contain all of the given component types and
        /// adds them to the given list.
        /// </summary>
        public readonly void Fill(ReadOnlySpan<RuntimeType> componentTypes, UnmanagedList<EntityID> list)
        {
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.ContainsTypes(componentTypes))
                {
                    list.AddRange(chunk.Entities.AsSpan());
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<T> list) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(type))
                {
                    UnmanagedList<T> components = chunk.GetComponents<T>();
                    list.AddRange(components.AsSpan());
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<EntityID> entities) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(type))
                {
                    entities.AddRange(chunk.Entities.AsSpan());
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<T> components, UnmanagedList<EntityID> entities) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(type))
                {
                    components.AddRange(chunk.GetComponents<T>().AsSpan());
                    var entitiesSpan = chunk.Entities.AsSpan();
                    entities.AddRange(entitiesSpan);
                }
            }
        }

        public readonly void Fill(RuntimeType componentType, UnmanagedList<EntityID> entities)
        {
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(componentType))
                {
                    entities.AddRange(chunk.Entities.AsSpan());
                }
            }
        }

        /// <summary>
        /// Iterates over all entities that contain the given component type.
        /// </summary>
        public readonly IEnumerable<EntityID> Query(RuntimeType componentType)
        {
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.Types.Contains(componentType))
                {
                    for (uint j = 0; j < chunk.Entities.Count; j++)
                    {
                        yield return chunk.Entities[j];
                    }
                }
            }
        }

        public readonly IEnumerable<EntityID> Query<T>() where T : unmanaged
        {
            return Query(RuntimeType.Get<T>());
        }

        /// <summary>
        /// Finds all entities that contain all of the given component types
        /// and invokes the callback for every entity found.
        /// <para>
        /// Destroying entities inside the callback is not recommended.
        /// </para>
        /// </summary>
        public readonly void Query(ReadOnlySpan<RuntimeType> componentTypes, Action<EntityID> callback)
        {
            for (int i = 0; i < ComponentChunks.Count; i++)
            {
                ComponentChunk chunk = ComponentChunks.Values[i];
                if (chunk.ContainsTypes(componentTypes))
                {
                    for (uint j = 0; j < chunk.Entities.Count; j++)
                    {
                        callback(chunk.Entities[j]);
                    }
                }
            }
        }

        public readonly void QueryComponents<T1>(QueryCallback<T1> action) where T1 : unmanaged
        {
            UnsafeWorld.QueryComponents(value, action);
        }

        public readonly void QueryComponents<T1, T2>(QueryCallback<T1, T2> action) where T1 : unmanaged where T2 : unmanaged
        {
            UnsafeWorld.QueryComponents(value, action);
        }

        public readonly void QueryComponents<T1, T2, T3>(QueryCallback<T1, T2, T3> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            UnsafeWorld.QueryComponents(value, action);
        }

        public readonly void QueryComponents<T1, T2, T3, T4>(QueryCallback<T1, T2, T3, T4> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            UnsafeWorld.QueryComponents(value, action);
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

    public delegate void ListenerCallback<T>(ref T message) where T : unmanaged;
    public delegate void ListenerCallback(ref Container message);

    public unsafe delegate void CreatedCallback(UnsafeWorld* world, EntityID id);
    public unsafe delegate void DestroyedCallback(UnsafeWorld* world, EntityID id);
}
