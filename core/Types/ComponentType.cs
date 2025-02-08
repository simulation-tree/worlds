using System;
using System.Diagnostics;
using Types;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Represents an unmanaged component type usable with entities.
    /// </summary>
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        private readonly byte index;

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
        public ComponentType(byte value)
        {
            ThrowIfOutOfRange(value);

            this.index = value;
        }

        public ComponentType(uint value)
        {
            ThrowIfOutOfRange(value);

            this.index = (byte)value;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this component type.
        /// </summary>
        public readonly string ToString(Schema schema)
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(schema, buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Writes the index of this component type to the <paramref name="destination"/>.
        /// </summary>
        public readonly uint ToString(USpan<char> destination)
        {
            return index.ToString(destination);
        }

        /// <summary>
        /// Writes a string representation of this component type to the <paramref name="destination"/>.
        /// </summary>
        public readonly uint ToString(Schema schema, USpan<char> destination)
        {
            if (schema.Contains(this))
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
            return obj is Type type && Equals(type);
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

        public static implicit operator byte(ComponentType componentType)
        {
            return componentType.index;
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