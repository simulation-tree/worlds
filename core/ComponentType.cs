using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Simulation
{
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        private static readonly Dictionary<Type, ComponentType> systemTypeToType = new();
        private static readonly List<Type> systemTypes = new();
        private static readonly List<ushort> sizes = new();
        private static readonly List<ComponentType> all = new();

        static ComponentType()
        {
            RuntimeHelpers.RunClassConstructor(typeof(World).TypeHandle);
        }

        public static IReadOnlyList<ComponentType> All => all;

        public readonly byte value;

        public readonly Type SystemType => systemTypes[value];
        public readonly USpan<char> Name => SystemType.Name.AsUSpan();
        public readonly USpan<char> FullName => (SystemType.FullName ?? string.Empty).AsUSpan();
        public readonly USpan<char> Namespace => (SystemType.Namespace ?? string.Empty).AsUSpan();
        public readonly ushort Size => sizes[value];

        [Obsolete("Default constructor not supported", true)]
        public ComponentType()
        {
            throw new NotImplementedException();
        }

        internal ComponentType(byte value)
        {
            this.value = value;
        }

        public readonly override string ToString()
        {
            return SystemType.ToString();
        }

        public readonly uint ToString(USpan<char> buffer)
        {
            USpan<char> name = Name;
            return name.CopyTo(buffer);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Type type && Equals(type);
        }

        public readonly bool Equals(ComponentType other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static ComponentType Register<T>() where T : unmanaged
        {
            ThrowIfTypeAlreadyExists<T>();
            byte index = (byte)systemTypes.Count;
            ComponentType type = new(index);
            Type systemType = typeof(T);
            systemTypeToType.Add(systemType, type);
            systemTypes.Add(systemType);
            sizes.Add((ushort)TypeInfo<T>.size);
            all.Add(type);
            return type;
        }

        public static ComponentType Get<T>() where T : unmanaged
        {
            ThrowIfTypeDoesntExist<T>();
            return systemTypeToType[typeof(T)];
        }

        public static int CombineHash(USpan<ComponentType> types)
        {
            uint typeCount = types.Length;
            if (typeCount == 0)
            {
                return 0;
            }

            USpan<ComponentType> typesSpan = stackalloc ComponentType[(int)typeCount];
            types.CopyTo(typesSpan);
            uint hash = 0;
            uint max = 0;
            uint index = 0;
            while (true)
            {
                max = 0;
                index = 0;
                for (uint i = 0; i < typeCount; i++)
                {
                    ComponentType type = typesSpan[i];
                    if (type.value > max)
                    {
                        max = type.value;
                        index = i;
                    }
                }

                unchecked
                {
                    hash += max * 174440041u;
                }

                ComponentType last = typesSpan[typeCount - 1];
                typesSpan[index] = last;
                typeCount--;
                if (typeCount == 0)
                {
                    break;
                }
            }

            unchecked
            {
                return (int)hash;
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfTypeAlreadyExists<T>() where T : unmanaged
        {
            if (systemTypeToType.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"Component type `{typeof(T)}` has already been registered");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfTypeDoesntExist<T>() where T : unmanaged
        {
            if (!systemTypeToType.ContainsKey(typeof(T)))
            {
                throw new NullReferenceException($"Component type `{typeof(T)}` is not registered");
            }
        }

        public static bool operator ==(ComponentType left, ComponentType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentType left, ComponentType right)
        {
            return !(left == right);
        }
    }
}
