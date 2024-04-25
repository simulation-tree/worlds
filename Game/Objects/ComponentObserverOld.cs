using Game.Events;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Unmanaged.Collections;

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

    public readonly unsafe struct ComponentObserver : IDisposable
    {
        private readonly UnsafeComponentObserver* value;
        private readonly ListenerWithContext listener;

        public ComponentObserver()
        {
            throw new NotImplementedException();
        }

        public ComponentObserver(World world, ComponentType type, delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
        {
            value = UnsafeComponentObserver.Allocate(world, type, added, removed);
            listener = world.Listen((nint)value, RuntimeType.Get<Update>(), &OnUpdate);
        }

        public readonly void Dispose()
        {
            listener.Dispose();
            UnsafeComponentObserver.Free(value);
        }

        [UnmanagedCallersOnly]
        private static void OnUpdate(nint context, World world, Container container)
        {
            UnsafeComponentObserver* observer = (UnsafeComponentObserver*)context;
            UnmanagedList<EntityID> tracked = new(observer->tracked);
            UnmanagedList<EntityID> found = new(observer->found);
            found.Clear();
            world.ReadEntities(ComponentTypeMask.Get(observer->type), found);
            uint i = 0;
            for (i = 0; i < found.Count; i++)
            {
                EntityID id = found[i];
                if (tracked.AddIfUnique(id))
                {
                    observer->added(world, id);
                }
            }

            if (tracked.Count > 0)
            {
                i = tracked.Count - 1;
                while (i != uint.MaxValue)
                {
                    EntityID id = tracked[i];
                    if (!found.Contains(id))
                    {
                        observer->removed(world, id);
                        tracked.RemoveAt(i);
                    }

                    i--;
                }
            }
        }

        private unsafe struct UnsafeComponentObserver
        {
            public readonly ComponentType type;
            public readonly UnsafeList* tracked;
            public readonly UnsafeList* found;
            public readonly delegate* unmanaged<World, EntityID, void> added;
            public readonly delegate* unmanaged<World, EntityID, void> removed;

            private UnsafeComponentObserver(ComponentType type, UnsafeList* tracked, UnsafeList* found, delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
            {
                this.type = type;
                this.tracked = tracked;
                this.found = found;
                this.added = added;
                this.removed = removed;
            }

            public static UnsafeComponentObserver* Allocate(World world, ComponentType type, delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
            {
                nint pointer = Marshal.AllocHGlobal(sizeof(UnsafeComponentObserver));
                Allocations.Register(pointer);
                UnsafeComponentObserver* value = (UnsafeComponentObserver*)pointer;
                UnsafeList* tracked = UnsafeList.Allocate<EntityID>();
                UnsafeList* found = UnsafeList.Allocate<EntityID>();
                value[0] = new(type, tracked, found, added, removed);
                return value;
            }

            public static bool IsDisposed(UnsafeComponentObserver* observer)
            {
                return Allocations.IsNull((nint)observer);
            }

            public static void Free(UnsafeComponentObserver* observer)
            {
                Allocations.ThrowIfNull((nint)observer);
                UnsafeList.Free(observer->tracked);
                UnsafeList.Free(observer->found);
                Marshal.FreeHGlobal((nint)observer);
                Allocations.Unregister((nint)observer);
            }
        }
    }
}
