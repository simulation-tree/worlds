using System;

namespace Worlds
{
    /// <summary>
    /// Contains information about an entity type.
    /// </summary>
    public struct Definition : IEquatable<Definition>
    {
        private BitMask componentTypes;
        private BitMask arrayTypes;
        private BitMask tagTypes;

        /// <summary>
        /// Mask of component types in this definition.
        /// </summary>
        public readonly BitMask ComponentTypes => componentTypes;

        /// <summary>
        /// Mask of array types in this definition.
        /// </summary>
        public readonly BitMask ArrayTypes => arrayTypes;

        /// <summary>
        /// Mask of tag types in this definition.
        /// </summary>
        public readonly BitMask TagTypes => tagTypes;

        /// <summary>
        /// Creates a new definition with the exact component and array <see cref="BitMask"/> values.
        /// </summary>
        public Definition(BitMask componentTypes, BitMask arrayTypes, BitMask tagTypes)
        {
            this.componentTypes = componentTypes;
            this.arrayTypes = arrayTypes;
            this.tagTypes = tagTypes;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[512];
            int length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        public readonly string ToString(Schema schema)
        {
            Span<char> buffer = stackalloc char[512];
            int length = ToString(schema, buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this definition.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            int length = 0;
            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (componentTypes.Contains(i))
                {
                    ComponentType componentType = new(i);
                    length += componentType.ToString(destination.Slice(length));
                    destination[length++] = ',';
                    destination[length++] = ' ';
                }
            }

            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (arrayTypes.Contains(i))
                {
                    ArrayType arrayType = new(i);
                    length += arrayType.ToString(destination.Slice(length));
                    destination[length++] = ',';
                    destination[length++] = ' ';
                }
            }

            if (length > 0)
            {
                length -= 2;
            }

            return length;
        }

        /// <summary>
        /// Builds a string representation of this definition.
        /// </summary>
        public readonly int ToString(Schema schema, Span<char> destination)
        {
            int length = 0;
            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (componentTypes.Contains(i))
                {
                    ComponentType componentType = new(i);
                    length += componentType.ToString(schema, destination.Slice(length));
                    destination[length++] = ',';
                    destination[length++] = ' ';
                }
            }

            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (arrayTypes.Contains(i))
                {
                    ArrayType arrayType = new(i);
                    length += arrayType.ToString(schema, destination.Slice(length));
                    destination[length++] = ',';
                    destination[length++] = ' ';
                }
            }

            if (length > 0)
            {
                length -= 2;
            }

