#pragma warning disable CS8981 //too bad
#pragma warning disable IDE1006 //so sad
using System;

namespace Simulation
{
    /// <summary>
    /// A <see cref="uint"/> type that refers to a reference local to its entity.
    /// <para>Can be implicitly cast from, and explicitly cast into a <see cref="uint"/> value.
    /// </para>
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

        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[8];
            int charsWritten = ToString(buffer);
            return new string(buffer[..charsWritten]);
        }

        public readonly int ToString(Span<char> buffer)
        {
            value.TryFormat(buffer, out int charsWritten);
            return charsWritten;
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

        public static explicit operator rint(uint value)
        {
            return new rint(value);
        }
    }
}