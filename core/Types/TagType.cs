using System;
using System.Diagnostics;
using Types;

namespace Worlds
{
    public readonly struct TagType : IEquatable<TagType>
    {
#if DEBUG
        internal static readonly System.Collections.Generic.Dictionary<int, TypeLayout> debugCachedTypes = new();
#endif

        /// <summary>
        /// Tag type stating that the entity is disabled.
        /// </summary>
        public static readonly TagType Disabled = new(BitMask.MaxValue);

        public readonly int index;

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
        public TagType(int value)
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
        /// Builds a string representation of this tag type.
        /// </summary>
        public readonly string ToString(Schema schema)
        {
            Span<char> buffer = stackalloc char[256];
            int length = ToString(schema, buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Writes the index of this tag type to the <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            return index.ToString(destination);
        }

        /// <summary>
        /// Writes the name of this tag type to the <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Schema schema, Span<char> destination)
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
            return index;
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

        public static implicit operator int(TagType type)
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