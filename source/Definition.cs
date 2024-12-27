using System;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Contains information about an entity type.
    /// </summary>
    public struct Definition : IEquatable<Definition>
    {
        private BitSet componentTypesMask;
        private BitSet arrayTypesMask;

        /// <summary>
        /// Mask of component types in this definition.
        /// </summary>
        public readonly BitSet ComponentTypesMask => componentTypesMask;

        /// <summary>
        /// Mask of array types in this definition.
        /// </summary>
        public readonly BitSet ArrayTypesMask => arrayTypesMask;

        /// <summary>
        /// Creates a new definition with the specified <paramref name="componentTypes"/> and <paramref name="arrayTypes"/>.
        /// </summary>
        public Definition(USpan<ComponentType> componentTypes, USpan<ArrayType> arrayTypes)
        {
            foreach (ComponentType type in componentTypes)
            {
                componentTypesMask |= type;
            }

            foreach (ArrayType type in arrayTypes)
            {
                arrayTypesMask |= type;
            }
        }

        /// <summary>
        /// Creates a new definition with the exact component and array <see cref="BitSet"/> values.
        /// </summary>
        public Definition(BitSet componentTypesMask, BitSet arrayTypesMask)
        {
            this.componentTypesMask = componentTypesMask;
            this.arrayTypesMask = arrayTypesMask;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[512];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this definition.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            uint length = 0;
            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (componentTypesMask == i)
                {
                    ComponentType type = new(i);
                    length += type.ToString(buffer.Slice(length));
                    buffer[length++] = ',';
                    buffer[length++] = ' ';
                }
            }

            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (arrayTypesMask == i)
                {
                    ArrayType type = new(i);
                    length += type.ToString(buffer.Slice(length));
                    buffer[length++] = ',';
                    buffer[length++] = ' ';
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
        public readonly byte CopyComponentTypesTo(USpan<ComponentType> destination)
        {
            byte count = 0;
            for (byte c = 0; c < BitSet.Capacity; c++)
            {
                if (componentTypesMask == c)
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
        public readonly byte CopyArrayTypesTo(USpan<ArrayType> destination)
        {
            byte count = 0;
            for (byte a = 0; a < BitSet.Capacity; a++)
            {
                if (arrayTypesMask == a)
                {
                    destination[count] = new(a);
                    count++;
                }
            }

            return count;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(componentTypesMask, arrayTypesMask);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Definition definition && Equals(definition);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Definition other)
        {
            return other.arrayTypesMask == arrayTypesMask && other.componentTypesMask == componentTypesMask;
        }

        /// <summary>
        /// Checks if this definition contains the specified <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(ComponentType componentType)
        {
            return componentTypesMask == componentType;
        }

        /// <summary>
        /// Checks if this definition contains the specified <typeparamref name="T"/> component type.
        /// </summary>
        public readonly bool ContainsComponent<T>(Schema schema) where T : unmanaged
        {
            return ContainsComponent(schema.GetComponent<T>());
        }

        /// <summary>
        /// Checks if this definition contains the specified <paramref name="arrayType"/>.
        /// </summary>
        public readonly bool ContainsArray(ArrayType arrayType)
        {
            return arrayTypesMask == arrayType;
        }

        /// <summary>
        /// Checks if this definition contains the specified <typeparamref name="T"/> array type.
        /// </summary>
        public readonly bool ContainsArray<T>(Schema schema) where T : unmanaged
        {
            return ContainsArray(schema.GetArrayElement<T>());
        }

        /// <summary>
        /// Adds the specified <paramref name="componentTypes"/> to this definition.
        /// </summary>
        public Definition AddComponentTypes(USpan<ComponentType> componentTypes)
        {
            foreach (ComponentType type in componentTypes)
            {
                componentTypesMask |= type;
            }

            return this;
        }

        /// <summary>
        /// Adds the specified <paramref name="arrayTypes"/> to this definition.
        /// </summary>
        public Definition AddArrayTypes(USpan<ArrayType> arrayTypes)
        {
            foreach (ArrayType type in arrayTypes)
            {
                arrayTypesMask |= type;
            }

            return this;
        }

        /// <summary>
        /// Adds the specified <paramref name="componentType"/> to this definition.
        /// </summary>
        public Definition AddComponentType(ComponentType componentType)
        {
            componentTypesMask |= componentType;
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/> component type to this definition.
        /// </summary>
        public Definition AddComponentType<C1>(Schema schema) where C1 : unmanaged
        {
            componentTypesMask |= schema.GetComponent<C1>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/> and <typeparamref name="C2"/> component types to this definition.
        /// </summary>
        public Definition AddComponentTypes<C1, C2>(Schema schema) where C1 : unmanaged where C2 : unmanaged
        {
            componentTypesMask |= schema.GetComponents<C1, C2>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            componentTypesMask |= schema.GetComponents<C1, C2, C3>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            componentTypesMask |= schema.GetComponents<C1, C2, C3, C4>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            componentTypesMask |= schema.GetComponents<C1, C2, C3, C4, C5>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            componentTypesMask |= schema.GetComponents<C1, C2, C3, C4, C5, C6>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            componentTypesMask |= schema.GetComponents<C1, C2, C3, C4, C5, C6, C7>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8>(Schema schema) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            componentTypesMask |= schema.GetComponents<C1, C2, C3, C4, C5, C6, C7, C8>();
            return this;
        }

        /// <summary>
        /// Adds the specified <paramref name="arrayType"/> to this definition.
        /// </summary>
        public Definition AddArrayType(ArrayType arrayType)
        {
            arrayTypesMask |= arrayType;
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/> array type to this definition.
        /// </summary>
        public Definition AddArrayType<A1>(Schema schema) where A1 : unmanaged
        {
            arrayTypesMask |= schema.GetArrayElements<A1>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/> and <typeparamref name="A2"/> array types to this definition.
        /// </summary>
        public Definition AddArrayTypes<A1, A2>(Schema schema) where A1 : unmanaged where A2 : unmanaged
        {
            arrayTypesMask |= schema.GetArrayElements<A1, A2>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/>, <typeparamref name="A2"/> and <typeparamref name="A3"/> array types to this definition.
        /// </summary>
        public Definition AddArrayTypes<A1, A2, A3>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged
        {
            arrayTypesMask |= schema.GetArrayElements<A1, A2, A3>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/>, <typeparamref name="A2"/>, <typeparamref name="A3"/> and <typeparamref name="A4"/> array types to this definition.
        /// </summary>
        public Definition AddArrayTypes<A1, A2, A3, A4>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged
        {
            arrayTypesMask |= schema.GetArrayElements<A1, A2, A3, A4>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/>, <typeparamref name="A2"/>, <typeparamref name="A3"/>, <typeparamref name="A4"/> and <typeparamref name="A5"/> array types to this definition.
        /// </summary>
        public Definition AddArrayTypes<A1, A2, A3, A4, A5>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged
        {
            arrayTypesMask |= schema.GetArrayElements<A1, A2, A3, A4, A5>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="A1"/>, <typeparamref name="A2"/>, <typeparamref name="A3"/>, <typeparamref name="A4"/>, <typeparamref name="A5"/> and <typeparamref name="A6"/> array types to this definition.
        /// </summary>
        public Definition AddArrayTypes<A1, A2, A3, A4, A5, A6>(Schema schema) where A1 : unmanaged where A2 : unmanaged where A3 : unmanaged where A4 : unmanaged where A5 : unmanaged where A6 : unmanaged
        {
            arrayTypesMask |= schema.GetArrayElements<A1, A2, A3, A4, A5, A6>();
            return this;
        }

        /// <summary>
        /// Retrieves the definition for the specified <typeparamref name="T"/> entity type.
        /// </summary>
        public static Definition Get<T>(Schema schema) where T : unmanaged, IEntity
        {
            return default(T).GetDefinition(schema);
        }

        public static Definition Get<T1, T2>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity
        {
            Definition t1 = default(T1).GetDefinition(schema);
            Definition t2 = default(T2).GetDefinition(schema);
            return new Definition(t1.componentTypesMask | t2.componentTypesMask, t1.arrayTypesMask | t2.arrayTypesMask);
        }

        public static Definition Get<T1, T2, T3>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity
        {
            Definition t1 = default(T1).GetDefinition(schema);
            Definition t2 = default(T2).GetDefinition(schema);
            Definition t3 = default(T3).GetDefinition(schema);
            return new Definition(t1.componentTypesMask | t2.componentTypesMask | t3.componentTypesMask, t1.arrayTypesMask | t2.arrayTypesMask | t3.arrayTypesMask);
        }

        public static Definition Get<T1, T2, T3, T4>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity
        {
            Definition t1 = default(T1).GetDefinition(schema);
            Definition t2 = default(T2).GetDefinition(schema);
            Definition t3 = default(T3).GetDefinition(schema);
            Definition t4 = default(T4).GetDefinition(schema);
            return new Definition(t1.componentTypesMask | t2.componentTypesMask | t3.componentTypesMask | t4.componentTypesMask, t1.arrayTypesMask | t2.arrayTypesMask | t3.arrayTypesMask | t4.arrayTypesMask);
        }

        public static Definition Get<T1, T2, T3, T4, T5>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity where T5 : unmanaged, IEntity
        {
            Definition t1 = default(T1).GetDefinition(schema);
            Definition t2 = default(T2).GetDefinition(schema);
            Definition t3 = default(T3).GetDefinition(schema);
            Definition t4 = default(T4).GetDefinition(schema);
            Definition t5 = default(T5).GetDefinition(schema);
            return new Definition(t1.componentTypesMask | t2.componentTypesMask | t3.componentTypesMask | t4.componentTypesMask | t5.componentTypesMask, t1.arrayTypesMask | t2.arrayTypesMask | t3.arrayTypesMask | t4.arrayTypesMask | t5.arrayTypesMask);
        }

        public static Definition Get<T1, T2, T3, T4, T5, T6>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity where T5 : unmanaged, IEntity where T6 : unmanaged, IEntity
        {
            Definition t1 = default(T1).GetDefinition(schema);
            Definition t2 = default(T2).GetDefinition(schema);
            Definition t3 = default(T3).GetDefinition(schema);
            Definition t4 = default(T4).GetDefinition(schema);
            Definition t5 = default(T5).GetDefinition(schema);
            Definition t6 = default(T6).GetDefinition(schema);
            return new Definition(t1.componentTypesMask | t2.componentTypesMask | t3.componentTypesMask | t4.componentTypesMask | t5.componentTypesMask | t6.componentTypesMask, t1.arrayTypesMask | t2.arrayTypesMask | t3.arrayTypesMask | t4.arrayTypesMask | t5.arrayTypesMask | t6.arrayTypesMask);
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