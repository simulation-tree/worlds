using System;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Contains information about an entity type.
    /// </summary>
    public unsafe struct Definition : IEquatable<Definition>
    {
        /// <summary>
        /// Amount of component types in this definition.
        /// </summary>
        public readonly byte componentTypeCount;

        /// <summary>
        /// Amount of array types in this definition.
        /// </summary>
        public readonly byte arrayTypeCount;

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
                componentTypesMask.Set(type.value);
                componentTypeCount++;
            }

            foreach (ArrayType type in arrayTypes)
            {
                arrayTypesMask.Set(type.value);
                arrayTypeCount++;
            }
        }

        /// <summary>
        /// Creates a new definition with the exact component and array <see cref="BitSet"/> values.
        /// </summary>
        public Definition(BitSet componentTypesMask, BitSet arrayTypesMask)
        {
            componentTypeCount = componentTypesMask.Count;
            arrayTypeCount = arrayTypesMask.Count;
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
                if (componentTypesMask.Contains(i))
                {
                    ComponentType type = new(i);
                    length += type.ToString(buffer.Slice(length));
                    buffer[length++] = ',';
                    buffer[length++] = ' ';
                }
            }

            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (arrayTypesMask.Contains(i))
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
        public readonly byte CopyComponentTypesTo(USpan<ComponentType> buffer)
        {
            byte count = 0;
            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (componentTypesMask.Contains(i))
                {
                    buffer[count] = new(i);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies the array types in this definition to the <paramref name="buffer"/>.
        /// </summary>
        public readonly byte CopyArrayTypesTo(USpan<ArrayType> buffer)
        {
            byte count = 0;
            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (arrayTypesMask.Contains(i))
                {
                    buffer[count] = new(i);
                    count++;
                }
            }

            return count;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(componentTypeCount, arrayTypeCount, componentTypesMask, arrayTypesMask);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Definition definition && Equals(definition);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Definition other)
        {
            return other.arrayTypeCount == arrayTypeCount && other.componentTypeCount == componentTypeCount && other.arrayTypesMask == arrayTypesMask && other.componentTypesMask == componentTypesMask;
        }

        /// <summary>
        /// Checks if this definition contains the specified <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(ComponentType componentType)
        {
            return componentTypesMask.Contains(componentType.value);
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
            return arrayTypesMask.Contains(arrayType.value);
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
        public readonly Definition AddComponentTypes(USpan<ComponentType> componentTypes)
        {
            BitSet componentTypesMask = this.componentTypesMask;
            foreach (ComponentType type in componentTypes)
            {
                componentTypesMask.Set(type.value);
            }

            return new(componentTypesMask, arrayTypesMask);
        }

        /// <summary>
        /// Adds the specified <paramref name="arrayTypes"/> to this definition.
        /// </summary>
        public readonly Definition AddArrayTypes(USpan<ArrayType> arrayTypes)
        {
            BitSet arrayTypesMask = this.arrayTypesMask;
            foreach (ArrayType type in arrayTypes)
            {
                arrayTypesMask.Set(type.value);
            }

            return new(componentTypesMask, arrayTypesMask);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T"/> component type to this definition.
        /// </summary>
        public readonly Definition AddComponentType<T>() where T : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[1] { ComponentType.Get<T>() };
            return AddComponentTypes(types);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T1"/> and <typeparamref name="T2"/> component types to this definition.
        /// </summary>
        public readonly Definition AddComponentTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[2] { ComponentType.Get<T1>(), ComponentType.Get<T2>() };
            return AddComponentTypes(types);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T1"/>, <typeparamref name="T2"/> and <typeparamref name="T3"/> component types to this definition.
        /// </summary>
        public readonly Definition AddComponentTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[3] { ComponentType.Get<T1>(), ComponentType.Get<T2>(), ComponentType.Get<T3>() };
            return AddComponentTypes(types);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/> and <typeparamref name="T4"/> component types to this definition.
        /// </summary>
        public readonly Definition AddComponentTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[4] { ComponentType.Get<T1>(), ComponentType.Get<T2>(), ComponentType.Get<T3>(), ComponentType.Get<T4>() };
            return AddComponentTypes(types);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/> and <typeparamref name="T5"/> component types to this definition.
        /// </summary>
        public readonly Definition AddComponentTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[5] { ComponentType.Get<T1>(), ComponentType.Get<T2>(), ComponentType.Get<T3>(), ComponentType.Get<T4>(), ComponentType.Get<T5>() };
            return AddComponentTypes(types);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/> and <typeparamref name="T6"/> component types to this definition.
        /// </summary>
        public readonly Definition AddComponentTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[6] { ComponentType.Get<T1>(), ComponentType.Get<T2>(), ComponentType.Get<T3>(), ComponentType.Get<T4>(), ComponentType.Get<T5>(), ComponentType.Get<T6>() };
            return AddComponentTypes(types);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T"/> array type to this definition.
        /// </summary>
        public readonly Definition AddArrayType<T>() where T : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[1] { ArrayType.Get<T>() };
            return AddArrayTypes(types);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T1"/> and <typeparamref name="T2"/> array types to this definition.
        /// </summary>
        public readonly Definition AddArrayTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[2] { ArrayType.Get<T1>(), ArrayType.Get<T2>() };
            return AddArrayTypes(types);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T1"/>, <typeparamref name="T2"/> and <typeparamref name="T3"/> array types to this definition.
        /// </summary>
        public readonly Definition AddArrayTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[3] { ArrayType.Get<T1>(), ArrayType.Get<T2>(), ArrayType.Get<T3>() };
            return AddArrayTypes(types);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/> and <typeparamref name="T4"/> array types to this definition.
        /// </summary>
        public readonly Definition AddArrayTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[4] { ArrayType.Get<T1>(), ArrayType.Get<T2>(), ArrayType.Get<T3>(), ArrayType.Get<T4>() };
            return AddArrayTypes(types);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/> and <typeparamref name="T5"/> array types to this definition.
        /// </summary>
        public readonly Definition AddArrayTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[5] { ArrayType.Get<T1>(), ArrayType.Get<T2>(), ArrayType.Get<T3>(), ArrayType.Get<T4>(), ArrayType.Get<T5>() };
            return AddArrayTypes(types);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/> and <typeparamref name="T6"/> array types to this definition.
        /// </summary>
        public readonly Definition AddArrayTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[6] { ArrayType.Get<T1>(), ArrayType.Get<T2>(), ArrayType.Get<T3>(), ArrayType.Get<T4>(), ArrayType.Get<T5>(), ArrayType.Get<T6>() };
            return AddArrayTypes(types);
        }

        /// <summary>
        /// Retrieves the definition for the specified <typeparamref name="T"/> entity type.
        /// </summary>
        public static Definition Get<T>() where T : unmanaged, IEntity
        {
            return default(T).Definition;
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