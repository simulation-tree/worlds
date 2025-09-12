using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace Worlds
{
    /// <summary>
    /// Describes component, array and tag types, including their sizes.
    /// <para>
    /// Used by <see cref="IEntity"/> types to describe themselves.
    /// </para>
    /// </summary>
    public struct Archetype : IEquatable<Archetype>
    {
        /// <summary>
        /// The schema that this archetype is associated with.
        /// </summary>
        public readonly Schema schema;

        /// <summary>
        /// The definition containg all components, array and tag types.
        /// </summary>
        public Definition definition;

        private SizesBuffer componentSizes;
        private SizesBuffer arrayElementSizes;

        /// <summary>
        /// Component types stored.
        /// </summary>
        public readonly BitMask ComponentTypes => definition.componentTypes;

        /// <summary>
        /// Array types stored.
        /// </summary>
        public readonly BitMask ArrayTypes => definition.arrayTypes;

        /// <summary>
        /// Tag types stored.
        /// </summary>
        public readonly BitMask TagTypes => definition.tagTypes;

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public Archetype()
        {
            throw new NotSupportedException();
        }
#endif
        /// <summary>
        /// Creates a new archetype for building a <see cref="definition"/>.
        /// </summary>
        public Archetype(Schema schema)
        {
            this.definition = default;
            this.schema = schema;
        }

        /// <summary>
        /// Initializes a new archetype from an existing <paramref name="definition"/>.
        /// </summary>
        public Archetype(Definition definition, Schema schema)
        {
            this.definition = definition;
            this.schema = schema;
            Vector256<ulong> componentTypes = definition.componentTypes.value;
            for (int vectorIndex = 0; vectorIndex < 4; vectorIndex++)
            {
                ulong element = componentTypes.GetElement(vectorIndex);
                while (element != 0)
                {
                    int trailingZeros = BitOperations.TrailingZeroCount(element);
                    int bitIndex = vectorIndex * 64 + trailingZeros;
                    componentSizes[bitIndex] = (ushort)schema.GetComponentSize(bitIndex);
                    element &= element - 1;
                }
            }

            Vector256<ulong> arrayTypes = definition.arrayTypes.value;
            for (int vectorIndex = 0; vectorIndex < 4; vectorIndex++)
            {
                ulong element = arrayTypes.GetElement(vectorIndex);
                while (element != 0)
                {
                    int trailingZeros = BitOperations.TrailingZeroCount(element);
                    int bitIndex = vectorIndex * 64 + trailingZeros;
                    arrayElementSizes[bitIndex] = (ushort)schema.GetArraySize(bitIndex);
                    element &= element - 1;
                }
            }
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Archetype archetype && Equals(archetype);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Archetype other)
        {
            return definition.Equals(other.definition) && schema.Equals(other.schema);
        }

        /// <summary>
        /// Retrieves the size of <paramref name="componentType"/> in bytes.
        /// </summary>
        public readonly int GetComponentSize(int componentType)
        {
            ThrowIfComponentTypeIsMissing(componentType);

            return componentSizes[componentType];
        }

        /// <summary>
        /// Retrieves the size of <paramref name="arrayType"/> elements in bytes.
        /// </summary>
        public readonly int GetArraySize(int arrayType)
        {
            ThrowIfArrayTypeIsMissing(arrayType);

            return arrayElementSizes[arrayType];
        }

        /// <summary>
        /// Retrieves the size of the component of type <typeparamref name="T"/> in bytes.
        /// </summary>
        public readonly int GetComponentSize<T>() where T : unmanaged
        {
            int componentType = schema.GetComponentType<T>();
            ThrowIfComponentTypeIsMissing(componentType);

            return componentSizes[componentType];
        }

        /// <summary>
        /// Retrieves the size of each element in an array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly int GetArrayElementSize<T>() where T : unmanaged
        {
            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayTypeIsMissing(arrayType);

            return arrayElementSizes[arrayType];
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(definition, schema);
        }

        /// <summary>
        /// Checks if the definition contains the specified <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(int componentType)
        {
            return definition.componentTypes.Contains(componentType);
        }

        /// <summary>
        /// Checks if the definition contains the specified <paramref name="arrayType"/>.
        /// </summary>
        public readonly bool ContainsArray(int arrayType)
        {
            return definition.arrayTypes.Contains(arrayType);
        }

        /// <summary>
        /// Checks if the definition contains the specified <paramref name="tagType"/>.
        /// </summary>
        public readonly bool ContainsTag(int tagType)
        {
            return definition.tagTypes.Contains(tagType);
        }

        /// <summary>
        /// Checks if the definition contains a component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            return definition.ContainsComponent<T>(schema);
        }

        /// <summary>
        /// Checks if the definition contains an array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsArray<T>() where T : unmanaged
        {
            return definition.ContainsArray<T>(schema);
        }

        /// <summary>
        /// Checks if the definition contains a tag of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsTag<T>() where T : unmanaged
        {
            return definition.ContainsTag<T>(schema);
        }

        /// <summary>
        /// Adds the definition of another entity of type <typeparamref name="T"/>.
        /// </summary>
        public void Add<T>() where T : unmanaged, IEntity
        {
            default(T).Describe(ref this);
        }

        /// <summary>
        /// Adds the component of type <typeparamref name="T"/> to the definition.
        /// </summary>
        public unsafe void AddComponentType<T>() where T : unmanaged
        {
            int componentType = schema.GetComponentType<T>();
            ThrowIfComponentTypeIsPresent(componentType);

            definition.AddComponentType(componentType);
            componentSizes[componentType] = (ushort)sizeof(T);
        }

        /// <summary>
        /// Adds <paramref name="componentType"/> to the definition.
        /// </summary>
        public void AddComponentType(int componentType)
        {
            ThrowIfComponentTypeIsPresent(componentType);

            definition.AddComponentType(componentType);
            componentSizes[componentType] = (ushort)schema.GetComponentSize(componentType);
        }

        /// <summary>
        /// Adds a an array of <typeparamref name="T"/> to the definition.
        /// </summary>
        public unsafe void AddArrayType<T>() where T : unmanaged
        {
            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayTypeIsPresent(arrayType);

            definition.AddArrayType(arrayType);
            arrayElementSizes[arrayType] = (ushort)sizeof(T);
        }

        /// <summary>
        /// Adds <paramref name="arrayType"/> to the definition.
        /// </summary>
        public void AddArrayType(int arrayType)
        {
            ThrowIfArrayTypeIsPresent(arrayType);

            definition.AddArrayType(arrayType);
            arrayElementSizes[arrayType] = (ushort)schema.GetArraySize(arrayType);
        }

        /// <summary>
        /// Adds the the tag of type <typeparamref name="T"/> to the definition.
        /// </summary>
        public void AddTagType<T>() where T : unmanaged
        {
            definition.AddTagType<T>(schema);
        }

        /// <summary>
        /// Adds <paramref name="tagType"/> to the definition.
        /// </summary>
        public void AddTagType(int tagType)
        {
            ThrowIfTagTypeIsPresent(tagType);

            definition.AddTagType(tagType);
        }

        /// <summary>
        /// Retrieves the definition for the specified <typeparamref name="T"/> entity type.
        /// </summary>
        public static Archetype Get<T>(Schema schema) where T : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T).Describe(ref archetype);
            return archetype;
        }

        /// <summary>
        /// Retrieves an archetype that combines the given entity types.
        /// </summary>
        public static Archetype Get<T1, T2>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            return archetype;
        }

        /// <summary>
        /// Retrieves an archetype that combines the given entity types.
        /// </summary>
        public static Archetype Get<T1, T2, T3>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            default(T3).Describe(ref archetype);
            return archetype;
        }

        /// <summary>
        /// Retrieves an archetype that combines the given entity types.
        /// </summary>
        public static Archetype Get<T1, T2, T3, T4>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            default(T3).Describe(ref archetype);
            default(T4).Describe(ref archetype);
            return archetype;
        }

        /// <summary>
        /// Retrieves an archetype that combines the given entity types.
        /// </summary>
        public static Archetype Get<T1, T2, T3, T4, T5>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity where T5 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            default(T3).Describe(ref archetype);
            default(T4).Describe(ref archetype);
            default(T5).Describe(ref archetype);
            return archetype;
        }

        /// <summary>
        /// Retrieves an archetype that combines the given entity types.
        /// </summary>
        public static Archetype Get<T1, T2, T3, T4, T5, T6>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity where T5 : unmanaged, IEntity where T6 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            default(T3).Describe(ref archetype);
            default(T4).Describe(ref archetype);
            default(T5).Describe(ref archetype);
            default(T6).Describe(ref archetype);
            return archetype;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeIsPresent(int componentType)
        {
            if (definition.componentTypes.Contains(componentType))
            {
                throw new InvalidOperationException($"Component type `{DataType.GetComponent(componentType, schema).ToString(schema)}` is already present in the archetype");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeIsMissing(int componentType)
        {
            if (!definition.componentTypes.Contains(componentType))
            {
                throw new InvalidOperationException($"Component type `{DataType.GetComponent(componentType, schema).ToString(schema)}` is missing from the archetype");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeIsMissing(int arrayType)
        {
            if (!definition.arrayTypes.Contains(arrayType))
            {
                throw new InvalidOperationException($"Array type `{DataType.GetArray(arrayType, schema).ToString(schema)}` is missing from the archetype");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeIsPresent(int arrayType)
        {
            if (definition.arrayTypes.Contains(arrayType))
            {
                throw new InvalidOperationException($"Array type `{DataType.GetArray(arrayType, schema).ToString(schema)}` is already present in the archetype");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagTypeIsPresent(int tagType)
        {
            if (definition.tagTypes.Contains(tagType))
            {
                throw new InvalidOperationException($"Tag type `{DataType.GetTag(tagType, schema).ToString(schema)}` is already present in the archetype");
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(Archetype left, Archetype right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Archetype left, Archetype right)
        {
            return !(left == right);
        }
    }
}