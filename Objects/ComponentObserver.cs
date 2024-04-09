using System;
using Unmanaged.Collections;

namespace Game.Objects
{
    public unsafe struct ComponentObserver<C> : IDisposable where C : unmanaged
    {
        private UnmanagedList<EntityID> tracked;
        private UnmanagedList<EntityID> foundEntities;
        private delegate* unmanaged<World, EntityID, void> added;
        private delegate* unmanaged<World, EntityID, void> removed;
        private World world;

        public ComponentObserver(delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
        {
            tracked = new();
            foundEntities = new();
            this.added = added;
            this.removed = removed;
            this.world = default;
        }

        public void Dispose()
        {
            removed = default;
            added = default;
            foundEntities.Dispose();
            tracked.Dispose();
        }

        public void Poll(World world)
        {
            this.world = world;
            world.QueryComponents<C>(ForEach);
            uint index = tracked.Count - 1;
            while (index > 0)
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
