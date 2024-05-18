using Unmanaged;

namespace Game.Unsafe
{
    internal unsafe struct UnsafeListener
    {
        private delegate* unmanaged<void> callback;

        public static bool IsDisposed(UnsafeListener* listener)
        {
            return Allocations.IsNull(listener);
        }

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

        public static void Free(ref UnsafeListener* listener)
        {
            Allocations.ThrowIfNull(listener);
            Allocations.Free(ref listener);
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
    }
}
