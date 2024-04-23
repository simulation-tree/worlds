using System;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Game
{
    public readonly unsafe struct Listener : IDisposable, IEquatable<Listener>
    {
        public readonly RuntimeType eventType;

        internal readonly UnsafeListener* value;
        private readonly World world;

        public readonly bool IsDisposed => UnsafeListener.IsDisposed(value);

        public Listener()
        {
            throw new NotImplementedException();
        }

        internal Listener(World world, RuntimeType eventType, delegate* unmanaged<World, Container, void> callback)
        {
            this.eventType = eventType;
            value = UnsafeListener.Allocate(callback);
            this.world = world;
        }

        public readonly void Dispose()
        {
            UnsafeWorld.Unlisten(world.value, this);
        }

        public readonly void Invoke(World world, Container message)
        {
            UnsafeListener.Invoke(value, world, message);
        }

        public override bool Equals(object? obj)
        {
            return obj is Listener listener && Equals(listener);
        }

        public bool Equals(Listener other)
        {
            return value == other.value;
        }

        public unsafe override int GetHashCode()
        {
            return new IntPtr(value).GetHashCode();
        }

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
        }

        public static bool operator ==(Listener left, Listener right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Listener left, Listener right)
        {
            return !(left == right);
        }
    }
}
