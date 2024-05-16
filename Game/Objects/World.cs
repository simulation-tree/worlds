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
            //UnmanagedList<UnsafeWorld.EntityDescription> slots = UnsafeWorld.GetEntitySlots(value);
            //UnmanagedList<EntityID> free = UnsafeWorld.GetFreeEntities(value);
            //uint id = UnsafeWorld.GetID(value);
            //uint entityCount = slots.Count - free.Count;
            //writer.WriteValue(entityCount);
            //for (uint i = 0; i < slots.Count; i++)
            //{
            //    UnsafeWorld.EntityDescription description = slots[i];
            //    EntityID entity = description.id;
            //    if (UnsafeWorld.ContainsEntity(value, entity))
            //    {
            //        writer.WriteValue(entity.value);
            //    }
            //}
            //
            //UnmanagedDictionary<int, nint> components = UnsafeWorld.GetComponentArchetypes(value);
            //writer.WriteValue(components.Count);
            //for (uint a = 0; a < components.Count; a++)
            //{
            //    UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)components.Values[(int)a];
            //    UnmanagedList<EntityID> componentEntities = UnsafeComponentChunk.GetEntities(chunk);
            //    writer.WriteValue(typeMask.value);
            //    writer.WriteValue(componentEntities.Count);
            //    for (uint e = 0; e < componentEntities.Count; e++)
            //    {
            //        EntityID entity = componentEntities[e];
            //        writer.WriteValue(entity.value);
            //    }
            //
            //    for (uint t = 0; t < ComponentType.MaxTypes; t++)
            //    {
            //        ComponentType type = new(t + 1);
            //        if (typeMask.Contains(type))
            //        {
            //            UnsafeList* componentList = data.GetComponents(type);
            //            Span<byte> listBytes = UnsafeList.AsSpan<byte>(componentList, 0, componentEntities.Count /* /type.RuntimeType.size);
            //            writer.WriteSpan<byte>(listBytes);
            //        }
            //    }
            //}
        }

        void IDeserializable.Deserialize(ref BinaryReader reader)
        {
            //value = UnsafeWorld.Allocate();
            //uint id = UnsafeWorld.GetID(value);
            //uint entityCount = reader.ReadValue<uint>();
            //uint currentEntityId = 1;
            //using UnmanagedList<EntityID> temporaryEntities = new();
            //for (uint i = 0; i < entityCount; i++)
            //{
            //    uint entityId = reader.ReadValue<uint>();
            //    uint catchup = entityId - currentEntityId;
            //    for (uint j = 0; j < catchup; j++)
            //    {
            //        EntityID temporaryEntity = UnsafeWorld.CreateEntity(value, default);
            //        temporaryEntities.Add(temporaryEntity);
            //    }
            //
            //    UnsafeWorld.CreateEntity(value, default);
            //    currentEntityId = entityId + 1;
            //}
            //
            //for (uint i = 0; i < temporaryEntities.Count; i++)
            //{
            //    UnsafeWorld.DestroyEntity(value, temporaryEntities[i]);
            //}
            //
            //UnmanagedDictionary<int, nint> components = UnsafeWorld.GetComponentArchetypes(value);
            //uint componentArchetypeCount = reader.ReadValue<uint>();
            //for (uint i = 0; i < componentArchetypeCount; i++)
            //{
            //    ComponentTypeMask typeMask = new(reader.ReadValue<ulong>());
            //    uint componentEntityCount = reader.ReadValue<uint>();
            //    CollectionOfComponents components = new(typeMask);
            //    if (componentsMap.TryGetValue(typeMask, out var existingMap))
            //    {
            //        existingMap.Dispose();
            //        componentsMap[typeMask] = components;
            //    }
            //    else
            //    {
            //        componentsMap.Add(typeMask, components);
            //        archetypes.Add(typeMask);
            //    }
            //
            //    for (uint j = 0; j < componentEntityCount; j++)
            //    {
            //        EntityID entity = new(reader.ReadValue<uint>());
            //        components.Add(entity);
            //    }
            //
            //    for (uint t = 0; t < ComponentType.MaxTypes; t++)
            //    {
            //        ComponentType type = new(t + 1);
            //        if (typeMask.Contains(type))
            //        {
            //            RuntimeType runtimeType = type.RuntimeType;
            //            UnsafeList* componentList = components.GetComponents(type);
            //            ReadOnlySpan<byte> listBytes = reader.ReadSpan<byte>(runtimeType.size * //componentEntityCount);
            //            UnsafeList.AddDefault(componentList, componentEntityCount);
            //            for (uint c = 0; c < componentEntityCount; c++)
            //            {
            //                Span<byte> elementBytes = UnsafeList.GetBytes(componentList, c);
            //                listBytes.CopyTo(elementBytes);
            //            }
            //        }
            //    }
            //}
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

        public readonly UnmanagedList<T> CreateCollection<T>(EntityID id) where T : unmanaged
        {
            return UnsafeWorld.CreateCollection<T>(value, id);
        }

        /// <summary>
        /// Creates a new collection of the given count and returns it as a span.
        /// </summary>
        public readonly Span<T> CreateCollection<T>(EntityID id, uint initialCount) where T : unmanaged
        {
            UnmanagedList<T> list = UnsafeWorld.CreateCollection<T>(value, id);
            list.AddDefault(initialCount);
            return list.AsSpan();
        }

        public readonly void CreateCollection<T>(EntityID entity, ReadOnlySpan<T> values) where T : unmanaged
        {
            UnmanagedList<T> list = UnsafeWorld.CreateCollection<T>(value, entity);
            list.AddRange(values);
        }

        public readonly void AddToCollection<T>(EntityID entity, T value) where T : unmanaged
        {
            UnmanagedList<T> list = UnsafeWorld.GetCollection<T>(this.value, entity);
            list.Add(value);
        }

        public readonly bool ContainsCollection<T>(EntityID entity) where T : unmanaged
        {
            return UnsafeWorld.ContainsCollection<T>(value, entity);
        }

        public readonly UnmanagedList<T> GetCollection<T>(EntityID entity) where T : unmanaged
        {
            return UnsafeWorld.GetCollection<T>(value, entity);
        }

        public readonly void RemoveAtFromCollection<T>(EntityID entity, uint index) where T : unmanaged
        {
            UnmanagedList<T> list = UnsafeWorld.GetCollection<T>(this.value, entity);
            list.RemoveAt(index);
        }

        public readonly void DestroyCollection<T>(EntityID entity) where T : unmanaged
        {
            UnsafeWorld.DestroyCollection<T>(value, entity);
        }

        public readonly void ClearCollection<T>(EntityID entity) where T : unmanaged
        {
            UnmanagedList<T> list = UnsafeWorld.GetCollection<T>(this.value, entity);
            list.Clear();
        }

        public readonly void AddComponent<T>(EntityID entity, T component) where T : unmanaged
        {
            ref T target = ref UnsafeWorld.AddComponentRef<T>(value, entity);
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
