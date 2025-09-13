using System;

namespace Worlds
{
    /// <summary>
    /// Describes a data type found on an entity.
    /// </summary>
    public readonly struct DataType : IEquatable<DataType>
    {
        /// <summary>
        /// The index of the data type.
        /// </summary>
        public readonly byte index;

        /// <summary>
        /// The type of the data.
        /// </summary>
        public readonly Kind kind;

        /// <summary>
        /// Size of the data type in bytes.
        /// </summary>
        public readonly ushort size;

        /// <summary>
        /// Checks if this data type refers to a component.
        /// </summary>
        public readonly bool IsComponent => kind == Kind.Component;

        /// <summary>
        /// Checks if this data type refers to an array.
        /// </summary>
        public readonly bool IsArrayElement => kind == Kind.Array;

        /// <summary>
        /// Checks if this data type refers to a tag.
        /// </summary>
        public readonly bool IsTag => kind == Kind.Tag;

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public DataType()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public DataType(int index, Kind kind, int size)
        {
            this.index = (byte)index;
            this.kind = kind;
            this.size = (ushort)size;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return $"{kind}:{index} {size} bytes";
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is DataType type && Equals(type);
        }

        /// <inheritdoc/>
        public readonly bool Equals(DataType other)
        {
            return index == other.index && kind == other.kind;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + index;
            hash = hash * 31 + (int)kind;
            return hash;
        }

        /// <inheritdoc/>
        public static bool operator ==(DataType left, DataType right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(DataType left, DataType right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Creates a new instance that references to <paramref name="componentType"/>.
        /// </summary>
        public unsafe static DataType GetComponent<T>(int componentType) where T : unmanaged
        {
            return new(componentType, Kind.Component, sizeof(T));
        }

        /// <summary>
        /// Creates a new instance that references to component of type <typeparamref name="T"/>.
        /// </summary>
        public unsafe static DataType GetComponent<T>(Schema schema) where T : unmanaged
        {
            return new(schema.GetComponentType<T>(), Kind.Component, sizeof(T));
        }

        /// <summary>
        /// Creates a new instance that references to <paramref name="componentType"/>.
        /// </summary>
        public static DataType GetComponent(int componentType, Schema schema)
        {
            return new(componentType, Kind.Component, schema.GetComponentSize(componentType));
        }

        /// <summary>
        /// Creates a new instance that references to <paramref name="arrayType"/>.
        /// </summary>
        public unsafe static DataType GetArray<T>(int arrayType) where T : unmanaged
        {
            return new(arrayType, Kind.Array, sizeof(T));
        }

        /// <summary>
        /// Creates a new instance that references to an array of type <typeparamref name="T"/>.
        /// </summary>
        public unsafe static DataType GetArray<T>(Schema schema) where T : unmanaged
        {
            return new(schema.GetArrayType<T>(), Kind.Array, sizeof(T));
        }

        /// <summary>
        /// Creates a new instance that references to <paramref name="arrayType"/>.
        /// </summary>
        public static DataType GetArray(int arrayType, Schema schema)
        {
            return new(arrayType, Kind.Array, schema.GetArraySize(arrayType));
        }

        /// <summary>
        /// Creates a new instance that references to tag of type <typeparamref name="T"/>.
        /// </summary>
        public static DataType GetTag<T>(Schema schema) where T : unmanaged
        {
            return new(schema.GetTagType<T>(), Kind.Tag, 1);
        }

        /// <summary>
        /// Creates a new instance that references to <paramref name="tagType"/>.
        /// </summary>
        public static DataType GetTag(int tagType, Schema schema)
        {
            return new(tagType, Kind.Tag, 1);
        }

        /// <summary>
        /// Describes the type of data found on an entity.
        /// </summary>
        public enum Kind : byte
        {
            /// <summary>
            /// Unknown and uninitialized.
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// A component type.
            /// </summary>
            Component = 1,

            /// <summary>
            /// An array type.
            /// </summary>
            Array = 2,

            /// <summary>
            /// A tag type.
            /// </summary>
            Tag = 3
        }
    }
}