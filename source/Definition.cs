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
            ThrowIfTypeCountIsTooGreat(componentTypes.Length + arrayTypes.Length);
            this.componentTypes = (byte)componentTypes.Length;
            this.arrayTypes = (byte)arrayTypes.Length;
            typesMask = 0;
            byte index = 0;
            for (uint i = 0; i < componentTypes.Length; i++)
            {
                types[index] = componentTypes[i].value;
                typesMask = (uint)(typesMask & ~(1UL << index));
                index++;
            }

            for (uint i = 0; i < arrayTypes.Length; i++)
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

        public readonly bool ContainsComponent(RuntimeType type)
        {
            byte typeCount = TotalTypeCount;
            for (uint i = 0; i < typeCount; i++)
            {
                if (types[i] == type.value && !IsArrayType(i))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            return ContainsComponent(RuntimeType.Get<T>());
        }

        public readonly bool ContainsArray(RuntimeType type)
        {
            byte typeCount = TotalTypeCount;
            for (uint i = 0; i < typeCount; i++)
            {
                if (types[i] == type.value && IsArrayType(i))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool ContainsArray<T>() where T : unmanaged
        {
            return ContainsArray(RuntimeType.Get<T>());
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

        public readonly Definition AddComponentTypes(USpan<RuntimeType> types)
        {
            ThrowIfFull();
            byte typeCount = TotalTypeCount;
            ThrowIfTypeCountIsTooGreat(typeCount + types.Length);
            Definition newDefinition = this;
            for (uint i = 0; i < types.Length; i++)
            {
                newDefinition.types[typeCount + i] = types[i].value;
                newDefinition.typesMask = (uint)(typesMask & ~(1UL << (byte)(typeCount + i)));
            }

            newDefinition.componentTypes += (byte)types.Length;
            return newDefinition;
        }

        public readonly Definition AddComponentType<T>() where T : unmanaged
        {
            return AddComponentType(RuntimeType.Get<T>());
        }

        public readonly Definition AddComponentTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[2] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>() };
            return AddComponentTypes(types);
        }

        public readonly Definition AddComponentTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[3] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>() };
            return AddComponentTypes(types);
        }

        public readonly Definition AddComponentTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[4] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>() };
            return AddComponentTypes(types);
        }

        public readonly Definition AddComponentTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[5] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>(), RuntimeType.Get<T5>() };
            return AddComponentTypes(types);
        }

        public readonly Definition AddComponentTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[6] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>(), RuntimeType.Get<T5>(), RuntimeType.Get<T6>() };
            return AddComponentTypes(types);
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

        public readonly Definition AddArrayTypes(USpan<RuntimeType> types)
        {
            ThrowIfFull();
            byte typeCount = TotalTypeCount;
            ThrowIfTypeCountIsTooGreat(typeCount + types.Length);
            Definition newDefinition = this;
            for (uint i = 0; i < types.Length; i++)
            {
                newDefinition.types[typeCount + i] = types[i].value;
                newDefinition.typesMask = (uint)(typesMask | 1UL << (byte)(typeCount + i));
            }

            newDefinition.arrayTypes += (byte)types.Length;
            return newDefinition;
        }

        public readonly Definition AddArrayTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[2] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>() };
            return AddArrayTypes(types);
        }

        public readonly Definition AddArrayTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[3] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>() };
            return AddArrayTypes(types);
        }

        public readonly Definition AddArrayTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[4] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>() };
            return AddArrayTypes(types);
        }

        public readonly Definition AddArrayTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[5] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>(), RuntimeType.Get<T5>() };
            return AddArrayTypes(types);
        }

        public readonly Definition AddArrayTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            USpan<RuntimeType> types = stackalloc RuntimeType[6] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>(), RuntimeType.Get<T5>(), RuntimeType.Get<T6>() };
            return AddArrayTypes(types);
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfFull()
        {
            if (TotalTypeCount == MaxTypes)
            {
                throw new InvalidOperationException("Definition is full, unable to contain any more types");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfTypeCountIsTooGreat(uint count)
        {
            if (count > MaxTypes)
            {
                throw new InvalidOperationException($"Definition cannot contain more than {MaxTypes} types");
            }
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfIndexOutOfRange(uint index)
        {
            if (index >= TotalTypeCount)
            {
                throw new IndexOutOfRangeException();
            }
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfComponentTypeAlreadyExists(RuntimeType type)
        {
            uint typeCount = TotalTypeCount;
            for (uint i = 0; i < typeCount; i++)
            {
                if (types[i] == type.value)
                {
                    if (!IsArrayType(i))
                    {
                        throw new InvalidOperationException($"Component type `{type}` already exists in definition");
                    }
                }
            }
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfArrayTypeAlreadyExists(RuntimeType type)
        {
            uint typeCount = TotalTypeCount;
            for (uint i = 0; i < typeCount; i++)
            {
                if (types[i] == type.value)
                {
                    if (IsArrayType(i))
                    {
                        throw new InvalidOperationException($"Array type `{type}` already exists in definition");
                    }
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