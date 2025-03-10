﻿using System;
using System.Diagnostics;

namespace Worlds
{
    /// <summary>
    /// Describes a data type found on an entity.
    /// </summary>
    public readonly struct DataType : IEquatable<DataType>
    {
        public readonly byte index;

        /// <summary>
        /// Size of the data type in bytes.
        /// </summary>
        public readonly ushort size;

        public readonly Kind kind;

        public readonly ComponentType ComponentType
        {
            get
            {
                ThrowIfNotComponent();

                return new(index);
            }
        }

        public readonly ArrayElementType ArrayType
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

        public readonly bool IsComponent => kind == Kind.Component;
        public readonly bool IsArrayElement => kind == Kind.ArrayElement;
        public readonly bool IsTag => kind == Kind.Tag;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public DataType()
        {
            throw new NotSupportedException();
        }
#endif

        public DataType(ComponentType componentType, int size)
        {
            index = (byte)componentType.index;
            kind = Kind.Component;
            this.size = (ushort)size;
        }

        public DataType(ArrayElementType arrayType, int size)
        {
            index = (byte)arrayType.index;
            kind = Kind.ArrayElement;
            this.size = (ushort)size;
        }

        public DataType(TagType tagType)
        {
            index = (byte)tagType.index;
            kind = Kind.Tag;
            size = 0;
        }

        public DataType(int index, Kind kind, int size)
        {
            this.index = (byte)index;
            this.kind = kind;
            this.size = (ushort)size;
        }

        public readonly override string ToString()
        {
#if DEBUG
            if (kind == Kind.Component)
            {
                return ComponentType.debugCachedTypes[index].ToString();
            }
            else if (kind == Kind.ArrayElement)
            {
                return ArrayElementType.debugCachedTypes[index].ToString();
            }
            else if (kind == Kind.Tag)
            {
                return TagType.debugCachedTypes[index].ToString();
            }
#endif

            Span<char> buffer = stackalloc char[256];
            int length = index.ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        public readonly string ToString(Schema schema)
        {
            Span<char> buffer = stackalloc char[256];
            int length = ToString(schema, buffer);
            return buffer.Slice(0, length).ToString();
        }

        public readonly int ToString(Schema schema, Span<char> destination)
        {
            if (kind == Kind.Component)
            {
                ComponentType componentType = new(index);
                if (schema.ContainsComponentType(componentType))
                {
                    return schema.GetComponentLayout(componentType).ToString(destination);
                }
            }
            else if (kind == Kind.ArrayElement)
            {
                ArrayElementType arrayElementType = new(index);
                if (schema.ContainsArrayType(arrayElementType))
                {
                    return schema.GetArrayLayout(arrayElementType).ToString(destination);
                }
            }
            else if (kind == Kind.Tag)
            {
                TagType tagType = new(index);
                if (schema.ContainsTagType(tagType))
                {
                    return schema.GetTagLayout(tagType).ToString(destination);
                }
            }

            return index.ToString(destination);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotComponent()
        {
            if (kind != Kind.Component)
            {
                throw new("Not a component type");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotArrayElement()
        {
            if (kind != Kind.ArrayElement)
            {
                throw new("Not an array element type");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotTag()
        {
            if (kind != Kind.Tag)
            {
                throw new("Not a tag type");
            }
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is DataType type && Equals(type);
        }

        public readonly bool Equals(DataType other)
        {
            return index == other.index && kind == other.kind;
        }

        public readonly override int GetHashCode()
        {
            unchecked
            {
                return (index << 2) | (byte)kind;
            }
        }

        public static bool operator ==(DataType left, DataType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DataType left, DataType right)
        {
            return !(left == right);
        }

        public static implicit operator byte(DataType type)
        {
            return type.index;
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