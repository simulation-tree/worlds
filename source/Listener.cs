using Simulation.Unsafe;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// A proof object for a callback whenever a <see cref="World"/>
    /// polls for the registered event type.
    /// <para>
    /// Automatically disposed together with its <see cref="World"/>.
    /// </para>
    /// </summary>
    public readonly unsafe struct Listener : IDisposable, IEquatable<Listener>
    {
        public readonly RuntimeType messageType;

        internal readonly UnsafeListener* value;
        private readonly World world;

        public readonly bool IsDisposed => UnsafeListener.IsDisposed(value);

#if NET5_0_OR_GREATER
        public Listener()
        {
            throw new NotImplementedException();
        }

        internal Listener(World world, RuntimeType eventType, delegate* unmanaged<World, Allocation, RuntimeType, void> callback)
        {
            this.messageType = eventType;
            value = UnsafeListener.Allocate(callback);
            this.world = world;
        }
#else
        internal Listener(World world, RuntimeType eventType, delegate*<World, Allocation, RuntimeType, void> callback)
        {
            this.eventType = eventType;
            value = UnsafeListener.Allocate(callback);
            this.world = world;
        }
#endif

        /// <summary>
        /// Unregisters the callback.
        /// </summary>
        public readonly void Dispose()
        {
            ThrowIfDisposed();
            UnsafeWorld.RemoveListener(world.value, this);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Listener));
            }
        }

        public readonly void Invoke(World world, Allocation message, RuntimeType messageType)
        {
            UnsafeListener.Invoke(value, world, message, messageType);
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
