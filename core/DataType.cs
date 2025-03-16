using System;
using System.Diagnostics;

namespace Worlds
{
    /// <summary>
    /// Describes a data type found on an entity.
    /// </summary>
    public readonly struct DataType : IEquatable<DataType>
    {
        public readonly byte index;
        public readonly Kind kind;

        /// <summary>
        /// Size of the data type in bytes.
        /// </summary>
        public readonly ushort size;

        public readonly bool IsComponent => kind == Kind.Component;
        public readonly bool IsArrayElement => kind == Kind.Array;
        public readonly bool IsTag => kind == Kind.Tag;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public DataType()
        {
            throw new NotSupportedException();
        }
#endif

        public DataType(int index, Kind kind, int size)
        {
            this.index = (byte)index;
            this.kind = kind;
            this.size = (ushort)size;
        }

        public readonly override string ToString()
        {
            return $"{kind}:{index} {size} bytes";
        }

        public readonly string ToString(Schema schema)
        {
            if (kind == Kind.Component)
            {
                if (schema.ContainsComponentType(index))
                {
                    return schema.GetComponentLayout(index).ToString();
                }
            }
            else if (kind == Kind.Array)
            {
                if (schema.ContainsArrayType(index))
                {
                    return schema.GetArrayLayout(index).ToString();
                }
            }
            else if (kind == Kind.Tag)
            {
                if (schema.ContainsTagType(index))
                {
                    return schema.GetTagLayout(index).ToString();
                }
            }

            return index.ToString();
        }

        public readonly int ToString(Schema schema, Span<char> destination)
        {
            if (kind == Kind.Component)
            {
                if (schema.ContainsComponentType(index))
                {
                    return schema.GetComponentLayout(index).ToString(destination);
                }
            }
            else if (kind == Kind.Array)
            {
                if (schema.ContainsArrayType(index))
                {
                    return schema.GetArrayLayout(index).ToString(destination);
                }
            }
            else if (kind == Kind.Tag)
            {
                if (schema.ContainsTagType(index))
                {
                    return schema.GetTagLayout(index).ToString(destination);
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
            if (kind != Kind.Array)
            {
                throw new("Not an array type");
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

        public unsafe static DataType GetComponent<T>(int componentType) where T : unmanaged
        {
            return new(componentType, Kind.Component, sizeof(T));
        }

        public unsafe static DataType GetComponent<T>(Schema schema) where T : unmanaged
        {
            return new(schema.GetComponentType<T>(), Kind.Component, sizeof(T));
        }

        public static DataType GetComponent(int componenentType, Schema schema)
        {
            return new(componenentType, Kind.Component, schema.GetComponentSize(componenentType));
        }

        public unsafe static DataType GetArray<T>(int arrayType) where T : unmanaged
        {
            return new(arrayType, Kind.Array, sizeof(T));
        }

        public unsafe static DataType GetArray<T>(Schema schema) where T : unmanaged
        {
            return new(schema.GetArrayType<T>(), Kind.Array, sizeof(T));
        }

        public static DataType GetArray(int arrayType, Schema schema)
        {
            return new(arrayType, Kind.Array, schema.GetArraySize(arrayType));
        }

        public static DataType GetTag<T>(Schema schema) where T : unmanaged
        {
            return new(schema.GetTagType<T>(), Kind.Tag, 1);
        }

        public static DataType GetTag(int tagType, Schema schema)
        {
            return new(tagType, Kind.Tag, 1);
        }

        /// <summary>
        /// Describes the type of data found on an entity.
        /// </summary>
        public enum Kind : byte
        {
            Unknown = 0,
            Component = 1,
            Array = 2,
            Tag = 3
        }
    }
}