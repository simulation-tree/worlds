using Game.Events;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public unsafe struct ComponentObserver : IDisposable
    {
        private UnsafeComponentObserver* value;
        private readonly ListenerWithContext listener;

        public readonly bool IsDisposed => UnsafeComponentObserver.IsDisposed(value);

        public ComponentObserver()
        {
            throw new NotImplementedException();
        }

        public ComponentObserver(World world, RuntimeType type, delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
        {
            value = UnsafeComponentObserver.Allocate(world, type, added, removed);
            listener = world.Listen((nint)value, RuntimeType.Get<Update>(), &OnUpdate);
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            listener.Dispose();
            UnsafeComponentObserver.Free(ref value);
        }

        [Conditional("TRACK_ALLOCATIONS")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(ComponentObserver));
            }
        }

        [UnmanagedCallersOnly]
        private static void OnUpdate(nint context, World world, Container container)
        {
            UnsafeComponentObserver* observer = (UnsafeComponentObserver*)context;
            UnmanagedList<EntityID> tracked = new(observer->tracked);
            UnmanagedList<EntityID> found = new(observer->found);
            found.Clear();
            world.Fill(observer->type, found);
            uint i = 0;
            for (i = 0; i < found.Count; i++)
            {
                EntityID id = found[i];
                if (tracked.TryAdd(id))
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
                        tracked.RemoveAtBySwapping(i);
                    }

                    i--;
                }
            }
        }

        private unsafe struct UnsafeComponentObserver
        {
            public readonly RuntimeType type;
            public UnsafeList* tracked;
            public UnsafeList* found;
            public readonly delegate* unmanaged<World, EntityID, void> added;
            public readonly delegate* unmanaged<World, EntityID, void> removed;

            private UnsafeComponentObserver(RuntimeType type, UnsafeList* tracked, UnsafeList* found, delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
            {
                this.type = type;
                this.tracked = tracked;
                this.found = found;
                this.added = added;
                this.removed = removed;
            }

            public static UnsafeComponentObserver* Allocate(World world, RuntimeType type, delegate* unmanaged<World, EntityID, void> added, delegate* unmanaged<World, EntityID, void> removed)
            {
                UnsafeComponentObserver* value = Allocations.Allocate<UnsafeComponentObserver>();
                UnsafeList* tracked = UnsafeList.Allocate<EntityID>();
                UnsafeList* found = UnsafeList.Allocate<EntityID>();
                value[0] = new(type, tracked, found, added, removed);
                return value;
            }

            public static bool IsDisposed(UnsafeComponentObserver* observer)
            {
                return Allocations.IsNull(observer);
            }

            public static void Free(ref UnsafeComponentObserver* observer)
            {
                Allocations.ThrowIfNull(observer);
                UnsafeList.Free(ref observer->tracked);
                UnsafeList.Free(ref observer->found);
                Allocations.Free(ref observer);
            }
        }
    }
}
