using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public unsafe struct UnsafeComponentObserver<T> where T : unmanaged
    {
        private readonly UnmanagedList<EntityID> tracked;
        private readonly UnmanagedList<EntityID> foundEntities;
        private readonly World world;
        private readonly delegate* unmanaged<World, EntityID, void> added;
        private readonly delegate* unmanaged<World, EntityID, void> removed;

        private UnsafeComponentObserver(World world, delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
        {
            tracked = new();
            foundEntities = new();
            this.world = world;
            this.added = added;
            this.removed = removed;
        }

        public static UnsafeComponentObserver<T>* Create(World world, delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
        {
            nint pointer = Marshal.AllocHGlobal(sizeof(UnsafeComponentObserver<T>));
            UnsafeComponentObserver<T>* typedPointer = (UnsafeComponentObserver<T>*)pointer;
            typedPointer[0] = new(world, added, removed);
            Allocations.Register(pointer);
            return typedPointer;
        }

        public static bool IsDisposed(UnsafeComponentObserver<T>* observer)
        {
            return Allocations.IsNull((nint)observer);
        }

        public static void Dispose(UnsafeComponentObserver<T>* observer)
        {
            Allocations.ThrowIfNull((nint)observer);
            observer->tracked.Dispose();
            observer->foundEntities.Dispose();
            Marshal.FreeHGlobal((nint)observer);
            Allocations.Unregister((nint)observer);
        }

        public static void PollChanges(UnsafeComponentObserver<T>* observer)
        {
            Allocations.ThrowIfNull((nint)observer);
            UnmanagedList<EntityID> tracked = observer->tracked;
            UnmanagedList<EntityID> foundEntities = observer->foundEntities;
            using UnmanagedList<EntityID> entities = observer->world.GetEntities(ComponentTypeMask.Get<T>());
            for (uint i = 0; i < entities.Count; i++)
            {
                EntityID id = entities[i];
                if (tracked.AddIfUnique(id))
                {
                    observer->added(observer->world, id);
                }

                foundEntities.Add(id);
            }

            uint index = tracked.Count - 1;
            while (index != uint.MaxValue)
            {
                EntityID id = tracked[index];
                if (!foundEntities.Contains(id))
                {
                    tracked.RemoveAt(index);
                    observer->removed(observer->world, id);
                }

                index--;
            }

            foundEntities.Clear();
        }
    }
}
