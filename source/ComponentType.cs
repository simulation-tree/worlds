using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Represents an unmanaged component type usable with entities.
    /// </summary>
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        private static readonly Dictionary<Type, ComponentType> systemTypeToType = new();
        private static readonly List<Type> systemTypes = new();
        private static readonly List<ComponentType> all = new();

        /// <summary>
        /// All registered component types.
        /// </summary>
        public static IReadOnlyList<ComponentType> All => all;

        /// <summary>
        /// Index of the component type within a <see cref="BitSet"/>.
        /// </summary>
        public readonly byte index;

        private readonly ushort size;

        /// <summary>
        /// Byte size of the component type.
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
        /// Underlying <see cref="Type"/> that this component type represents.
        /// </summary>
        public readonly Type SystemType => systemTypes[index];

        /// <summary>
        /// Name of the component type.
        /// </summary>
        public readonly USpan<char> Name => SystemType.Name.AsUSpan();

        /// <summary>
        /// Full name of the component type in the format `{Namespace}.{Name}`.
        /// </summary>
        public readonly USpan<char> FullName => (SystemType.FullName ?? string.Empty).AsUSpan();

        /// <summary>
        /// Namespace of the component type.
        /// </summary>
        public readonly USpan<char> Namespace => (SystemType.Namespace ?? string.Empty).AsUSpan();

        /// <summary>
        /// Not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public ComponentType()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Initializes an existing component type with the given index.
        /// </summary>
        public ComponentType(byte value, ushort size = 0)
        {
            this.index = value;
            this.size = size;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSizeIsNull()
        {
            if (size == default)
            {
                throw new InvalidOperationException($"Component type `{SystemType}` has no size");
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
        /// Builds a string representation of this component type.
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
        public readonly bool Equals(ComponentType other)
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
            if (systemTypeToType.TryGetValue(typeof(T), out ComponentType targetType))
            {
                return index == targetType.index;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Registers or retrieves a component type for the given system type.
        /// </summary>
        public static ComponentType Register<T>() where T : unmanaged
        {
            Type systemType = typeof(T);
            if (!systemTypeToType.TryGetValue(systemType, out ComponentType type))
            {
                byte index = (byte)systemTypes.Count;
                type = new(index, (ushort)TypeInfo<T>.size);
                systemTypeToType.Add(systemType, type);
                systemTypes.Add(systemType);
                all.Add(type);
            }

            return type;
        }

        /// <summary>
        /// Retrieves a component type for the given system type.
        /// </summary>
        public static ComponentType Get<C>() where C : unmanaged
        {
            ThrowIfTypeDoesntExist<C>();

            return TypeCache<C>.type;
        }

        internal static class TypeCache<C> where C : unmanaged
        {
            internal static readonly ComponentType type = systemTypeToType[typeof(C)];
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
        /// Throws an <see cref="InvalidOperationException"/> if the given <typeparamref name="T"/> has already been registered.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfTypeAlreadyExists<T>() where T : unmanaged
        {
            if (systemTypeToType.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"Component type `{typeof(T)}` has already been registered");
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

        public static implicit operator byte(ComponentType type)
        {
            return type.index;
        }
    }
}
