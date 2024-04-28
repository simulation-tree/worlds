using System;

namespace Game
{
    /// <summary>
    /// Watches over changes to existence of <typeparamref name="T"/> components.
    /// </summary>
    public readonly unsafe struct ComponentObserverOld<T> : IDisposable where T : unmanaged
    {
        private readonly UnsafeComponentObserverOld<T>* pointer;

        public ComponentObserverOld(World world, delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
        {
            pointer = UnsafeComponentObserverOld<T>.Allocate(world, added, removed);
        }

        public readonly void Dispose()
        {
            UnsafeComponentObserverOld<T>.Dispose(pointer);
        }

        /// <summary>
        /// Iterates over the world to find changes to <typeparamref name="T"/> components.
        /// </summary>
        public readonly void PollChanges()
        {
            UnsafeComponentObserverOld<T>.PollChanges(pointer);
        }
    }
}
