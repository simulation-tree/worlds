using Game.ECS;
using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public readonly unsafe struct World : IDisposable, IEquatable<World>
    {
        internal readonly UnmanagedWorld* value;

        public readonly uint ID => UnmanagedWorld.GetID(value);
        public readonly uint Count => UnmanagedWorld.GetCount(value);
        public readonly bool IsDisposed => UnmanagedWorld.IsDisposed(value);

        /// <summary>
        /// Creates a new disposable world.
        /// </summary>
        public World()
        {
            value = UnmanagedWorld.Create();
        }

        internal World(UnmanagedWorld* pointer)
        {
            this.value = pointer;
        }

        public readonly void Dispose()
        {
            UnmanagedWorld.Dispose(value);
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

        public readonly void SubmitEvent<T>(T message) where T : unmanaged
        {
            UnmanagedWorld.SubmitEvent(value, Container.Allocate(message));
        }

        public readonly void AddListener<T>(Listener<T> listener) where T : unmanaged
        {
            UnmanagedWorld.AddListener(value, listener);
        }

        public readonly void RemoveListener<T>(Listener<T> listener) where T : unmanaged
        {
            UnmanagedWorld.RemoveListener(value, listener);
        }

        public readonly void AddListener(RuntimeType type, Listener listener)
        {
            UnmanagedWorld.AddListener(value, type, listener);
        }

        public readonly void RemoveListener(RuntimeType type, Listener listener)
        {
            UnmanagedWorld.RemoveListener(value, type, listener);
        }

        /// <summary>
        /// Iterates over all events with <see cref="SubmitEvent"/> and
        /// notifies added listeners.
        /// </summary>
        public readonly void PollListeners()
        {
            UnmanagedWorld.PollListeners(value);
        }

        public readonly void DestroyEntity(EntityID id)
        {
            UnmanagedWorld.DestroyEntity(value, id);
        }

        public readonly ComponentTypeMask GetComponents(EntityID id)
        {
            return UnmanagedWorld.GetComponents(value, id);
        }

        public readonly EntityID CreateEntity()
        {
            return CreateEntity(default);
        }

        public readonly EntityID CreateEntity(ComponentTypeMask componentTypes)
        {
            return UnmanagedWorld.CreateEntity(value, componentTypes);
        }

        public readonly bool ContainsEntity(EntityID id)
        {
            return UnmanagedWorld.ContainsEntity(value, id);
        }

        public readonly UnmanagedList<T> CreateCollection<T>(EntityID id) where T : unmanaged
        {
            return UnmanagedWorld.CreateCollection<T>(value, id);
        }

        public readonly Span<T> CreateCollection<T>(EntityID id, uint initialCount) where T : unmanaged
        {
            return UnmanagedWorld.CreateCollection<T>(value, id, initialCount);
        }

        public readonly void AddToCollection<T>(EntityID id, T value) where T : unmanaged
        {
            UnmanagedWorld.AddToCollection(this.value, id, value);
        }

        public readonly bool ContainsCollection<T>(EntityID id) where T : unmanaged
        {
            return UnmanagedWorld.ContainsCollection<T>(value, id);
        }

        public readonly UnmanagedList<T> GetCollection<T>(EntityID id) where T : unmanaged
        {
            return UnmanagedWorld.GetCollection<T>(value, id);
        }

        public readonly void RemoveAtFromCollection<T>(EntityID id, uint index) where T : unmanaged
        {
            UnmanagedWorld.RemoveAtFromCollection<T>(value, id, index);
        }

        public readonly void DestroyCollection<T>(EntityID id) where T : unmanaged
        {
            UnmanagedWorld.DestroyCollection<T>(value, id);
        }

        public readonly void ClearCollection<T>(EntityID id) where T : unmanaged
        {
            UnmanagedWorld.ClearCollection<T>(value, id);
        }

        public readonly void AddComponent<T>(EntityID id, T component) where T : unmanaged
        {
            UnmanagedWorld.AddComponent(value, id, component);
        }

        public readonly void RemoveComponent<T>(EntityID id) where T : unmanaged
        {
            UnmanagedWorld.RemoveComponent<T>(value, id);
        }

        public readonly bool ContainsComponent<C>(EntityID id) where C : unmanaged
        {
            return UnmanagedWorld.ContainsComponent<C>(value, id);
        }

        public readonly bool ContainsComponent(EntityID id, ComponentType type)
        {
            return UnmanagedWorld.ContainsComponent(value, id, type);
        }

        public readonly ref C GetComponentRef<C>(EntityID id) where C : unmanaged
        {
            return ref UnmanagedWorld.GetComponentRef<C>(value, id);
        }

        public readonly C GetComponent<C>(EntityID id, C defaultValue = default) where C : unmanaged
        {
            return UnmanagedWorld.GetComponent(value, id, defaultValue);
        }

        public readonly Span<byte> GetComponent(EntityID id, ComponentType type)
        {
            return UnmanagedWorld.GetComponent(value, id, type);
        }

        public readonly ref C TryGetComponentRef<C>(EntityID id, out bool found) where C : unmanaged
        {
            return ref UnmanagedWorld.TryGetComponentRef<C>(value, id, out found);
        }

        public readonly ReadOnlySpan<EntityID> GetEntities(ComponentTypeMask componentTypes)
        {
            return UnmanagedWorld.GetEntities(value, componentTypes);
        }

        public readonly void QueryComponents(ComponentType type, QueryCallback action)
        {
            UnmanagedWorld.QueryComponents(value, type, action);
        }

        public readonly void QueryComponents<C1>(QueryCallback action) where C1 : unmanaged
        {
            UnmanagedWorld.QueryComponents<C1>(value, action);
        }

        public readonly void QueryComponents<C1>(QueryCallback<C1> action) where C1 : unmanaged
        {
            UnmanagedWorld.QueryComponents(value, action);
        }

        public readonly void QueryComponents<C1, C2>(QueryCallback<C1, C2> action) where C1 : unmanaged where C2 : unmanaged
        {
            UnmanagedWorld.QueryComponents(value, action);
        }

        public readonly void QueryComponents<C1, C2, C3>(QueryCallback<C1, C2, C3> action) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            UnmanagedWorld.QueryComponents(value, action);
        }

        public readonly void QueryComponents<C1, C2, C3, C4>(QueryCallback<C1, C2, C3, C4> action) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            UnmanagedWorld.QueryComponents(value, action);
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

    public delegate void Listener<T>(ref T message) where T : unmanaged;
    public delegate void Listener(ref Container message);

    public struct EntityDescription(EntityID id, uint version, ComponentTypeMask componentTypes, CollectionTypeMask collectionTypes)
    {
        public EntityID id = id;
        public uint version = version;
        public ComponentTypeMask componentTypes = componentTypes;
        public CollectionTypeMask collectionTypes = collectionTypes;
    }

    public unsafe delegate void CreatedCallback(UnmanagedWorld* world, EntityID id);
    public unsafe delegate void DestroyedCallback(UnmanagedWorld* world, EntityID id);
}
