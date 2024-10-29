using System;
using Unmanaged;

namespace Simulation.Functions
{
    public unsafe readonly struct HandleFunction : IEquatable<HandleFunction>
    {
#if NET
        private readonly delegate* unmanaged<SystemContainer, World, Allocation, void> value;

        public HandleFunction(delegate* unmanaged<SystemContainer, World, Allocation, void> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<SystemContainer, World, Allocation, void> value;

        public FinalizeFunction(delegate*<SystemContainer, World, Allocation, void> value)
        {
            this.value = value;
        }
#endif
        public readonly void Invoke(SystemContainer container, World programWorld, Allocation message)
        {
            value(container, programWorld, message);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is HandleFunction function && Equals(function);
        }

        public readonly bool Equals(HandleFunction other)
        {
            return ((nint)value) == ((nint)other.value);
        }

        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        public static bool operator ==(HandleFunction left, HandleFunction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HandleFunction left, HandleFunction right)
        {
            return !(left == right);
        }
    }
}