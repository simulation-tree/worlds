using Simulation.Unsafe;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
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
            ThrowIfDisposed();
            UnsafeWorld.RemoveListener(world.value, this);
        }

        [Conditional("TRACK_ALLOCATIONS")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Listener));
            }
        }

        public readonly void Invoke(World world, Container message)
        {
            UnsafeListener.Invoke(value, world, message);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Listener listener && Equals(listener);
        }

        public readonly bool Equals(Listener other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return new IntPtr(value).GetHashCode();
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
