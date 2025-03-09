﻿using System;
using System.Diagnostics;
using Types;

namespace Worlds
{
    /// <summary>
    /// Represents an unmanaged array type usable with entities.
    /// </summary>
    public readonly struct ArrayElementType : IEquatable<ArrayElementType>
    {
#if DEBUG
        internal static readonly System.Collections.Generic.Dictionary<int, TypeLayout> debugCachedTypes = new();
#endif

        public readonly int index;

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
        public ArrayElementType(int value)
        {
            ThrowIfOutOfRange(value);

            this.index = value;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[256];
            int length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this array type.
        /// </summary>
        public readonly string ToString(Schema schema)
        {
            Span<char> buffer = stackalloc char[256];
            int length = ToString(schema, buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Writes the index of this array type to the <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            return index.ToString(destination);
        }

        /// <summary>
        /// Writes a string representation of this array type to the <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Schema schema, Span<char> destination)
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
            return index;
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

        public static implicit operator int(ArrayElementType type)
        {
            return type.index;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfRange(int value)
        {
            if (value > BitMask.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Value must be less than or equal to {BitMask.MaxValue}");
            }
        }
    }
}