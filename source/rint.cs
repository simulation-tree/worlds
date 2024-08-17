#pragma warning disable CS8981 //too bad
using System;

namespace Simulation
{
    /// <summary>
    /// A <see cref="uint"/> type that refers to a reference.
    /// </summary>
    public readonly struct rint : IEquatable<rint>
    {
        internal readonly uint value;

        internal rint(uint value)
        {
            this.value = value;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is rint rint && Equals(rint);
        }

        public readonly bool Equals(rint other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(value);
        }

        public static bool operator ==(rint left, rint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(rint left, rint right)
        {
            return !(left == right);
        }

        public static implicit operator uint(rint value)
        {
            return value.value;
        }
    }
}