using System;
using System.Diagnostics;
using Unmanaged.Collections;

namespace Game
{
    public readonly unsafe struct Entity : IDisposable
    {
        public readonly EntityID entityId;
        public readonly World world;

        private readonly delegate* unmanaged<World, EntityID, void> destroyedCallback;

        public readonly bool IsDisposed => !world.ContainsEntity(entityId);

        public Entity(World world, delegate* unmanaged<World, EntityID, void> destroyedCallback)
        {
            entityId = world.CreateEntity();
            this.world = world;
            this.destroyedCallback = destroyedCallback;
            UnmanagedWorld.EntityDestroyed += OnDestroyed;
        }

        public readonly void Dispose()
        {
            ThrowIfDisposed();

            world.DestroyEntity(entityId);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Entity));
            }
        }

        private readonly void OnDestroyed(UnmanagedWorld* world, EntityID id)
        {
            if (entityId == id && this.world.AsPointer() == world)
            {
                UnmanagedWorld.EntityDestroyed -= OnDestroyed;
                destroyedCallback(this.world, id);
            }
        }

        public readonly void AddComponent<T>(T component) where T : unmanaged
        {
            ThrowIfDisposed();

            world.AddComponent(entityId, component);
        }

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            return world.ContainsComponent<T>(entityId);
        }

        public readonly ref T GetComponentRef<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            return ref world.GetComponentRef<T>(entityId);
        }

        public readonly ref T TryGetComponentRef<T>(out bool found) where T : unmanaged
        {
            ThrowIfDisposed();

            return ref world.TryGetComponentRef<T>(entityId, out found);
        }

        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            world.RemoveComponent<T>(entityId);
        }

        public readonly bool ContainsCollection<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            return world.ContainsCollection<T>(entityId);
        }

        public readonly Span<T> CreateCollection<T>(uint count) where T : unmanaged
        {
            ThrowIfDisposed();

            return world.CreateCollection<T>(entityId, count);
        }

        public readonly UnmanagedList<T> CreateCollection<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            return world.CreateCollection<T>(entityId);
        }

        public readonly UnmanagedList<T> GetCollection<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            return world.GetCollection<T>(entityId);
        }

        public readonly void DestroyCollection<T>() where T : unmanaged
        {
            ThrowIfDisposed();

            world.DestroyCollection<T>(entityId);
        }

        public readonly void AddToCollection<T>(T value) where T : unmanaged
        {
            ThrowIfDisposed();

            world.AddToCollection(entityId, value);
        }

        public readonly void RemoveAtFromCollection<T>(uint index) where T : unmanaged
        {
            ThrowIfDisposed();

            world.RemoveAtFromCollection<T>(entityId, index);
        }
    }
}
