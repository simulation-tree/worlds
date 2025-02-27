using System;
using System.Diagnostics;
using Types;
using Unmanaged;

namespace Worlds
{
    public readonly struct TagType : IEquatable<TagType>
    {
        /// <summary>
        /// Tag type stating that the entity is disabled.
        /// </summary>
        public static readonly TagType Disabled = new(BitMask.MaxValue);

        public readonly uint index;

#if NET
        /// <summary>
        /// Not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public TagType()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Initializes an existing tag type with the given index.
        /// </summary>
        public TagType(byte value)
        {
            ThrowIfOutOfRange(value);

            this.index = value;
        }

        public TagType(uint value)
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
        /// Builds a string representation of this tag type.
        /// </summary>
        public readonly string ToString(Schema schema)
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(schema, buffer);
            return buffer.GetSpan(length).ToString();
        }

        /// <summary>
        /// Writes the index of this tag type to the <paramref name="destination"/>.
        /// </summary>
        public readonly uint ToString(USpan<char> destination)
        {
            return index.ToString(destination);
        }

        /// <summary>
        /// Writes the name of this tag type to the <paramref name="destination"/>.
        /// </summary>
        public readonly uint ToString(Schema schema, USpan<char> destination)
        {
            return schema.GetTagLayout(this).ToString(destination);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is TagType type && Equals(type);
        }

        /// <inheritdoc/>
        public readonly bool Equals(TagType other)
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

        public readonly TypeLayout GetLayout(Schema schema)
        {
            return schema.GetTagLayout(this);
        }

        /// <inheritdoc/>
        public static bool operator ==(TagType left, TagType right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(TagType left, TagType right)
        {
            return !left.Equals(right);
        }

        public static implicit operator uint(TagType type)
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