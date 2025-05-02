using System;

namespace Worlds
{
    /// <summary>
    /// Contains information about an entity type.
    /// </summary>
    public struct Definition : IEquatable<Definition>
    {
        /// <summary>
        /// An empty definition.
        /// </summary>
        public static readonly Definition Default = default;

        /// <summary>
        /// The mask of component types present.
        /// </summary>
        public BitMask componentTypes;

        /// <summary>
        /// The mask of array types present.
        /// </summary>
        public BitMask arrayTypes;

        /// <summary>
        /// The mask of tag types present.
        /// </summary>
        public BitMask tagTypes;

        /// <summary>
        /// Checks if this definition describes a disabled entity.
        /// </summary>
        public readonly bool IsDisabled => (tagTypes.d & 1UL << 63) != 0;

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
            Span<char> buffer = stackalloc char[1024];
            int length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <inheritdoc/>
        public readonly string ToString(Schema schema)
        {
            Span<char> buffer = stackalloc char[1024];
            int length = ToString(schema, buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this definition.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            //todo: improve to string for definitions to be more precise
            int length = 0;
            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (componentTypes.Contains(i))
                {
                    length += i.ToString(destination.Slice(length));
                    destination[length++] = ',';
                    destination[length++] = ' ';
                }
            }

            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (arrayTypes.Contains(i))
                {
                    length += i.ToString(destination.Slice(length));
                    destination[length++] = ',';
                    destination[length++] = ' ';
                }
            }

            if (length > 0)
            {
                length -= 2;
            }

            if (IsDisabled)
            {
                const string Keyword = "Disabled";
                if (length > 0)
                {
                    destination[length++] = ' ';
                }

                destination[length++] = '(';
                Keyword.AsSpan().CopyTo(destination.Slice(length));
                length += Keyword.Length;
                destination[length++] = ')';
            }

            return length;
        }

        /// <summary>
        /// Builds a string representation of this definition.
        /// </summary>
        public readonly int ToString(Schema schema, Span<char> destination)
        {
            int length = 0;
            for (int c = 0; c < BitMask.Capacity; c++)
            {
                if (componentTypes.Contains(c))
                {
                    length += DataType.GetComponent(c, schema).ToString(schema, destination.Slice(length));
                    destination[length++] = ',';
                    destination[length++] = ' ';
                }
            }

            for (int a = 0; a < BitMask.Capacity; a++)
            {
                if (arrayTypes.Contains(a))
                {
                    length += DataType.GetComponent(a, schema).ToString(schema, destination.Slice(length));
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
        public readonly int CopyComponentTypesTo(Span<int> destination)
        {
            int count = 0;
            for (int c = 0; c < BitMask.Capacity; c++)
            {
                if (componentTypes.Contains(c))
                {
                    destination[count++] = c;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies the array types in this definition to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of array types copied.</returns>
        public readonly int CopyArrayTypesTo(Span<int> destination)
        {
            int count = 0;
            for (int a = 0; a < BitMask.Capacity; a++)
            {
                if (arrayTypes.Contains(a))
                {
                    destination[count++] = a;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies the tag types in this definition to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of tag types copied.</returns>
        public readonly int CopyTagTypesTo(Span<int> destination)
        {
            int count = 0;
            for (int t = 0; t < BitMask.Capacity; t++)
            {
                if (tagTypes.Contains(t))
                {
                    destination[count++] = t;
                }
            }

            return count;
        }

        /// <summary>
        /// Retrieves a <see cref="long"/> precision hash code.
        /// </summary>
        public readonly long GetLongHashCode()
        {
            unchecked
            {
                long hash = 17;
                hash = hash * 23 + componentTypes.GetLongHashCode();
                hash = hash * 23 + arrayTypes.GetLongHashCode();
                hash = hash * 23 + tagTypes.GetLongHashCode();
                return hash;
            }
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + componentTypes.GetHashCode();
                hash = hash * 23 + arrayTypes.GetHashCode();
                hash = hash * 23 + tagTypes.GetHashCode();
                return hash;
            }
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

        /// <summary>
        /// Checks if the definition contains the <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(int componentType)
        {
            return componentTypes.Contains(componentType);
        }

        /// <summary>
        /// Checks if the definition contains the <paramref name="arrayType"/>.
        /// </summary>
        public readonly bool ContainsArray(int arrayType)
        {
            return arrayTypes.Contains(arrayType);
        }

        /// <summary>
        /// Checks if the definition contains the <paramref name="tagType"/>.
        /// </summary>
        public readonly bool ContainsTag(int tagType)
        {
            return tagTypes.Contains(tagType);
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

            return componentTypes.Contains(schema.GetComponentType<T>());
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

            return arrayTypes.Contains(schema.GetArrayType<T>());
        }

        /// <summary>
        /// Checks if the definition contains a <typeparamref name="T"/> tag type.
        /// </summary>
        public readonly bool ContainsTag<T>(Schema schema) where T : unmanaged
        {
            if (!schema.ContainsTagType<T>())
            {
                return false;
            }

            return tagTypes.Contains(schema.GetTagType<T>());
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
        public void AddComponentType(int componentType)
        {
            componentTypes.Set(componentType);
        }

        /// <summary>
        /// Removes the <paramref name="componentType"/> from the definition.
        /// </summary>
        public void RemoveComponentType(int componentType)
        {
            componentTypes.Clear(componentType);
        }

        /// <summary>
        /// Removes the <paramref name="componentTypes"/> from the definition.
        /// </summary>
        public void RemoveComponentTypes(BitMask componentTypes)
        {
            this.componentTypes &= ~componentTypes;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T"/> component type to this definition.
        /// </summary>
        public void AddComponentType<T>(Schema schema) where T : unmanaged
        {
            componentTypes.Set(schema.GetComponentType<T>());
        }

        /// <summary>
        /// Adds the components from the given <paramref name="componentTypes"/> bit mask.
        /// </summary>
        public void AddComponentTypes(BitMask componentTypes)
        {
            this.componentTypes |= componentTypes;
        }

        /// <summary>
        /// Adds the specified component types to the definition.
        /// </summary>
        public void AddComponentTypes<C1, C2>(Schema schema) where C1 : unmanaged where C2 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2>();
        }

        /// <summary>
        /// Adds the specified component types to the definition.
        /// </summary>
        public void AddComponentTypes<C1, C2, C3>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3>();
        }

        /// <summary>
        /// Adds the specified component types to the definition.
        /// </summary>
        public void AddComponentTypes<C1, C2, C3, C4>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4>();
        }

        /// <summary>
        /// Adds the specified component types to the definition.
        /// </summary>
        public void AddComponentTypes<C1, C2, C3, C4, C5>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5>();
        }

        /// <summary>
        /// Adds the specified component types to the definition.
        /// </summary>
        public void AddComponentTypes<C1, C2, C3, C4, C5, C6>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6>();
        }

        /// <summary>
        /// Adds the specified component types to the definition.
        /// </summary>
        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7>();
        }

        /// <summary>
        /// Adds the specified component types to the definition.
        /// </summary>
        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8>();
        }

        /// <summary>
        /// Adds the specified component types to the definition.
        /// </summary>
        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9>();
        }

        /// <summary>
        /// Adds the specified component types to the definition.
        /// </summary>
        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>();
        }

        /// <summary>
        /// Adds the specified component types to the definition.
        /// </summary>
        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>();
        }

        /// <summary>
        /// Adds the specified component types to the definition.
        /// </summary>
        public void AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            componentTypes |= schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>();
        }

