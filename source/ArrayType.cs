using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Represents an unmanaged array type usable with entities.
    /// </summary>
    public readonly struct ArrayType : IEquatable<ArrayType>
    {
        private static readonly Dictionary<Type, ArrayType> systemTypeToType = new();
        private static readonly List<Type> systemTypes = new();
        private static readonly List<ArrayType> all = new();
        private static readonly List<TypeLayout> layouts = new();

        /// <summary>
        /// All registered array types.
        /// </summary>
        public static IReadOnlyList<ArrayType> All => all;

        /// <summary>
        /// Index of the array type within a <see cref="BitSet"/>.
        /// </summary>
        public readonly byte index;

        private readonly ushort size;

        /// <summary>
        /// Byte size of the array type.
        /// </summary>
        public readonly ushort Size
        {
            get
            {
                ThrowIfSizeIsNull();

                return size;
            }
        }

        /// <summary>
        /// Underlying <see cref="Type"/> that this array type represents.
        /// </summary>
        public readonly Type SystemType => systemTypes[index];

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
        /// Not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public ArrayType()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Initializes an existing array type.
        /// </summary>
        public ArrayType(byte value, ushort size = 0)
        {
            this.index = value;
            this.size = size;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSizeIsNull()
        {
            if (size == default)
            {
                throw new NotSupportedException($"Array type `{SystemType}` does not have a size");
            }
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
            return index == other.index;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return index.GetHashCode();
        }

        /// <summary>
        /// Checks if this component type is the same as <typeparamref name="T"/>.
        /// </summary>
        public readonly bool Is<T>() where T : unmanaged
        {
            if (systemTypeToType.TryGetValue(typeof(T), out ArrayType targetType))
            {
                return index == targetType.index;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves a possible <see cref="TypeLayout"/> for this component type.
        /// </summary>
        public readonly bool TryGetLayout(out TypeLayout layout)
        {
            if (index < layouts.Count)
            {
                layout = layouts[index];
                return true;
            }
            else
            {
                layout = default;
                return false;
            }
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
                type = new(index, (ushort)TypeInfo<T>.size);
                systemTypeToType.Add(systemType, type);
                systemTypes.Add(systemType);
                all.Add(type);

                if (TypeLayout.IsRegistered<T>())
                {
                    layouts.Add(TypeLayout.Get<T>());
                }
                else
                {
                    layouts.Add(default);
                }
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

        public static BitSet GetBitSet<C1>() where C1 : unmanaged
        {
            return BitSetCache<C1>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2>() where C1 : unmanaged where C2 : unmanaged
        {
            return BitSetCache<C1, C2>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            return BitSetCache<C1, C2, C3>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5, C6>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5, C6>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5, C6, C7>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5, C6, C7>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>.bitSet;
        }

        public static BitSet GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>() where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            return BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>.bitSet;
        }

        internal static class BitSetCache<C1> where C1 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>());
        }

        internal static class BitSetCache<C1, C2> where C1 : unmanaged where C2 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>());
        }

        internal static class BitSetCache<C1, C2, C3> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>());
        }

        internal static class BitSetCache<C1, C2, C3, C4> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5, C6> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>(), Get<C6>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5, C6, C7> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>(), Get<C6>(), Get<C7>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>(), Get<C6>(), Get<C7>(), Get<C8>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>(), Get<C6>(), Get<C7>(), Get<C8>(), Get<C9>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>(), Get<C6>(), Get<C7>(), Get<C8>(), Get<C9>(), Get<C10>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>(), Get<C6>(), Get<C7>(), Get<C8>(), Get<C9>(), Get<C10>(), Get<C11>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>(), Get<C6>(), Get<C7>(), Get<C8>(), Get<C9>(), Get<C10>(), Get<C11>(), Get<C12>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>(), Get<C6>(), Get<C7>(), Get<C8>(), Get<C9>(), Get<C10>(), Get<C11>(), Get<C12>(), Get<C13>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>(), Get<C6>(), Get<C7>(), Get<C8>(), Get<C9>(), Get<C10>(), Get<C11>(), Get<C12>(), Get<C13>(), Get<C14>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>(), Get<C6>(), Get<C7>(), Get<C8>(), Get<C9>(), Get<C10>(), Get<C11>(), Get<C12>(), Get<C13>(), Get<C14>(), Get<C15>());
        }

        internal static class BitSetCache<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            internal static readonly BitSet bitSet = new(Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>(), Get<C5>(), Get<C6>(), Get<C7>(), Get<C8>(), Get<C9>(), Get<C10>(), Get<C11>(), Get<C12>(), Get<C13>(), Get<C14>(), Get<C15>(), Get<C16>());
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
            return type.index;
        }
    }
}
