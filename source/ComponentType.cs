using System;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Represents an unmanaged component type usable with entities.
    /// </summary>
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        /// <summary>
        /// Index of the component type within a <see cref="BitSet"/>.
        /// </summary>
        public readonly byte index;

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
        /// Builds a string representation of this component type.
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
        public readonly bool Equals(ComponentType other)
        {
            return index == other.index;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return index.GetHashCode();
        }

        /// <summary>
        /// Retrieves the <see cref="TypeLayout"/> for this component type.
        /// </summary>
        public readonly TypeLayout GetLayout(Schema schema)
        {
            return schema.GetLayout(this);
        }

        public static bool operator ==(ComponentType left, ComponentType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentType left, ComponentType right)
        {
            return !(left == right);
        }

        public static implicit operator byte(ComponentType type)
        {
            return type.index;
        }
    }
}
