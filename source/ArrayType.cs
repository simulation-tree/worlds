using System;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Represents an unmanaged array type usable with entities.
    /// </summary>
    public readonly struct ArrayType : IEquatable<ArrayType>
    {
        /// <summary>
        /// Index of the array type within a <see cref="BitSet"/>.
        /// </summary>
        public readonly byte index;

#if NET
        /// <summary>
        /// Not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public ArrayType()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Initializes an existing array type.
        /// </summary>
        public ArrayType(byte value)
        {
            this.index = value;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this array type.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            return index.ToString(buffer);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Type type && Equals(type);
        }

        /// <inheritdoc/>
        public readonly bool Equals(ArrayType other)
        {
            return index == other.index;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return index.GetHashCode();
        }

        /// <summary>
        /// Retrieves a possible <see cref="TypeLayout"/> for this component type.
        /// </summary>
        public readonly TypeLayout GetLayout(Schema schema)
        {
            return schema.GetLayout(this);
        }

        public static bool operator ==(ArrayType left, ArrayType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArrayType left, ArrayType right)
        {
            return !(left == right);
        }

        public static implicit operator byte(ArrayType type)
        {
            return type.index;
        }
    }
}
