using System;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Contains information about an entity type.
    /// </summary>
    public unsafe struct Definition : IEquatable<Definition>
    {
        public readonly byte componentTypeCount;
        public readonly byte arrayTypeCount;

        private BitSet componentTypesMask;
        private BitSet arrayTypesMask;

        public readonly BitSet ComponentTypesMask => componentTypesMask;
        public readonly BitSet ArrayTypesMask => arrayTypesMask;

        public Definition(USpan<ComponentType> componentTypes, USpan<ArrayType> arrayTypes)
        {
            foreach (ComponentType type in componentTypes)
            {
                componentTypesMask.Set(type.value);
                this.componentTypeCount++;
            }

            foreach (ArrayType type in arrayTypes)
            {
                arrayTypesMask.Set(type.value);
                this.arrayTypeCount++;
            }
        }

        public Definition(byte componentTypeCount, byte arrayTypeCount, BitSet componentTypesMask, BitSet arrayTypesMask)
        {
            this.componentTypeCount = componentTypeCount;
            this.arrayTypeCount = arrayTypeCount;
            this.componentTypesMask = componentTypesMask;
            this.arrayTypesMask = arrayTypesMask;
        }

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

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(componentTypeCount, arrayTypeCount, componentTypesMask, arrayTypesMask);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Definition definition && Equals(definition);
        }

        public readonly bool Equals(Definition other)
        {
            return other.arrayTypeCount == arrayTypeCount && other.componentTypeCount == componentTypeCount && other.arrayTypesMask == arrayTypesMask && other.componentTypesMask == componentTypesMask;
        }

        public readonly bool ContainsComponent(ComponentType type)
        {
            return componentTypesMask.Contains(type.value);
        }

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            return ContainsComponent(ComponentType.Get<T>());
        }

        public readonly bool ContainsArray(ArrayType type)
        {
            return arrayTypesMask.Contains(type.value);
        }

        public readonly bool ContainsArray<T>() where T : unmanaged
        {
            return ContainsArray(ArrayType.Get<T>());
        }

        public readonly Definition AddComponentTypes(USpan<ComponentType> types)
        {
            byte componentTypeCount = this.componentTypeCount;
            BitSet componentTypesMask = this.componentTypesMask;
            foreach (ComponentType type in types)
            {
                componentTypesMask.Set(type.value);
                componentTypeCount++;
            }

            return new(componentTypeCount, arrayTypeCount, componentTypesMask, arrayTypesMask);
        }

        public readonly Definition AddArrayTypes(USpan<ArrayType> types)
        {
            byte arrayTypeCount = this.arrayTypeCount;
            BitSet arrayTypesMask = this.arrayTypesMask;
            foreach (ArrayType type in types)
            {
                arrayTypesMask.Set(type.value);
                arrayTypeCount++;
            }

            return new(componentTypeCount, arrayTypeCount, componentTypesMask, arrayTypesMask);
        }

        public readonly Definition AddComponentType<T>() where T : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[1] { ComponentType.Get<T>() };
            return AddComponentTypes(types);
        }

        public readonly Definition AddComponentTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[2] { ComponentType.Get<T1>(), ComponentType.Get<T2>() };
            return AddComponentTypes(types);
        }

        public readonly Definition AddComponentTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[3] { ComponentType.Get<T1>(), ComponentType.Get<T2>(), ComponentType.Get<T3>() };
            return AddComponentTypes(types);
        }

        public readonly Definition AddComponentTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[4] { ComponentType.Get<T1>(), ComponentType.Get<T2>(), ComponentType.Get<T3>(), ComponentType.Get<T4>() };
            return AddComponentTypes(types);
        }

        public readonly Definition AddComponentTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[5] { ComponentType.Get<T1>(), ComponentType.Get<T2>(), ComponentType.Get<T3>(), ComponentType.Get<T4>(), ComponentType.Get<T5>() };
            return AddComponentTypes(types);
        }

        public readonly Definition AddComponentTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            USpan<ComponentType> types = stackalloc ComponentType[6] { ComponentType.Get<T1>(), ComponentType.Get<T2>(), ComponentType.Get<T3>(), ComponentType.Get<T4>(), ComponentType.Get<T5>(), ComponentType.Get<T6>() };
            return AddComponentTypes(types);
        }

        public readonly Definition AddArrayType<T>() where T : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[1] { ArrayType.Get<T>() };
            return AddArrayTypes(types);
        }

        public readonly Definition AddArrayTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[2] { ArrayType.Get<T1>(), ArrayType.Get<T2>() };
            return AddArrayTypes(types);
        }

        public readonly Definition AddArrayTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[3] { ArrayType.Get<T1>(), ArrayType.Get<T2>(), ArrayType.Get<T3>() };
            return AddArrayTypes(types);
        }

        public readonly Definition AddArrayTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[4] { ArrayType.Get<T1>(), ArrayType.Get<T2>(), ArrayType.Get<T3>(), ArrayType.Get<T4>() };
            return AddArrayTypes(types);
        }

        public readonly Definition AddArrayTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[5] { ArrayType.Get<T1>(), ArrayType.Get<T2>(), ArrayType.Get<T3>(), ArrayType.Get<T4>(), ArrayType.Get<T5>() };
            return AddArrayTypes(types);
        }

        public readonly Definition AddArrayTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            USpan<ArrayType> types = stackalloc ArrayType[6] { ArrayType.Get<T1>(), ArrayType.Get<T2>(), ArrayType.Get<T3>(), ArrayType.Get<T4>(), ArrayType.Get<T5>(), ArrayType.Get<T6>() };
            return AddArrayTypes(types);
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfComponentTypeAlreadyExists(ComponentType type)
        {
            for (uint i = 0; i < componentTypeCount; i++)
            {
                if (componentTypesMask.Contains(type.value))
                {
                    throw new InvalidOperationException($"Component type `{type}` already in this definition");
                }
            }
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfArrayTypeAlreadyExists(ArrayType type)
        {
            for (uint i = 0; i < arrayTypeCount; i++)
            {
                if (arrayTypesMask.Contains(type.value))
                {
                    throw new InvalidOperationException($"Array type `{type}` already in this definition");
                }
            }
        }

        public static Definition Get<T>() where T : unmanaged, IEntity
        {
            return default(T).Definition;
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