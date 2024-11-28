using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Represents an unmanaged component type usable with entities.
    /// </summary>
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        private static readonly Dictionary<Type, ComponentType> systemTypeToType = new();
        private static readonly List<Type> systemTypes = new();
        private static readonly List<ushort> sizes = new();
        private static readonly List<ComponentType> all = new();

        /// <summary>
        /// All registered component types.
        /// </summary>
        public static IReadOnlyList<ComponentType> All => all;

        /// <summary>
        /// Index of the component type within a <see cref="BitSet"/>.
        /// </summary>
        public readonly byte value;

        /// <summary>
        /// Underlying <see cref="Type"/> that this component type represents.
        /// </summary>
        public readonly Type SystemType => systemTypes[value];

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
        /// Byte size of the component type.
        /// </summary>
        public readonly ushort Size => sizes[value];

        /// <summary>
        /// Not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public ComponentType()
        {
            throw new NotSupportedException();
        }

        internal ComponentType(byte value)
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
            return value == other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return value.GetHashCode();
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
                type = new(index);
                systemTypeToType.Add(systemType, type);
                systemTypes.Add(systemType);
                sizes.Add((ushort)TypeInfo<T>.size);
                all.Add(type);
            }

            return type;
        }

        /// <summary>
        /// Retrieves a component type for the given system type.
        /// </summary>
        public static ComponentType Get<T>() where T : unmanaged
        {
            ThrowIfTypeDoesntExist<T>();
            return TypeCache<T>.type;
        }

        internal static class TypeCache<T> where T : unmanaged
        {
            internal static readonly ComponentType type = systemTypeToType[typeof(T)];
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

        /// <inheritdoc/>
        public static bool operator ==(ComponentType left, ComponentType right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ComponentType left, ComponentType right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static implicit operator byte(ComponentType type)
        {
            return type.value;
        }
    }
}
