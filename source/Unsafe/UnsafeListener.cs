using Unmanaged;

namespace Simulation.Unsafe
{
    public unsafe struct UnsafeListener
    {
#if NET
        private delegate* unmanaged<void> callback;

        public static UnsafeListener* Allocate(delegate* unmanaged<World, Allocation, RuntimeType, void> callback)
        {
            UnsafeListener* listener = Allocations.Allocate<UnsafeListener>();
            listener->callback = (delegate* unmanaged<void>)callback;
            return listener;
        }

        public static UnsafeListener* Allocate(delegate* unmanaged<nint, World, Allocation, RuntimeType, void> callback)
        {
            UnsafeListener* listener = Allocations.Allocate<UnsafeListener>();
            listener->callback = (delegate* unmanaged<void>)callback;
            return listener;
        }

        public static void Invoke(UnsafeListener* listener, World world, Allocation message, RuntimeType messageType)
        {
            Allocations.ThrowIfNull(listener);
            delegate* unmanaged<World, Allocation, RuntimeType, void> callback = (delegate* unmanaged<World, Allocation, RuntimeType, void>)listener->callback;
            callback(world, message, messageType);
        }

        public static void Invoke(UnsafeListener* listener, nint context, World world, Allocation message, RuntimeType messageType)
        {
            Allocations.ThrowIfNull(listener);
            delegate* unmanaged<nint, World, Allocation, RuntimeType, void> callback = (delegate* unmanaged<nint, World, Allocation, RuntimeType, void>)listener->callback;
            callback(context, world, message, messageType);
        }
#else
        private delegate*<World, Allocation, RuntimeType, void> callback;

        public static UnsafeListener* Allocate(delegate*<World, Allocation, RuntimeType, void> callback)
        {
            UnsafeListener* listener = Allocations.Allocate<UnsafeListener>();
            listener->callback = callback;
            return listener;
        }

        public static UnsafeListener* Allocate(delegate*<nint, World, Allocation, RuntimeType, void> callback)
        {
            UnsafeListener* listener = Allocations.Allocate<UnsafeListener>();
            listener->callback = (delegate*<World, Allocation, RuntimeType, void>)callback;
            return listener;
        }

        public static void Invoke(UnsafeListener* listener, World world, Allocation message, RuntimeType messageType)
        {
            Allocations.ThrowIfNull(listener);
            listener->callback(world, message, messageType);
        }

        public static void Invoke(UnsafeListener* listener, nint context, World world, Allocation message, RuntimeType messageType)
        {
            Allocations.ThrowIfNull(listener);
            ((delegate*<nint, World, Allocation, RuntimeType, void>)listener->callback)(context, world, message, messageType);
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
