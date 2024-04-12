using System;
using Unmanaged.Collections;

namespace Game
{
    /// <summary>
    /// Watches over changes to existence of <typeparamref name="C"/> components.
    /// </summary>
    public readonly unsafe struct ComponentObserver<C> : IDisposable where C : unmanaged
    {
        private readonly UnmanagedList<EntityID> tracked;
        private readonly UnmanagedList<EntityID> foundEntities;
        private readonly World world;
        private readonly delegate* unmanaged<World, EntityID, void> added;
        private readonly delegate* unmanaged<World, EntityID, void> removed;

        public ComponentObserver(World world, delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
        {
            tracked = new();
            foundEntities = new();
            this.world = world;
            this.added = added;
            this.removed = removed;
        }

        public readonly void Dispose()
        {
            foundEntities.Dispose();
            tracked.Dispose();
        }

        public readonly void PollChanges()
        {
            world.QueryComponents<C>(ForEach);
            uint index = tracked.Count - 1;
            while (index != uint.MaxValue)
            {
                EntityID id = tracked[index];
                if (!foundEntities.Contains(id))
                {
                    tracked.RemoveAt(index);
                    removed(world, id);
                }

                index--;
            }

            foundEntities.Clear();
        }

        private readonly void ForEach(in EntityID id)
        {
            foundEntities.Add(id);
            if (!tracked.Contains(id))
            {
                tracked.Add(id);
                added(world, id);
            }
        }
    }
}
