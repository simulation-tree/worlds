using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
{
    public readonly struct ArrayType : IEquatable<ArrayType>
    {
        private static readonly Dictionary<Type, ArrayType> systemTypeToType = new();
        private static readonly List<Type> systemTypes = new();
        private static readonly List<ushort> sizes = new();
        private static readonly List<ArrayType> all = new();

        public static IReadOnlyList<ArrayType> All => all;

        public readonly byte value;

        public readonly Type SystemType => systemTypes[value];
        public readonly USpan<char> Name => SystemType.Name.AsUSpan();
        public readonly USpan<char> FullName => (SystemType.FullName ?? string.Empty).AsUSpan();
        public readonly USpan<char> Namespace => (SystemType.Namespace ?? string.Empty).AsUSpan();
        public readonly ushort Size => sizes[value];

        [Obsolete("Default constructor not supported", true)]
        public ArrayType()
        {
            throw new NotImplementedException();
        }

        internal ArrayType(byte value)
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

        public readonly bool Equals(ArrayType other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static ArrayType Register<T>() where T : unmanaged
        {
            ThrowIfTypeAlreadyExists<T>();
            byte index = (byte)systemTypes.Count;
            ArrayType type = new(index);
            Type systemType = typeof(T);
            systemTypeToType.Add(systemType, type);
            systemTypes.Add(systemType);
            sizes.Add((ushort)TypeInfo<T>.size);
            all.Add(type);
            return type;
        }

        public static ArrayType Get<T>() where T : unmanaged
        {
            ThrowIfTypeDoesntExist<T>();
            return systemTypeToType[typeof(T)];
        }

        [Conditional("DEBUG")]
        public static void ThrowIfTypeAlreadyExists<T>() where T : unmanaged
        {
            if (systemTypeToType.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"Array type `{typeof(T)}` has already been registered");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfTypeDoesntExist<T>() where T : unmanaged
        {
            if (!systemTypeToType.ContainsKey(typeof(T)))
            {
                throw new NullReferenceException($"Array type `{typeof(T)}` is not registered");
            }
        }

        public static bool operator ==(ArrayType left, ArrayType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArrayType left, ArrayType right)
        {
            return !(left == right);
        }

        public static implicit operator byte(ArrayType type)
        {
            return type.value;
        }
    }
}
