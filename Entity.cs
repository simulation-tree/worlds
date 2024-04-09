using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Game
{
    /// <summary>
    /// An object that represents a container of components.
    /// <para>Can receive event messages with <see cref="IListener{T}"/>.</para>
    /// </summary>
    public class Entity : IDisposable
    {
        public readonly EntityID entityId;

        private readonly List<object> listeners;
        private bool disposed;
        private readonly VirtualMachine vm;

        /// <summary>
        /// Has this entity instance been destroyed in its world?
        /// </summary>
        public unsafe bool IsDestroyed
        {
            get
            {
                if (disposed) return true;
                return !World.ContainsEntity(entityId);
            }
        }

        /// <summary>
        /// The world that the entity belongs to.
        /// </summary>
        public ref World World => ref vm.World;

        public unsafe Entity(VirtualMachine vm)
        {
            this.vm = vm;
            entityId = World.CreateEntity();
            UnmanagedWorld.EntityDestroyed += OnDestroyed;
            listeners = ListenerUtils.AddImplementations(World, this);
        }

        public void AddComponent<T>(T component) where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfDestroyed();
            World.AddComponent(entityId, component);
        }

        public bool ContainsComponent<T>() where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfDestroyed();
            return World.ContainsComponent<T>(entityId);
        }

        public ref T GetComponent<T>() where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfDestroyed();
            return ref World.GetComponentRef<T>(entityId);
        }

        public ref T TryGetComponent<T>(out bool found) where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfDestroyed();
            return ref World.TryGetComponentRef<T>(entityId, out found);
        }

        public void RemoveComponent<T>() where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfDestroyed();
            World.RemoveComponent<T>(entityId);
        }

        public bool ContainsCollection<T>() where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfDestroyed();
            return World.ContainsCollection<T>(entityId);
        }

        public Span<T> GetCollection<T>() where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfDestroyed();
            return World.GetCollection<T>(entityId);
        }

        public void DestroyCollection<T>() where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfDestroyed();
            World.DestroyCollection<T>(entityId);
        }

        public void AddToCollection<T>(T value) where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfDestroyed();
            World.AddToCollection(entityId, value);
        }

        public void RemoveAtFromCollection<T>(uint index) where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfDestroyed();
            World.RemoveAtFromCollection<T>(entityId, index);
        }

        private unsafe void OnDestroyed(UnmanagedWorld* world, EntityID id)
        {
            if (this.entityId == id && World.AsPointer() == world)
            {
                UnmanagedWorld.EntityDestroyed -= OnDestroyed;
                ListenerUtils.RemoveImplementations(world, listeners);
                OnDestroyed();
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfDestroyed()
        {
            if (IsDestroyed)
            {
                throw new ObjectDisposedException(nameof(Entity));
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Entity));
            }
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            ThrowIfDestroyed();
            disposed = true;
            World.DestroyEntity(entityId);
        }

        /// <summary>
        /// Called when the entity has been destroyed from the world.
        /// </summary>
        protected virtual void OnDestroyed()
        {
        }
    }
}
