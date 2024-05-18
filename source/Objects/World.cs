using Game.ECS;
using System;
using System.Runtime.CompilerServices;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public unsafe struct World : IDisposable, IEquatable<World>, ISerializable, IDeserializable
    {
        internal UnsafeWorld* value;

        public readonly uint ID => UnsafeWorld.GetID(value);

        /// <summary>
        /// Amount of entities in the world.
        /// </summary>
        public readonly uint Count
        {
            get
            {
                uint slotCount = UnsafeWorld.GetEntitySlots(value).Count;
                uint freeCount = UnsafeWorld.GetFreeEntities(value).Count;
                return slotCount - freeCount;
            }
        }

        public readonly bool IsDisposed => UnsafeWorld.IsDisposed(value);

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

        void ISerializable.Serialize(BinaryWriter writer)
        {
            UnmanagedList<UnsafeWorld.EntityDescription> slots = UnsafeWorld.GetEntitySlots(value);
            UnmanagedList<EntityID> free = UnsafeWorld.GetFreeEntities(value);
            UnmanagedDictionary<int, ComponentChunk> components = UnsafeWorld.GetComponentChunks(value);
            using UnmanagedList<RuntimeType> uniqueTypes = new();
            for (int a = 0; a < components.Count; a++)
            {
                ComponentChunk chunk = components.Values[a];
                for (int b = 0; b < chunk.Types.Length; b++)
                {
                    RuntimeType type = chunk.Types[b];
                    if (!uniqueTypes.Contains(type))
                    {
                        uniqueTypes.Add(type);
                    }
                }
            }

            for (uint i = 0; i < slots.Count; i++)
            {
                UnsafeWorld.EntityDescription slot = slots[i];
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

            uint entityCount = slots.Count - free.Count;
            writer.WriteValue(entityCount);
            for (uint i = 0; i < slots.Count; i++)
            {
                UnsafeWorld.EntityDescription slot = slots[i];
                EntityID entity = slot.id;
                if (ContainsEntity(entity))
                {
                    writer.WriteValue(entity.value);
                    ComponentChunk chunk = components[slot.componentsKey];
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

        void IDeserializable.Deserialize(ref BinaryReader reader)
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
            UnmanagedList<UnsafeWorld.EntityDescription> slots = UnsafeWorld.GetEntitySlots(world.value);
            UnmanagedDictionary<int, ComponentChunk> components = UnsafeWorld.GetComponentChunks(world.value);
            for (uint i = 0; i < slots.Count; i++)
            {
                UnsafeWorld.EntityDescription slot = slots[i];
                if (world.ContainsEntity(slot.id))
                {
                    EntityID entity = CreateEntity();
                    ComponentChunk chunk = components[slot.componentsKey];
                    for (int j = 0; j < chunk.Types.Length; j++)
                    {
                        RuntimeType type = chunk.Types[j];
                        Span<byte> bytes = chunk.GetComponentBytes(slot.id, type);
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

        public readonly ReadOnlySpan<RuntimeType> GetComponents(EntityID entity)
        {
            return UnsafeWorld.GetComponents(value, entity);
        }

        /// <summary>
        /// Finds components for every entity given and writes them into the same index.
        /// </summary>
        /// <returns>Amount of components found and copied into destination span.</returns>
        public readonly void ReadComponents<T>(ReadOnlySpan<EntityID> entities, Span<T> destination, Span<bool> contains) where T : unmanaged
        {
            contains.Clear();
            destination.Clear();

            using UnmanagedArray<EntityID> entityArray = new(entities);
            using UnmanagedArray<bool> containsArray = new((uint)entities.Length);
            using UnmanagedArray<T> destinationArray = new((uint)entities.Length);
            UnsafeWorld.QueryComponents(value, (in EntityID id, ref T component) =>
            {
                for (uint i = 0; i < entityArray.Length; i++)
                {
                    if (entityArray[i] == id)
                    {
                        destinationArray[i] = component;
                        containsArray[i] = true;
                    }
                }
            });

            destinationArray.CopyTo(destination);
            containsArray.CopyTo(contains);
        }

        public readonly void ReadComponents<T>(ReadOnlySpan<EntityID> entities, Span<T> destination) where T : unmanaged
        {
            using UnmanagedArray<EntityID> entityArray = new(entities);
            using UnmanagedArray<T> destinationArray = new((uint)entities.Length);
            UnsafeWorld.QueryComponents(value, (in EntityID id, ref T component) =>
            {
                for (uint i = 0; i < entityArray.Length; i++)
                {
                    if (entityArray[i] == id)
                    {
                        destinationArray[i] = component;
                    }
                }
            });

            destinationArray.CopyTo(destination);
        }

        public readonly bool TryGetFirst<T>(out T found) where T : unmanaged
        {
            return TryGetFirst(out _, out found);
        }

        public readonly bool TryGetFirst<T>(out EntityID id, out T found) where T : unmanaged
        {
            using UnmanagedList<EntityID> entities = new();
            ReadEntities([RuntimeType.Get<T>()], entities);
            if (entities.Count > 0)
            {
                id = entities[0];
                found = UnsafeWorld.GetComponentRef<T>(value, id);
                return true;
            }
            else
            {
                id = default;
                found = default;
                return false;
            }
        }

        public readonly EntityID CreateEntity()
        {
            return UnsafeWorld.CreateEntity(value, default);
        }

        public readonly bool ContainsEntity(EntityID id)
        {
            return UnsafeWorld.ContainsEntity(value, id);
        }

        public readonly UnmanagedList<T> CreateCollection<T>(EntityID entity) where T : unmanaged
        {
            return new(UnsafeWorld.CreateCollection(value, entity, RuntimeType.Get<T>()));
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

        public readonly UnmanagedList<T> GetCollection<T>(EntityID entity) where T : unmanaged
        {
            return new(UnsafeWorld.GetCollection(value, entity, RuntimeType.Get<T>()));
        }

        public readonly void RemoveAtFromCollection<T>(EntityID entity, uint index) where T : unmanaged
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
        /// Adds a default component and returns it by reference for initialization.
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

        public readonly bool ContainsComponent<T>(EntityID entity) where T : unmanaged
        {
            return ContainsComponent(entity, RuntimeType.Get<T>());
        }

        public readonly bool ContainsComponent(EntityID entity, RuntimeType type)
        {
            return UnsafeWorld.ContainsComponent(value, entity, type);
        }

        public readonly ref T GetComponentRef<T>(EntityID id) where T : unmanaged
        {
            return ref UnsafeWorld.GetComponentRef<T>(value, id);
        }

        /// <summary>
        /// Returns the component of the expected type if it exists, otherwise the default value
        /// is given.
        /// </summary>
        public readonly T GetComponent<T>(EntityID entity, T defaultValue = default) where T : unmanaged
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

        public readonly ref C TryGetComponentRef<C>(EntityID entity, out bool found) where C : unmanaged
        {
            if (ContainsComponent<C>(entity))
            {
                found = true;
                return ref GetComponentRef<C>(entity);
            }
            else
            {
                found = false;
                return ref Unsafe.AsRef<C>(null);
            }
        }

        public readonly void SetComponent<T>(EntityID id, T component) where T : unmanaged
        {
            ref T existing = ref GetComponentRef<T>(id);
            existing = component;
        }

        public readonly void ReadEntities(ReadOnlySpan<RuntimeType> types, UnmanagedList<EntityID> list)
        {
            QueryComponents(types, (in EntityID id) =>
            {
                list.Add(id);
            });
        }

        public readonly void QueryComponents(RuntimeType type, QueryCallback action)
        {
            UnsafeWorld.QueryComponents(value, [type], action);
        }

        public readonly void QueryComponents(ReadOnlySpan<RuntimeType> types, QueryCallback action)
        {
            UnsafeWorld.QueryComponents(value, types, action);
        }

        public readonly void QueryComponents(QueryCallback action)
        {
            UnmanagedList<UnsafeWorld.EntityDescription> slots = UnsafeWorld.GetEntitySlots(value);
            for (uint i = 0; i < slots.Count; i++)
            {
                UnsafeWorld.EntityDescription description = slots[i];
                EntityID id = description.id;
                if (ContainsEntity(id))
                {
                    action(id);
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
