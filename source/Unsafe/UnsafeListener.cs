using Unmanaged;

namespace Simulation.Unsafe
{
    public unsafe struct UnsafeListener
    {
#if NET5_0_OR_GREATER
        private delegate* unmanaged<void> callback;

        public static UnsafeListener* Allocate(delegate* unmanaged<World, Container, void> callback)
        {
            UnsafeListener* listener = Allocations.Allocate<UnsafeListener>();
            listener->callback = (delegate* unmanaged<void>)callback;
            return listener;
        }

        public static UnsafeListener* Allocate(delegate* unmanaged<nint, World, Container, void> callback)
        {
            UnsafeListener* listener = Allocations.Allocate<UnsafeListener>();
            listener->callback = (delegate* unmanaged<void>)callback;
            return listener;
        }

        public static void Invoke(UnsafeListener* listener, World world, Container message)
        {
            Allocations.ThrowIfNull(listener);
            delegate* unmanaged<World, Container, void> callback = (delegate* unmanaged<World, Container, void>)listener->callback;
            callback(world, message);
        }

        public static void Invoke(UnsafeListener* listener, nint context, World world, Container message)
        {
            Allocations.ThrowIfNull(listener);
            delegate* unmanaged<nint, World, Container, void> callback = (delegate* unmanaged<nint, World, Container, void>)listener->callback;
            callback(context, world, message);
        }
#else
        private delegate*<World, Container, void> callback;

        public static UnsafeListener* Allocate(delegate*<World, Container, void> callback)
        {
            UnsafeListener* listener = Allocations.Allocate<UnsafeListener>();
            listener->callback = callback;
            return listener;
        }

        public static UnsafeListener* Allocate(delegate*<nint, World, Container, void> callback)
        {
            UnsafeListener* listener = Allocations.Allocate<UnsafeListener>();
            listener->callback = (delegate*<World, Container, void>)callback;
            return listener;
        }

        public static void Invoke(UnsafeListener* listener, World world, Container message)
        {
            Allocations.ThrowIfNull(listener);
            listener->callback(world, message);
        }

        public static void Invoke(UnsafeListener* listener, nint context, World world, Container message)
        {
            Allocations.ThrowIfNull(listener);
            ((delegate*<nint, World, Container, void>)listener->callback)(context, world, message);
        }
#endif

        public static bool IsDisposed(UnsafeListener* listener)
        {
            return Allocations.IsNull(listener);
        }

        public static void Free(ref UnsafeListener* listener)
        {
            Allocations.ThrowIfNull(listener);
            Allocations.Free(ref listener);
        }
    }
}