            return length;
        }

        /// <summary>
        /// Copies the component types in this definition to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of component types copied.</returns>
        public readonly int CopyComponentTypesTo(Span<ComponentType> destination)
        {
            int count = 0;
            for (int c = 0; c < BitMask.Capacity; c++)
            {
                if (componentTypes.Contains(c))
                {
                    destination[count] = new(c);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies the array types in this definition to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of array types copied.</returns>
        public readonly int CopyArrayTypesTo(Span<ArrayType> destination)
        {
            int count = 0;
            for (int a = 0; a < BitMask.Capacity; a++)
            {
                if (arrayTypes.Contains(a))
                {
                    destination[count] = new(a);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies the tag types in this definition to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of tag types copied.</returns>
        public readonly int CopyTagTypesTo(Span<TagType> destination)
        {
            int count = 0;
            for (int t = 0; t < BitMask.Capacity; t++)
            {
                if (tagTypes.Contains(t))
                {
                    destination[count] = new(t);
                    count++;
                }
            }

            return count;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(componentTypes, arrayTypes, tagTypes);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Definition definition && Equals(definition);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Definition other)
        {
            return other.arrayTypes == arrayTypes && other.componentTypes == componentTypes && other.tagTypes == tagTypes;
        }

        public readonly bool ContainsComponent(ComponentType componentType)
        {
            return componentTypes.Contains(componentType.index);
        }

        public readonly bool ContainsArray(ArrayType arrayType)
        {
            return arrayTypes.Contains(arrayType.index);
        }

        public readonly bool ContainsTag(TagType tagType)
        {
            return tagTypes.Contains(tagType.index);
        }

        public readonly bool ContainsComponent(int index)
        {
            return componentTypes.Contains(index);
        }

        public readonly bool ContainsArray(int index)
        {
            return arrayTypes.Contains(index);
        }

        public readonly bool ContainsTag(int index)
        {
            return tagTypes.Contains(index);
        }

        /// <summary>
        /// Checks if this definition contains the specified <typeparamref name="T"/> component type.
        /// </summary>
        public readonly bool ContainsComponent<T>(Schema schema) where T : unmanaged
        {
            if (!schema.ContainsComponentType<T>())
            {
                return false;
            }

            return componentTypes.Contains(schema.GetComponentTypeIndex<T>());
        }

        /// <summary>
        /// Checks if this definition contains the specified <typeparamref name="T"/> array type.
        /// </summary>
        public readonly bool ContainsArray<T>(Schema schema) where T : unmanaged
        {
            if (!schema.ContainsArrayType<T>())
            {
                return false;
            }

            return arrayTypes.Contains(schema.GetArrayType<T>().index);
        }

        public readonly bool ContainsTag<T>(Schema schema) where T : unmanaged
        {
            if (!schema.ContainsTagType<T>())
            {
                return false;
            }

            return tagTypes.Contains(schema.GetTagType<T>().index);
        }

        /// <summary>
        /// Adds the given <paramref name="arrayTypes"/> bit mask to this definition.
        /// </summary>
        public void AddArrayTypes(BitMask arrayTypes)
        {
            this.arrayTypes |= arrayTypes;
        }

        /// <summary>
        /// Adds the specified <paramref name="componentType"/> to this definition.
        /// </summary>
        public void AddComponentType(ComponentType componentType)
        {
            componentTypes.Set(componentType.index);
        }

        public void RemoveComponentType(ComponentType componentType)
        {
            componentTypes.Clear(componentType.index);
        }

        public void AddComponentType(int index)
        {
            componentTypes.Set(index);
        }

        public void RemoveComponentType(int index)
        {
            componentTypes.Clear(index);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T"/> component type to this definition.
        /// </summary>
        public void AddComponentType<T>(Schema schema) where T : unmanaged
        {
            componentTypes.Set(schema.GetComponentTypeIndex<T>());
        }

        /// <summary>
        /// Adds the components from the given <paramref name="componentTypes"/> bit mask.
        /// </summary>
        public void AddComponentTypes(BitMask componentTypes)
        {
            this.componentTypes |= componentTypes;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/> and <typeparamref name="C2"/> component types to this definition.
        /// </summary>
        public void AddComponentTypes<C1, C2>(Schema schema) where C1 : unmanaged where C2 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2>();
        }

        public void AddComponentTypes<C1, C2, C3>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3>();
        }

        public void AddComponentTypes<C1, C2, C3, C4>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4>();
        }

        public void AddComponentTypes<C1, C2, C3, C4, C5>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5>();
        }

        public void AddComponentTypes<C1, C2, C3, C4, C5, C6>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6>();
        }

        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7>();
        }

        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8>();
        }

        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9>();
        }

        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>();
        }

        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>();
        }

        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>();
        }

        /// <summary>
        /// Adds the specified <paramref name="arrayType"/> to this definition.
        /// </summary>
        public void AddArrayType(ArrayType arrayType)
        {
            arrayTypes.Set(arrayType.index);
        }

        public void AddArrayType(int arrayType)
        {
            arrayTypes.Set(arrayType);
        }

        public void RemoveArrayType(ArrayType arrayType)
        {
            arrayTypes.Clear(arrayType.index);
        }

        public void RemoveArrayType(int arrayType)
        {
            arrayTypes.Clear(arrayType);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/> array type to this definition.
        /// </summary>
        public void AddArrayType<A1>(Schema schema) where A1 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1>();
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/> and <typeparamref name="A2"/> array types to this definition.
        /// </summary>
        public void AddArrayTypes<A1, A2>(Schema schema) where A1 : unmanaged where A2 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2>();
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/>, <typeparamref name="A2"/> and <typeparamref name="A3"/> array types to this definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3>();
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/>, <typeparamref name="A2"/>, <typeparamref name="A3"/> and <typeparamref name="A4"/> array types to this definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4>();
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/>, <typeparamref name="A2"/>, <typeparamref name="A3"/>, <typeparamref name="A4"/> and <typeparamref name="A5"/> array types to this definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4, A5>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5>();
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/>, <typeparamref name="A2"/>, <typeparamref name="A3"/>, <typeparamref name="A4"/>, <typeparamref name="A5"/> and <typeparamref name="A6"/> array types to this definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4, A5, A6>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6>();
        }

        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7>();
        }

        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged where A8 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8>();
        }

        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged where A8 : unmanaged where A9 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9>();
        }

        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged where A8 : unmanaged where A9 : unmanaged where A10 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10>();
        }

        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged where A8 : unmanaged where A9 : unmanaged where A10 : unmanaged where A11 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11>();
        }

        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged where A8 : unmanaged where A9 : unmanaged where A10 : unmanaged where A11 : unmanaged where A12 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12>();
        }

        public void AddTagType(TagType tagType)
        {
            tagTypes.Set(tagType.index);
        }

        public void RemoveTagType(TagType tagType)
        {
            tagTypes.Clear(tagType.index);
        }

        public void AddTagType(int tagType)
        {
            tagTypes.Set(tagType);
        }

        public void RemoveTagType(int tagType)
        {
            tagTypes.Clear(tagType);
        }

        public void AddTagType<T>(Schema schema) where T : unmanaged
        {
            tagTypes.Set(schema.GetTagType<T>().index);
        }

        public void AddTagTypes(BitMask tagTypes)
        {
            this.tagTypes |= tagTypes;
        }

        public void AddTagTypes<T1, T2>(Schema schema) where T1 : unmanaged where T2 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2>();
        }

        public void AddTagTypes<T1, T2, T3>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3>();
        }

        public void AddTagTypes<T1, T2, T3, T4>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4>();
        }

        public void AddTagTypes<T1, T2, T3, T4, T5>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5>();
        }

        public void AddTagTypes<T1, T2, T3, T4, T5, T6>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6>();
        }

        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7>();
        }

        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>();
        }

        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
        }

        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
        }

        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>();
        }

        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>();
        }

        public static Definition Get<T>(Schema schema) where T : unmanaged, IEntity
        {
            Archetype archetype = Archetype.Get<T>(schema);
            return archetype.Definition;
        }

        public static bool operator ==(Definition a, Definition b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Definition a, Definition b)
        {
            return !a.Equals(b);
        }
    }
}