#pragma warning disable CS8981 //too bad
#pragma warning disable IDE1006 //so sad
using System;

namespace Worlds
{
    /// <summary>
    /// A <see cref="uint"/> type that refers to a reference local to its entity.
    /// <para>Can be explicitly cast from and into a <see cref="uint"/> value.
    /// </para>
    /// </summary>
    public readonly struct rint : IEquatable<rint>
    {
        internal readonly uint value;

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public rint()
        {
            throw new NotSupportedException();
        }
#endif

        internal rint(uint value)
        {
            this.value = value;
        }

        internal rint(int value)
        {
            unchecked
            {
                this.value = (uint)value;
            }
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
            Span<char> buffer = stackalloc char[8];
            int length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this <see cref="rint"/> value.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            return value.ToString(destination);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                return (int)value;
            }
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
        public static explicit operator int(rint value)
        {
            unchecked
            {
                return (int)value.value;
            }
        }

        /// <inheritdoc/>
        public static explicit operator rint(int value)
        {
            return new(value);
        }

        /// <summary>
        /// Checks if the left <see cref="rint"/> value is less than the right <see cref="int"/> value.
        /// </summary>
        public static bool operator <(rint left, int right)
        {
            unchecked
            {
                return left.value < (uint)right;
            }
        }

        /// <summary>
        /// Checks if the left <see cref="int"/> value is less than the right <see cref="rint"/> value.
        /// </summary>
        public static bool operator <(int left, rint right)
        {
            unchecked
            {
                return (uint)left < right.value;
            }
        }

        /// <summary>
        /// Checks if the left <see cref="rint"/> value is less than the right <see cref="rint"/> value.
        /// </summary>
        public static bool operator <(rint left, rint right)
        {
            return left.value < right.value;
        }

        /// <summary>
        /// Checks if the left <see cref="rint"/> value is greater than the right <see cref="int"/> value.
        /// </summary>
        public static bool operator >(rint left, int right)
        {
            unchecked
            {
                return left.value > (uint)right;
            }
        }

        /// <summary>
        /// Checks if the left <see cref="int"/> value is greater than the right <see cref="rint"/> value.
        /// </summary>
        public static bool operator >(int left, rint right)
        {
            unchecked
            {
                return (uint)left > right.value;
            }
        }

        /// <summary>
        /// Checks if the left <see cref="rint"/> value is greater than the right <see cref="rint"/> value.
        /// </summary>
        public static bool operator >(rint left, rint right)
        {
            return left.value > right.value;
        }

        /// <summary>
        /// Checks if the left <see cref="rint"/> value is less than or equal to the right <see cref="int"/> value.
        /// </summary>
        public static bool operator <=(rint left, int right)
        {
            unchecked
            {
                return left.value <= (uint)right;
            }
        }

        /// <summary>
        /// Checks if the left <see cref="int"/> value is less than or equal to the right <see cref="rint"/> value.
        /// </summary>
        public static bool operator <=(int left, rint right)
        {
            unchecked
            {
                return (uint)left <= right.value;
            }
        }

        /// <summary>
        /// Checks if the left <see cref="rint"/> value is less than or equal to the right <see cref="rint"/> value.
        /// </summary>
        public static bool operator <=(rint left, rint right)
        {
            return left.value <= right.value;
        }

        /// <summary>
        /// Checks if the left <see cref="rint"/> value is greater than or equal to the right <see cref="int"/> value.
        /// </summary>
        public static bool operator >=(rint left, int right)
        {
            unchecked
            {
                return left.value >= (uint)right;
            }
        }

        /// <summary>
        /// Checks if the left <see cref="int"/> value is greater than or equal to the right <see cref="rint"/> value.
        /// </summary>
        public static bool operator >=(int left, rint right)
        {
            unchecked
            {
                return (uint)left >= right.value;
            }
        }

        /// <summary>
        /// Checks if the left <see cref="rint"/> value is greater than or equal to the right <see cref="rint"/> value.
        /// </summary>
        public static bool operator >=(rint left, rint right)
        {
            return left.value >= right.value;
        }
    }
}