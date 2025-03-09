using System;
using System.Diagnostics;
using Types;

namespace Worlds
{
    /// <summary>
    /// Represents an unmanaged component type usable with entities.
    /// </summary>
    public readonly struct ComponentType : IEquatable<ComponentType>
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
        public ComponentType()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Initializes an existing component type with the given index.
        /// </summary>
        public ComponentType(int value)
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
        /// Builds a string representation of this component type.
        /// </summary>
        public readonly string ToString(Schema schema)
        {
            Span<char> buffer = stackalloc char[256];
            int length = ToString(schema, buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Writes the index of this component type to the <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            return index.ToString(destination);
        }

        /// <summary>
        /// Writes a string representation of this component type to the <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Schema schema, Span<char> destination)
        {
            if (schema.ContainsComponentType(this))
            {
                return schema.GetComponentLayout(this).ToString(destination);
            }
            else
            {
                return index.ToString(destination);
            }
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is ComponentType type && Equals(type);
        }

        /// <inheritdoc/>
        public readonly bool Equals(ComponentType other)
        {
            return index == other.index;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return index;
        }

        /// <summary>
        /// Retrieves the <see cref="TypeLayout"/> for this component type.
        /// </summary>
        public readonly TypeLayout GetLayout(Schema schema)
        {
            return schema.GetComponentLayout(this);
        }

        public static bool operator ==(ComponentType left, ComponentType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentType left, ComponentType right)
        {
            return !(left == right);
        }

        public static implicit operator int(ComponentType type)
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