        /// <summary>
        /// Adds the specified <paramref name="arrayType"/> to this definition.
        /// </summary>
        public void AddArrayType(int arrayType)
        {
            arrayTypes.Set(arrayType);
        }

        /// <summary>
        /// Removes the specified <paramref name="arrayType"/> from the definition.
        /// </summary>
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
        /// Adds the specified array types to the definition.
        /// </summary>
        public void AddArrayTypes<A1, A2>(Schema schema) where A1 : unmanaged where A2 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2>();
        }

        /// <summary>
        /// Adds the specified array types to the definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3>();
        }

        /// <summary>
        /// Adds the specified array types to the definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4>();
        }

        /// <summary>
        /// Adds the specified array types to the definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4, A5>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5>();
        }

        /// <summary>
        /// Adds the specified array types to the definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4, A5, A6>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6>();
        }

        /// <summary>
        /// Adds the specified array types to the definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7>();
        }

        /// <summary>
        /// Adds the specified array types to the definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged where A8 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8>();
        }

        /// <summary>
        /// Adds the specified array types to the definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged where A8 : unmanaged where A9 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9>();
        }

        /// <summary>
        /// Adds the specified array types to the definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged where A8 : unmanaged where A9 : unmanaged where A10 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10>();
        }

        /// <summary>
        /// Adds the specified array types to the definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged where A8 : unmanaged where A9 : unmanaged where A10 : unmanaged where A11 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11>();
        }

        /// <summary>
        /// Adds the specified array types to the definition.
        /// </summary>
        public void AddArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged where A7 : unmanaged where A8 : unmanaged where A9 : unmanaged where A10 : unmanaged where A11 : unmanaged where A12 : unmanaged
        {
            arrayTypes |= schema.GetArrayTypes<A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12>();
        }

        /// <summary>
        /// Adds the specified <paramref name="tagType"/> to this definition.
        /// </summary>
        public void AddTagType(int tagType)
        {
            tagTypes.Set(tagType);
        }

        /// <summary>
        /// Removes the specified <paramref name="tagType"/> from the definition.
        /// </summary>
        public void RemoveTagType(int tagType)
        {
            tagTypes.Clear(tagType);
        }

        /// <summary>
        /// Adds a <typeparamref name="T"/> tag type to this definition.
        /// </summary>
        public void AddTagType<T>(Schema schema) where T : unmanaged
        {
            tagTypes.Set(schema.GetTagType<T>());
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        /// <param name="tagTypes"></param>
        public void AddTagTypes(BitMask tagTypes)
        {
            this.tagTypes |= tagTypes;
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        public void AddTagTypes<T1, T2>(Schema schema) where T1 : unmanaged where T2 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2>();
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        public void AddTagTypes<T1, T2, T3>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3>();
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        public void AddTagTypes<T1, T2, T3, T4>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4>();
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        public void AddTagTypes<T1, T2, T3, T4, T5>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5>();
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        public void AddTagTypes<T1, T2, T3, T4, T5, T6>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6>();
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7>();
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>();
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>();
        }

        /// <summary>
        /// Adds the specified tag types to the definition.
        /// </summary>
        public void AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Schema schema) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            tagTypes |= schema.GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>();
        }

        /// <summary>
        /// Retrieves the definition for entity of type <typeparamref name="T"/>.
        /// </summary>
        public static Definition Get<T>(Schema schema) where T : unmanaged, IEntity
        {
            Archetype archetype = Archetype.Get<T>(schema);
            return archetype.definition;
        }

        /// <inheritdoc/>
        public static bool operator ==(Definition a, Definition b)
        {
            return a.Equals(b);
        }

        /// <inheritdoc/>
        public static bool operator !=(Definition a, Definition b)
        {
            return !a.Equals(b);
        }
    }
}