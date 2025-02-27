﻿using System;
using System.Diagnostics;
using Types;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Represents an unmanaged array type usable with entities.
    /// </summary>
    public readonly struct ArrayElementType : IEquatable<ArrayElementType>
    {
        public readonly uint index;

#if NET
        /// <summary>
        /// Not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public ArrayElementType()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Initializes an existing array type.
        /// </summary>
        public ArrayElementType(byte value)
        {
            ThrowIfOutOfRange(value);

            this.index = value;
        }

        public ArrayElementType(uint value)
        {
            ThrowIfOutOfRange(value);

            this.index = value;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.GetSpan(length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this array type.
        /// </summary>
        public readonly string ToString(Schema schema)
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(schema, buffer);
            return buffer.GetSpan(length).ToString();
        }

        /// <summary>
        /// Writes the index of this array type to the <paramref name="destination"/>.
        /// </summary>
        public readonly uint ToString(USpan<char> destination)
        {
            return index.ToString(destination);
        }

        /// <summary>
        /// Writes a string representation of this array type to the <paramref name="destination"/>.
        /// </summary>
        public readonly uint ToString(Schema schema, USpan<char> destination)
        {
            return schema.GetArrayLayout(this).ToString(destination);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is ArrayElementType type && Equals(type);
        }

        /// <inheritdoc/>
        public readonly bool Equals(ArrayElementType other)
        {
            return index == other.index;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                return (int)index;
            }
        }

        /// <summary>
        /// Retrieves a possible <see cref="TypeLayout"/> for this component type.
        /// </summary>
        public readonly TypeLayout GetLayout(Schema schema)
        {
            return schema.GetArrayLayout(this);
        }

        public static bool operator ==(ArrayElementType left, ArrayElementType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArrayElementType left, ArrayElementType right)
        {
            return !(left == right);
        }

        public static implicit operator uint(ArrayElementType type)
        {
            return type.index;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfRange(uint value)
        {
            if (value > BitMask.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Value must be less than or equal to {BitMask.MaxValue}");
            }
        }
    }
}