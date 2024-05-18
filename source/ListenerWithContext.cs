using Game.Unsafe;
using System;
using Unmanaged;

namespace Game
{
    public readonly unsafe struct ListenerWithContext : IDisposable, IEquatable<ListenerWithContext>
    {
        public readonly RuntimeType eventType;

        internal readonly UnsafeListener* value;
        private readonly nint context;
        private readonly World world;

        public readonly bool IsDisposed => UnsafeListener.IsDisposed(value);

        public ListenerWithContext()
        {
            throw new NotImplementedException();
        }

        internal ListenerWithContext(nint context, World world, RuntimeType eventType, delegate* unmanaged<nint, World, Container, void> callback)
        {
            this.eventType = eventType;
            this.context = context;
            value = UnsafeListener.Allocate(callback);
            this.world = world;
        }

        public readonly void Dispose()
        {
            UnsafeWorld.Unlisten(world.value, this);
        }

        public readonly void Invoke(World world, Container message)
        {
            UnsafeListener.Invoke(value, context, world, message);
        }

        public override bool Equals(object? obj)
        {
            return obj is ListenerWithContext context && Equals(context);
        }

        public bool Equals(ListenerWithContext other)
        {
            return value == other.value;
        }

        public override unsafe int GetHashCode()
        {
            return new IntPtr(value).GetHashCode();
        }

        public static bool operator ==(ListenerWithContext left, ListenerWithContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ListenerWithContext left, ListenerWithContext right)
        {
            return !(left == right);
        }
    }
}
