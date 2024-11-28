#pragma warning disable CS8981 //too bad
#pragma warning disable IDE1006 //so sad
using System;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// A <see cref="uint"/> type that refers to a reference local to its entity.
    /// <para>Can be explicitly cast from and into a <see cref="uint"/> value.
    /// </para>
    /// </summary>
    public readonly struct rint : IEquatable<rint>
    {
        internal readonly uint value;

        internal rint(uint value)
        {
            this.value = value;
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is rint rint && Equals(rint);
        }

        /// <inheritdoc/>
        public readonly bool Equals(rint other)
        {
            return value == other.value;
        }

        /// <inheritdoc/>
        public unsafe readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[8];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this <see cref="rint"/> value.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            return value.ToString(buffer);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(value);
        }

        /// <inheritdoc/>
        public static bool operator ==(rint left, rint right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(rint left, rint right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static explicit operator uint(rint value)
        {
            return value.value;
        }

        /// <inheritdoc/>
        public static explicit operator rint(uint value)
        {
            return new rint(value);
        }
    }
}