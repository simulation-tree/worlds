using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Describes a data type found on an entity.
    /// </summary>
    public readonly struct DataType : IEquatable<DataType>
    {
        private readonly byte index;
        private readonly ushort typeAndSize;

        public readonly ComponentType ComponentType
        {
            get
            {
                ThrowIfNotComponent();

                return new(index);
            }
        }

        public readonly ArrayElementType ArrayElementType
        {
            get
            {
                ThrowIfNotArrayElement();

                return new(index);
            }
        }

        public readonly TagType Tag
        {
            get
            {
                ThrowIfNotTag();

                return new(index);
            }
        }

        public readonly Kind Type => (Kind)(typeAndSize & 0b11);

        /// <summary>
        /// Size of the data type.
        /// <para>
        /// Tag types will always be size 0, despite the declaring type.
        /// </para>
        /// </summary>
        public readonly ushort Size => (ushort)(typeAndSize >> 2);

        public readonly bool IsComponent => Type == Kind.Component;
        public readonly bool IsArrayElement => Type == Kind.ArrayElement;
        public readonly bool IsTag => Type == Kind.Tag;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public DataType()
        {
            throw new NotSupportedException();
        }
#endif

        public DataType(ComponentType componentType, ushort size)
        {
            index = componentType;
            typeAndSize = (ushort)((byte)Kind.Component | (size << 2));
        }

        public DataType(ArrayElementType arrayElementType, ushort size)
        {
            index = arrayElementType;
            typeAndSize = (ushort)((byte)Kind.ArrayElement | (size << 2));
        }

        public DataType(TagType tagType)
        {
            index = tagType;
            typeAndSize = (byte)Kind.Tag;
        }

        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        public readonly uint ToString(USpan<char> destination)
        {
            return index.ToString(destination);
        }

        public readonly string ToString(Schema schema)
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(schema, buffer);
            return buffer.Slice(0, length).ToString();
        }

        public readonly uint ToString(Schema schema, USpan<char> destination)
        {
            Kind type = Type;
            if (type == Kind.Component)
            {
                ComponentType componentType = new(index);
                if (schema.Contains(componentType))
                {
                    return schema.GetComponentLayout(componentType).ToString(destination);
                }
            }
            else if (type == Kind.ArrayElement)
            {
                ArrayElementType arrayElementType = new(index);
                if (schema.Contains(arrayElementType))
                {
                    return schema.GetArrayElementLayout(arrayElementType).ToString(destination);
                }
            }
            else if (type == Kind.Tag)
            {
                TagType tagType = new(index);
                if (schema.Contains(tagType))
                {
                    return schema.GetTagLayout(tagType).ToString(destination);
                }
            }

            return index.ToString(destination);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotComponent()
        {
            if (Type != Kind.Component)
            {
                throw new("Not a component type");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotArrayElement()
        {
            if (Type != Kind.ArrayElement)
            {
                throw new("Not an array element type");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotTag()
        {
            if (Type != Kind.Tag)
            {
                throw new("Not a tag type");
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is DataType type && Equals(type);
        }

        public bool Equals(DataType other)
        {
            return index == other.index && typeAndSize == other.typeAndSize;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(index, typeAndSize);
        }

        public static bool operator ==(DataType left, DataType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DataType left, DataType right)
        {
            return !(left == right);
        }

        public static implicit operator ComponentType(DataType type)
        {
            type.ThrowIfNotComponent();

            return new(type.index);
        }

        public static implicit operator ArrayElementType(DataType type)
        {
            type.ThrowIfNotArrayElement();

            return new(type.index);
        }

        public static implicit operator TagType(DataType type)
        {
            type.ThrowIfNotTag();

            return new(type.index);
        }

        /// <summary>
        /// Describes the type of data found on an entity.
        /// </summary>
        public enum Kind : byte
        {
            Unknown = 0,
            Component = 1,
            ArrayElement = 2,
            Tag = 3
        }
    }
}