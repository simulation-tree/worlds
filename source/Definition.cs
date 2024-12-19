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
        /// Copies the component types in this definition to the <paramref name="buffer"/>.
        /// </summary>
        /// <returns>Amount of component types copied.</returns>
        public readonly byte CopyComponentTypesTo(USpan<ComponentType> buffer)
        {
            byte count = 0;
            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (componentTypesMask == i)
                {
                    buffer[count] = ComponentType.All[i];
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies the array types in this definition to the <paramref name="buffer"/>.
        /// </summary>
        /// <returns>Amount of array types copied.</returns>
        public readonly byte CopyArrayTypesTo(USpan<ArrayType> buffer)
        {
            byte count = 0;
            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (arrayTypesMask == i)
                {
                    buffer[count] = ArrayType.All[i];
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
        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            return ContainsComponent(ComponentType.Get<T>());
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
        public readonly bool ContainsArray<T>() where T : unmanaged
        {
            return ContainsArray(ArrayType.Get<T>());
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
        public Definition AddComponentType<C1>() where C1 : unmanaged
        {
            componentTypesMask |= ComponentType.Get<C1>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/> and <typeparamref name="C2"/> component types to this definition.
        /// </summary>
        public Definition AddComponentTypes<C1, C2>() where C1 : unmanaged where C2 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/>, <typeparamref name="C2"/> and <typeparamref name="C3"/> component types to this definition.
        /// </summary>
        public Definition AddComponentTypes<C1, C2, C3>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/>, <typeparamref name="C2"/>, <typeparamref name="C3"/> and <typeparamref name="C4"/> component types to this definition.
        /// </summary>
        public Definition AddComponentTypes<C1, C2, C3, C4>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/>, <typeparamref name="C2"/>, <typeparamref name="C3"/>, <typeparamref name="C4"/> and <typeparamref name="C5"/> component types to this definition.
        /// </summary>
        public Definition AddComponentTypes<C1, C2, C3, C4, C5>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/>, <typeparamref name="C2"/>, <typeparamref name="C3"/>, <typeparamref name="C4"/>, <typeparamref name="C5"/> and <typeparamref name="C6"/> component types to this definition.
        /// </summary>
        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>();
            return this;
        }

        public Definition AddComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            componentTypesMask |= ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>();
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
        /// Adds the specified <typeparamref name="C1"/> array type to this definition.
        /// </summary>
        public Definition AddArrayType<C1>() where C1 : unmanaged
        {
            arrayTypesMask |= ArrayType.Get<C1>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/> and <typeparamref name="C2"/> array types to this definition.
        /// </summary>
        public Definition AddArrayTypes<C1, C2>() where C1 : unmanaged where C2 : unmanaged
        {
            arrayTypesMask |= ArrayType.GetBitSet<C1, C2>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/>, <typeparamref name="C2"/> and <typeparamref name="C3"/> array types to this definition.
        /// </summary>
        public Definition AddArrayTypes<C1, C2, C3>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            arrayTypesMask |= ArrayType.GetBitSet<C1, C2, C3>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/>, <typeparamref name="C2"/>, <typeparamref name="C3"/> and <typeparamref name="C4"/> array types to this definition.
        /// </summary>
        public Definition AddArrayTypes<C1, C2, C3, C4>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            arrayTypesMask |= ArrayType.GetBitSet<C1, C2, C3, C4>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/>, <typeparamref name="C2"/>, <typeparamref name="C3"/>, <typeparamref name="C4"/> and <typeparamref name="C5"/> array types to this definition.
        /// </summary>
        public Definition AddArrayTypes<C1, C2, C3, C4, C5>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            arrayTypesMask |= ArrayType.GetBitSet<C1, C2, C3, C4, C5>();
            return this;
        }

        /// <summary>
        /// Adds the specified <typeparamref name="C1"/>, <typeparamref name="C2"/>, <typeparamref name="C3"/>, <typeparamref name="C4"/>, <typeparamref name="C5"/> and <typeparamref name="C6"/> array types to this definition.
        /// </summary>
        public Definition AddArrayTypes<C1, C2, C3, C4, C5, C6>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            arrayTypesMask |= ArrayType.GetBitSet<C1, C2, C3, C4, C5, C6>();
            return this;
        }

        /// <summary>
        /// Retrieves the definition for the specified <typeparamref name="T"/> entity type.
        /// </summary>
        public static Definition Get<T>() where T : unmanaged, IEntity
        {
            return default(T).Definition;
        }

        public static Definition Get<T1, T2>() where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity
        {
            Definition t1 = default(T1).Definition;
            Definition t2 = default(T2).Definition;
            return new Definition(t1.componentTypesMask | t2.componentTypesMask, t1.arrayTypesMask | t2.arrayTypesMask);
        }

        public static Definition Get<T1, T2, T3>() where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity
        {
            Definition t1 = default(T1).Definition;
            Definition t2 = default(T2).Definition;
            Definition t3 = default(T3).Definition;
            return new Definition(t1.componentTypesMask | t2.componentTypesMask | t3.componentTypesMask, t1.arrayTypesMask | t2.arrayTypesMask | t3.arrayTypesMask);
        }

        public static Definition Get<T1, T2, T3, T4>() where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity
        {
            Definition t1 = default(T1).Definition;
            Definition t2 = default(T2).Definition;
            Definition t3 = default(T3).Definition;
            Definition t4 = default(T4).Definition;
            return new Definition(t1.componentTypesMask | t2.componentTypesMask | t3.componentTypesMask | t4.componentTypesMask, t1.arrayTypesMask | t2.arrayTypesMask | t3.arrayTypesMask | t4.arrayTypesMask);
        }

        public static Definition Get<T1, T2, T3, T4, T5>() where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity where T5 : unmanaged, IEntity
        {
            Definition t1 = default(T1).Definition;
            Definition t2 = default(T2).Definition;
            Definition t3 = default(T3).Definition;
            Definition t4 = default(T4).Definition;
            Definition t5 = default(T5).Definition;
            return new Definition(t1.componentTypesMask | t2.componentTypesMask | t3.componentTypesMask | t4.componentTypesMask | t5.componentTypesMask, t1.arrayTypesMask | t2.arrayTypesMask | t3.arrayTypesMask | t4.arrayTypesMask | t5.arrayTypesMask);
        }

        public static Definition Get<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity where T5 : unmanaged, IEntity where T6 : unmanaged, IEntity
        {
            Definition t1 = default(T1).Definition;
            Definition t2 = default(T2).Definition;
            Definition t3 = default(T3).Definition;
            Definition t4 = default(T4).Definition;
            Definition t5 = default(T5).Definition;
            Definition t6 = default(T6).Definition;
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