﻿using System;
using System.Diagnostics;

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
        public readonly Schema schema;

        private Definition definition;
        private unsafe fixed ushort componentSizes[(int)BitMask.Capacity];
        private unsafe fixed ushort arrayElementSizes[(int)BitMask.Capacity];

        public readonly Definition Definition => definition;
        public readonly BitMask ComponentTypes => definition.componentTypes;
        public readonly BitMask ArrayTypes => definition.arrayTypes;
        public readonly BitMask TagTypes => definition.tagTypes;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public Archetype()
        {
            throw new NotSupportedException();
        }
#endif

        public Archetype(Schema schema)
        {
            this.definition = default;
            this.schema = schema;
        }

        public unsafe Archetype(Definition definition, Schema schema)
        {
            this.definition = definition;
            this.schema = schema;
            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (definition.componentTypes.Contains(i))
                {
                    componentSizes[i] = (ushort)schema.GetComponentTypeSize(i);
                }

                if (definition.arrayTypes.Contains(i))
                {
                    arrayElementSizes[i] = (ushort)schema.GetArrayTypeSize(i);
                }
            }
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Archetype archetype && Equals(archetype);
        }

        public readonly bool Equals(Archetype other)
        {
            return definition.Equals(other.definition) && schema.Equals(other.schema);
        }

        public readonly int CopyComponentTypesTo(Span<ComponentType> destination)
        {
            return definition.CopyComponentTypesTo(destination);
        }

        public readonly int CopyArrayTypesTo(Span<ArrayType> destination)
        {
            return definition.CopyArrayTypesTo(destination);
        }

        public readonly int CopyTagTypesTo(Span<TagType> destination)
        {
            return definition.CopyTagTypesTo(destination);
        }

        public unsafe readonly ushort GetSize(ComponentType componentType)
        {
            ThrowIfComponentTypeIsMissing(componentType);

            return componentSizes[componentType.index];
        }

        public unsafe readonly ushort GetSize(ArrayType arrayType)
        {
            ThrowIfArrayTypeIsMissing(arrayType);

            return arrayElementSizes[arrayType.index];
        }

        public readonly int GetComponentSize<T>() where T : unmanaged
        {
            return schema.GetComponentTypeSize(schema.GetComponentTypeIndex<T>());
        }

        public readonly int GetArrayElementSize<T>() where T : unmanaged
        {
            return schema.GetArrayTypeSize(schema.GetArrayTypeIndex<T>());
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(definition, schema);
        }

        public readonly bool ContainsComponent(ComponentType componentType)
        {
            return definition.componentTypes.Contains(componentType.index);
        }

        public readonly bool ContainsArray(ArrayType arrayType)
        {
            return definition.arrayTypes.Contains(arrayType.index);
        }

        public readonly bool ContainsTag(TagType tagType)
        {
            return definition.tagTypes.Contains(tagType.index);
        }

        public readonly bool ContainsComponent(int index)
        {
            return definition.componentTypes.Contains(index);
        }

        public readonly bool ContainsArray(int index)
        {
            return definition.arrayTypes.Contains(index);
        }

        public readonly bool ContainsTag(int index)
        {
            return definition.tagTypes.Contains(index);
        }

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            return definition.ContainsComponent<T>(schema);
        }

        public readonly bool ContainsArray<T>() where T : unmanaged
        {
            return definition.ContainsArray<T>(schema);
        }

        public readonly bool ContainsTag<T>() where T : unmanaged
        {
            return definition.ContainsTag<T>(schema);
        }

        public void Add<T>() where T : unmanaged, IEntity
        {
            default(T).Describe(ref this);
        }

        /// <summary>
        /// Adds the component of type <typeparamref name="T"/> to the definition.
        /// </summary>
        public unsafe void AddComponentType<T>() where T : unmanaged
        {
            int componentType = schema.GetComponentTypeIndex<T>();
            ThrowIfComponentTypeIsPresent(componentType);

            definition.AddComponentType(componentType);
            componentSizes[componentType] = (ushort)sizeof(T);
        }

        /// <summary>
        /// Adds <paramref name="componentType"/> to the definition.
        /// </summary>
        public unsafe void AddComponentType(ComponentType componentType)
        {
            ThrowIfComponentTypeIsPresent(componentType);

            definition.AddComponentType(componentType);
            componentSizes[componentType.index] = (ushort)schema.GetComponentTypeSize(componentType);
        }

        /// <summary>
        /// Adds a an array of <typeparamref name="T"/> to the definition.
        /// </summary>
        public unsafe void AddArrayType<T>() where T : unmanaged
        {
            int arrayType = schema.GetArrayTypeIndex<T>();
            ThrowIfArrayTypeIsPresent(arrayType);

            definition.AddArrayType(arrayType);
            arrayElementSizes[arrayType] = (ushort)sizeof(T);
        }

        /// <summary>
        /// Adds <paramref name="arrayType"/> to the definition.
        /// </summary>
        public unsafe void AddArrayType(ArrayType arrayType)
        {
            ThrowIfArrayTypeIsPresent(arrayType);

            definition.AddArrayType(arrayType);
            arrayElementSizes[arrayType.index] = (ushort)schema.GetArrayTypeSize(arrayType);
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
        public void AddTagType(TagType tagType)
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

        public static Archetype Get<T1, T2>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            return archetype;
        }

        public static Archetype Get<T1, T2, T3>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            default(T3).Describe(ref archetype);
            return archetype;
        }

        public static Archetype Get<T1, T2, T3, T4>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            default(T3).Describe(ref archetype);
            default(T4).Describe(ref archetype);
            return archetype;
        }

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
                throw new InvalidOperationException($"Component type `{new ComponentType(componentType).ToString(schema)}` is already present in the archetype");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeIsMissing(int componentType)
        {
            if (!definition.componentTypes.Contains(componentType))
            {
                throw new InvalidOperationException($"Component type `{new ComponentType(componentType).ToString(schema)}` is missing from the archetype");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeIsMissing(int arrayType)
        {
            if (!definition.arrayTypes.Contains(arrayType))
            {
                throw new InvalidOperationException($"Array type `{new ArrayType(arrayType).ToString(schema)}` is missing from the archetype");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeIsPresent(int arrayType)
        {
            if (definition.arrayTypes.Contains(arrayType))
            {
                throw new InvalidOperationException($"Array type `{new ArrayType(arrayType).ToString(schema)}` is already present in the archetype");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagTypeIsPresent(int tagType)
        {
            if (definition.tagTypes.Contains(tagType))
            {
                throw new InvalidOperationException($"Tag type `{new TagType(tagType).ToString(schema)}` is already present in the archetype");
            }
        }

        public static bool operator ==(Archetype left, Archetype right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Archetype left, Archetype right)
        {
            return !(left == right);
        }
    }
}