using System;
using System.Diagnostics;
using Unmanaged.Collections;

namespace Game
{
    //todo: find a use for this, otherwise remove it
    public readonly unsafe struct Entity : IDisposable
    {
        public readonly EntityID id;
        public readonly World world;

        private readonly delegate* unmanaged<World, EntityID, void> destroyedCallback;

        public readonly bool IsDisposed => !world.ContainsEntity(id);

        public Entity(World world, delegate* unmanaged<World, EntityID, void> destroyedCallback)
        {
            id = world.CreateEntity();
            this.world = world;
            this.destroyedCallback = destroyedCallback;
            UnmanagedWorld.EntityDestroyed += OnDestroyed;
        }

        internal Entity(EntityID id, World world)
        {
            this.id = id;
            this.world = world;
            destroyedCallback = null;
        }

        public readonly void Dispose()
        {
            ThrowIfDisposed();

            world.DestroyEntity(id);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Entity));
            }
        }

        private readonly void OnDestroyed(UnmanagedWorld* value, EntityID id)
        {
            if (this.id == id && world.value == value)
            {
                UnmanagedWorld.EntityDestroyed -= OnDestroyed;
                destroyedCallback(world, id);
            }
        }

        public readonly void AddComponent<T>(T component) where T : unmanaged
        {
            ThrowIfDisposed();

            world.AddComponent(id, component);
        }

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            return world.ContainsComponent<T>(id);
        }

        public readonly ref T GetComponentRef<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            return ref world.GetComponentRef<T>(id);
        }

        public readonly ref T TryGetComponentRef<T>(out bool found) where T : unmanaged
        {
            ThrowIfDisposed();

            return ref world.TryGetComponentRef<T>(id, out found);
        }

        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            world.RemoveComponent<T>(id);
        }

        public readonly bool ContainsCollection<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            return world.ContainsCollection<T>(id);
        }

        public readonly Span<T> CreateCollection<T>(uint count) where T : unmanaged
        {
            ThrowIfDisposed();

            return world.CreateCollection<T>(id, count);
        }

        public readonly UnmanagedList<T> CreateCollection<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            return world.CreateCollection<T>(id);
        }

        public readonly UnmanagedList<T> GetCollection<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            return world.GetCollection<T>(id);
        }

        public readonly void DestroyCollection<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            world.DestroyCollection<T>(id);
        }

        public readonly void AddToCollection<T>(T value) where T : unmanaged
        {
            ThrowIfDisposed();

            world.AddToCollection(id, value);
        }

        public readonly void RemoveAtFromCollection<T>(uint index) where T : unmanaged
        {
            ThrowIfDisposed();

            world.RemoveAtFromCollection<T>(id, index);
        }
    }
}
