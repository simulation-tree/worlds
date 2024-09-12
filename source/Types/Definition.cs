using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Contains information about an entity type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    public unsafe struct Definition : IEquatable<Definition>
    {
        public const uint MaxTypes = 30;

        private fixed uint types[(int)MaxTypes];
        private uint typesMask;
        private byte componentTypes;
        private byte arrayTypes;
        //private ushort extraSpace;

        public readonly byte ComponentTypeCount => componentTypes;
        public readonly byte ArrayTypeCount => arrayTypes;
        public readonly byte TotalTypeCount => (byte)(componentTypes + arrayTypes);

        public readonly (RuntimeType type, bool isArray) this[uint index]
        {
            get
            {
                ThrowIfIndexOutOfRange(index);
                RuntimeType typeValue = new(types[index]);
                bool isArray = (typesMask & (1UL << (byte)index)) != 0;
                return (typeValue, isArray);
            }
        }

        public Definition(USpan<RuntimeType> componentTypes, USpan<RuntimeType> arrayTypes)
        {
            ThrowIfTypeCountIsTooGreat(componentTypes.length + arrayTypes.length);
            this.componentTypes = (byte)componentTypes.length;
            this.arrayTypes = (byte)arrayTypes.length;
            typesMask = 0;
            byte index = 0;
            for (uint i = 0; i < componentTypes.length; i++)
            {
                types[index] = componentTypes[i].value;
                typesMask = (uint)(typesMask & ~(1UL << index));
                index++;
            }

            for (uint i = 0; i < arrayTypes.length; i++)
            {
                types[index] = arrayTypes[i].value;
                typesMask = (uint)(typesMask | 1UL << index);
                index++;
            }
        }

        public readonly bool IsArrayType(uint index)
        {
            ThrowIfIndexOutOfRange(index);
            return (typesMask & (1UL << (byte)index)) != 0;
        }

        public readonly uint CopyComponentTypes(USpan<RuntimeType> buffer)
        {
            uint count = 0;
            byte typeCount = TotalTypeCount;
            for (uint i = 0; i < typeCount; i++)
            {
                if (!IsArrayType(i))
                {
                    buffer[count] = new(types[i]);
                    count++;
                }
            }

            return count;
        }

        public readonly uint CopyArrayTypes(USpan<RuntimeType> buffer)
        {
            uint count = 0;
            byte typeCount = TotalTypeCount;
            for (uint i = 0; i < typeCount; i++)
            {
                if (IsArrayType(i))
                {
                    buffer[count] = new(types[i]);
                    count++;
                }
            }

            return count;
        }

        public readonly uint CopyAllTypes(USpan<RuntimeType> buffer)
        {
            byte typeCount = TotalTypeCount;
            for (uint i = 0; i < typeCount; i++)
            {
                buffer[i] = new(types[i]);
            }

            return typeCount;
        }

        public readonly override int GetHashCode()
        {
            USpan<RuntimeType> buffer = stackalloc RuntimeType[(int)MaxTypes];
            uint count = CopyAllTypes(buffer);
            return RuntimeType.CombineHash(buffer.Slice(0, count));
        }

        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Definition definition && Equals(definition);
        }

        public readonly bool Equals(Definition other)
        {
            byte typeCount = TotalTypeCount;
            byte otherTypeCount = other.TotalTypeCount;
            if (typeCount != otherTypeCount)
            {
                return false;
            }

            //check if other type contains our types
            for (uint i = 0; i < typeCount; i++)
            {
                bool isArray = IsArrayType(i);
                uint type = types[i];
                bool contains = false;
                for (uint j = 0; j < otherTypeCount; j++)
                {
                    if (type == other.types[j] && isArray == other.IsArrayType(j))
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains)
                {
                    return false;
                }
            }

            return true;
        }

        public readonly Definition AddComponentType(RuntimeType type)
        {
            ThrowIfFull();
            ThrowIfComponentTypeAlreadyExists(type);
            byte typeCount = TotalTypeCount;
            Definition newDefinition = this;
            newDefinition.types[typeCount] = type.value;
            newDefinition.typesMask = (uint)(typesMask & ~(1UL << typeCount));
            newDefinition.componentTypes++;
            return newDefinition;
        }

        public readonly Definition AddComponentType<T>() where T : unmanaged
        {
            return AddComponentType(RuntimeType.Get<T>());
        }

        public readonly Definition AddComponentTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            return AddComponentType<T1>().AddComponentType<T2>();
        }

        public readonly Definition AddComponentTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            return AddComponentType<T1>().AddComponentType<T2>().AddComponentType<T3>();
        }

        public readonly Definition AddComponentTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            return AddComponentType<T1>().AddComponentType<T2>().AddComponentType<T3>().AddComponentType<T4>();
        }

        public readonly Definition AddComponentTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            return AddComponentType<T1>().AddComponentType<T2>().AddComponentType<T3>().AddComponentType<T4>().AddComponentType<T5>();
        }

        public readonly Definition AddArrayType(RuntimeType type)
        {
            ThrowIfFull();
            ThrowIfArrayTypeAlreadyExists(type);
            byte typeCount = TotalTypeCount;
            Definition newDefinition = this;
            newDefinition.types[typeCount] = type.value;
            newDefinition.typesMask = (uint)(typesMask | 1UL << typeCount);
            newDefinition.arrayTypes++;
            return newDefinition;
        }

        public readonly Definition AddArrayType<T>() where T : unmanaged
        {
            return AddArrayType(RuntimeType.Get<T>());
        }

        public readonly Definition AddArrayTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            return AddArrayType<T1>().AddArrayType<T2>();
        }

        public readonly Definition AddArrayTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            return AddArrayType<T1>().AddArrayType<T2>().AddArrayType<T3>();
        }

        public readonly Definition AddArrayTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            return AddArrayType<T1>().AddArrayType<T2>().AddArrayType<T3>().AddArrayType<T4>();
        }

        public readonly Definition AddArrayTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            return AddArrayType<T1>().AddArrayType<T2>().AddArrayType<T3>().AddArrayType<T4>().AddArrayType<T5>();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfFull()
        {
            if (TotalTypeCount == MaxTypes)
            {
                throw new InvalidOperationException("Definition is full, unable to contain any more types");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfTypeCountIsTooGreat(uint count)
        {
            if (count > MaxTypes)
            {
                throw new InvalidOperationException($"Definition cannot contain more than {MaxTypes} types");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfIndexOutOfRange(uint index)
        {
            if (index >= TotalTypeCount)
            {
                throw new IndexOutOfRangeException();
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeAlreadyExists(RuntimeType type)
        {
            uint typeCount = TotalTypeCount;
            for (uint i = 0; i < typeCount; i++)
            {
                if (types[i] == type.value)
                {
                    if (!IsArrayType(i))
                    {
                        throw new InvalidOperationException($"Component type {type} already exists in definition");
                    }
                }
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeAlreadyExists(RuntimeType type)
        {
            uint typeCount = TotalTypeCount;
            for (uint i = 0; i < typeCount; i++)
            {
                if (types[i] == type.value)
                {
                    if (IsArrayType(i))
                    {
                        throw new InvalidOperationException($"Array type {type} already exists in definition");
                    }
                }
            }
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