using System;

namespace Game
{
    /// <summary>
    /// Watches over changes to existence of <typeparamref name="T"/> components.
    /// </summary>
    public readonly unsafe struct ComponentObserver<T> : IDisposable where T : unmanaged
    {
        private readonly UnsafeComponentObserver<T>* pointer;

        public ComponentObserver(World world, delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
        {
            pointer = UnsafeComponentObserver<T>.Create(world, added, removed);
        }

        public readonly void Dispose()
        {
            UnsafeComponentObserver<T>.Dispose(pointer);
        }

        /// <summary>
        /// Iterates over the world to find changes to <typeparamref name="T"/> components.
        /// </summary>
        public readonly void PollChanges()
        {
            UnsafeComponentObserver<T>.PollChanges(pointer);
        }
    }
}
