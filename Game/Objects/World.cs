using System;
using System.Runtime.CompilerServices;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public readonly unsafe struct World : IDisposable, IEquatable<World>
    {
        internal readonly UnsafeWorld* value;

        public readonly uint ID => UnsafeWorld.GetID(value);
        public readonly uint Count => UnsafeWorld.GetEntityCount(value);
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

        public readonly void Dispose()
        {
            UnsafeWorld.Free(value);
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

            return ID == other.ID;
        }

        public readonly override int GetHashCode()
        {
            return value->GetHashCode();
        }

        public readonly void Submit<T>(T message) where T : unmanaged
        {
            UnsafeWorld.Submit(value, Container.Allocate(message));
        }

        /// <summary>
        /// Iterates over all events with <see cref="Submit"/> and
        /// notifies added listeners.
        /// </summary>
        public readonly void Poll()
        {
            UnsafeWorld.Poll(value);
        }

        public readonly void DestroyEntity(EntityID id)
        {
            UnsafeWorld.DestroyEntity(value, id);
        }

        public readonly void Listen<T>(delegate* unmanaged<World, Container, void> callback) where T : unmanaged
        {
            UnsafeWorld.Listen(value, RuntimeType.Get<T>(), callback);
        }

        public readonly ComponentTypeMask GetComponents(EntityID id)
        {
            return UnsafeWorld.GetComponents(value, id);
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
            using UnmanagedArray<bool> containsArray = new((uint)contains.Length);
            using UnmanagedArray<T> destinationArray = new((uint)destination.Length);
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
            using UnmanagedArray<T> destinationArray = new((uint)destination.Length);
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
            using UnmanagedList<EntityID> entities = GetEntities(ComponentTypeMask.Get<T>());
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

        public readonly void CreateCollection<T>(EntityID id, ReadOnlySpan<T> values) where T : unmanaged
        {
            UnmanagedList<T> list = UnsafeWorld.CreateCollection<T>(value, id);
            list.AddRange(values);
        }

        public readonly void AddToCollection<T>(EntityID id, T value) where T : unmanaged
        {
            UnsafeWorld.AddToCollection(this.value, id, value);
        }

        public readonly bool ContainsCollection<T>(EntityID id) where T : unmanaged
        {
            return UnsafeWorld.ContainsCollection<T>(value, id);
        }

        public readonly UnmanagedList<T> GetCollection<T>(EntityID id) where T : unmanaged
        {
            return UnsafeWorld.GetCollection<T>(value, id);
        }

        public readonly void RemoveAtFromCollection<T>(EntityID id, uint index) where T : unmanaged
        {
            UnsafeWorld.RemoveAtFromCollection<T>(value, id, index);
        }

        public readonly void DestroyCollection<T>(EntityID id) where T : unmanaged
        {
            UnsafeWorld.DestroyCollection<T>(value, id);
        }

        public readonly void ClearCollection<T>(EntityID id) where T : unmanaged
        {
            UnsafeWorld.ClearCollection<T>(value, id);
        }

        public readonly void AddComponent<T>(EntityID id, T component) where T : unmanaged
        {
            UnsafeWorld.AddComponent(value, id, component);
        }

        /// <summary>
        /// Adds a default component and returns it by reference for initialization.
        /// </summary>
        public readonly ref T AddComponentRef<T>(EntityID id) where T : unmanaged
        {
            AddComponent<T>(id, default);
            return ref GetComponentRef<T>(id);
        }

        public readonly void RemoveComponent<T>(EntityID id) where T : unmanaged
        {
            UnsafeWorld.RemoveComponent<T>(value, id);
        }

        public readonly bool ContainsComponent<T>(EntityID id) where T : unmanaged
        {
            return UnsafeWorld.ContainsComponent(value, id, ComponentType.Get<T>());
        }

        public readonly bool ContainsComponent(EntityID id, ComponentType type)
        {
            return UnsafeWorld.ContainsComponent(value, id, type);
        }

        public readonly ref T GetComponentRef<T>(EntityID id) where T : unmanaged
        {
            return ref UnsafeWorld.GetComponentRef<T>(value, id);
        }

        /// <summary>
        /// Returns a component of the given type if it exists, otherwise returns a default value.
        /// </summary>
        public readonly T GetComponent<T>(EntityID id, T defaultValue = default) where T : unmanaged
        {
            if (ContainsComponent<T>(id))
            {
                return GetComponentRef<T>(id);
            }
            else
            {
                return defaultValue;
            }
        }

        public readonly Span<byte> GetComponent(EntityID id, ComponentType type)
        {
            return UnsafeWorld.GetComponentBytes(value, id, type);
        }

        public readonly ref C TryGetComponentRef<C>(EntityID id, out bool found) where C : unmanaged
        {
            if (ContainsComponent<C>(id))
            {
                found = true;
                return ref GetComponentRef<C>(id);
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

        public readonly UnmanagedList<EntityID> GetEntities(ComponentTypeMask componentTypes)
        {
            UnmanagedList<EntityID> entities = new();
            QueryComponents(componentTypes, (in EntityID id) =>
            {
                entities.Add(id);
            });

            return entities;
        }

        public readonly void QueryComponents(ComponentType type, QueryCallback action)
        {
            UnsafeWorld.QueryComponents(value, ComponentTypeMask.Get(type), action);
        }

        public readonly void QueryComponents(ComponentTypeMask types, QueryCallback action)
        {
            UnsafeWorld.QueryComponents(value, types, action);
        }

        public readonly void QueryComponents<C1>(QueryCallback action) where C1 : unmanaged
        {
            UnsafeWorld.QueryComponents<C1>(value, action);
        }

        public readonly void QueryComponents<C1>(QueryCallback<C1> action) where C1 : unmanaged
        {
            UnsafeWorld.QueryComponents(value, action);
        }

        public readonly void QueryComponents<C1, C2>(QueryCallback<C1, C2> action) where C1 : unmanaged where C2 : unmanaged
        {
            UnsafeWorld.QueryComponents(value, action);
        }

        public readonly void QueryComponents<C1, C2, C3>(QueryCallback<C1, C2, C3> action) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            UnsafeWorld.QueryComponents(value, action);
        }

        public readonly void QueryComponents<C1, C2, C3, C4>(QueryCallback<C1, C2, C3, C4> action) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
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
