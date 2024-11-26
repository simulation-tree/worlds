using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Represents an unmanaged array type usable with entities.
    /// </summary>
    public readonly struct ArrayType : IEquatable<ArrayType>
    {
        private static readonly Dictionary<Type, ArrayType> systemTypeToType = new();
        private static readonly List<Type> systemTypes = new();
        private static readonly List<ushort> sizes = new();
        private static readonly List<ArrayType> all = new();

        /// <summary>
        /// All registered array types.
        /// </summary>
        public static IReadOnlyList<ArrayType> All => all;

        /// <summary>
        /// Index of the array type within a <see cref="BitSet"/>.
        /// </summary>
        public readonly byte value;

        /// <summary>
        /// Underlying <see cref="Type"/> that this array type represents.
        /// </summary>
        public readonly Type SystemType => systemTypes[value];

        /// <summary>
        /// Name of the array type.
        /// </summary>
        public readonly USpan<char> Name => SystemType.Name.AsUSpan();

        /// <summary>
        /// Full name of the array type in the format `{Namespace}.{Name}`.
        /// </summary>
        public readonly USpan<char> FullName => (SystemType.FullName ?? string.Empty).AsUSpan();

        /// <summary>
        /// Namespace of the array type.
        /// </summary>
        public readonly USpan<char> Namespace => (SystemType.Namespace ?? string.Empty).AsUSpan();

        /// <summary>
        /// Byte size of the array type.
        /// </summary>
        public readonly ushort Size => sizes[value];

        /// <summary>
        /// Not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public ArrayType()
        {
            throw new NotSupportedException();
        }

        internal ArrayType(byte value)
        {
            this.value = value;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this array type.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            USpan<char> namespac = Namespace;
            USpan<char> name = Name;
            uint length = 0;
            if (namespac.Length > 0)
            {
                length += namespac.CopyTo(buffer);
                buffer[length++] = '.';
            }

            length += name.CopyTo(buffer.Slice(length));
            return length;
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Type type && Equals(type);
        }

        /// <inheritdoc/>
        public readonly bool Equals(ArrayType other)
        {
            return value == other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return value.GetHashCode();
        }

        /// <summary>
        /// Registers or retrieves an array type for the given system type.
        /// </summary>
        public static ArrayType Register<T>() where T : unmanaged
        {
            Type systemType = typeof(T);
            if (!systemTypeToType.TryGetValue(systemType, out ArrayType type))
            {
                byte index = (byte)systemTypes.Count;
                type = new(index);
                systemTypeToType.Add(systemType, type);
                systemTypes.Add(systemType);
                sizes.Add((ushort)TypeInfo<T>.size);
                all.Add(type);
            }

            return type;
        }

        /// <summary>
        /// Retrieves the array type for the given system type.
        /// </summary>
        public static ArrayType Get<T>() where T : unmanaged
        {
            ThrowIfTypeDoesntExist<T>();
            return TypeCache<T>.type;
        }

        internal static class TypeCache<T> where T : unmanaged
        {
            public static readonly ArrayType type = systemTypeToType[typeof(T)];
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the given <typeparamref name="T"/> is already registered.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfTypeAlreadyExists<T>() where T : unmanaged
        {
            if (systemTypeToType.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"Array type `{typeof(T)}` has already been registered");
            }
        }

        /// <summary>
        /// Throws an <see cref="NullReferenceException"/> if the given <typeparamref name="T"/> is not registered.
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfTypeDoesntExist<T>() where T : unmanaged
        {
            if (!systemTypeToType.ContainsKey(typeof(T)))
            {
                throw new NullReferenceException($"Array type `{typeof(T)}` is not registered");
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(ArrayType left, ArrayType right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ArrayType left, ArrayType right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static implicit operator byte(ArrayType type)
        {
            return type.value;
        }
    }
}
