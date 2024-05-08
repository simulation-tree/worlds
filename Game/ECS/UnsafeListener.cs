using System.Runtime.InteropServices;
using Unmanaged;

namespace Game
{
    internal unsafe struct UnsafeListener
    {
        private delegate* unmanaged<void> callback;

        public static bool IsDisposed(UnsafeListener* listener)
        {
            return Allocations.IsNull((nint)listener);
        }

        public static UnsafeListener* Allocate(delegate* unmanaged<World, Container, void> callback)
        {
            UnsafeListener* listener = (UnsafeListener*)Marshal.AllocHGlobal(sizeof(UnsafeListener));
            listener->callback = (delegate* unmanaged<void>)callback;
            Allocations.Register((nint)listener);
            return listener;
        }

        public static UnsafeListener* Allocate(delegate* unmanaged<nint, World, Container, void> callback)
        {
            UnsafeListener* listener = (UnsafeListener*)Marshal.AllocHGlobal(sizeof(UnsafeListener));
            listener->callback = (delegate* unmanaged<void>)callback;
            Allocations.Register((nint)listener);
            return listener;
        }

        public static void Free(UnsafeListener* listener)
        {
            Allocations.ThrowIfNull((nint)listener);
            Marshal.FreeHGlobal((nint)listener);
            Allocations.Unregister((nint)listener);
        }

        public static void Invoke(UnsafeListener* listener, World world, Container message)
        {
            Allocations.ThrowIfNull((nint)listener);
            delegate* unmanaged<World, Container, void> callback = (delegate* unmanaged<World, Container, void>)listener->callback;
            callback(world, message);
        }

        public static void Invoke(UnsafeListener* listener, nint context, World world, Container message)
        {
            Allocations.ThrowIfNull((nint)listener);
            delegate* unmanaged<nint, World, Container, void> callback = (delegate* unmanaged<nint, World, Container, void>)listener->callback;
            callback(context, world, message);
        }
    }
}
