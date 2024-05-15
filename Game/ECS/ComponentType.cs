using System;
using System.Collections.Generic;
using Unmanaged;

namespace Game
{
    /// <summary>
    /// Represents a type of a component on an entity.
    /// </summary>
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        private static readonly RuntimeType[] runtimeTypes = new RuntimeType[MaxTypes];
        private static readonly Dictionary<RuntimeType, ComponentType> typeMap = [];
        private static ushort count = 0;

        public const byte MaxTypes = 64;

        /// <summary>
        /// The unique value of this type.
        /// </summary>
        public readonly byte value;

        public readonly RuntimeType RuntimeType => runtimeTypes[value - 1];
        public readonly bool IsValid => value > 0 && value <= count;

        public ComponentType(byte value)
        {
            this.value = value;
        }

        public ComponentType(int value)
        {
            this.value = (byte)value;
        }

        public ComponentType(uint value)
        {
            this.value = (byte)value;
        }

        public override string ToString()
        {
            return RuntimeType.ToString();
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is ComponentType type && Equals(type);
        }

        public readonly bool Equals(ComponentType other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static bool operator ==(ComponentType left, ComponentType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentType left, ComponentType right)
        {
            return !(left == right);
        }

        public static ComponentType Get<T>() where T : unmanaged
        {
            RuntimeType runtimeType = RuntimeType.Get<T>();
            if (typeMap.TryGetValue(runtimeType, out ComponentType type))
            {
                return type;
            }

            if (count >= MaxTypes)
            {
                throw new InvalidOperationException("Too many component types registered.");
            }

            type = new ComponentType(count + 1);
            runtimeTypes[count] = runtimeType;
            typeMap[runtimeType] = type;
            count++;
            return type;
        }

        public static void Reset()
        {
            typeMap.Clear();
            Array.Clear(runtimeTypes, 0, count);
            count = 0;
        }

        public static RuntimeType?[] Dump()
        {
            RuntimeType?[] types = new RuntimeType?[MaxTypes];
            for (int i = 0; i < count; i++)
            {
                ComponentType type = new(i + 1);
                if (type.IsValid)
                {
                    types[i] = type.RuntimeType;
                }
                else
                {
                    types[i] = null;
                }
            }

            return types;
        }

        public static void Load(RuntimeType?[] types)
        {
            if (types.Length != MaxTypes)
            {
                throw new ArgumentException($"Invalid number of types, expected {MaxTypes}.", nameof(types));
            }

            typeMap.Clear();
            count = 0;
            for (int i = 0; i < types.Length; i++)
            {
                ComponentType type = new(i + 1);
                if (types[i] is RuntimeType runtimeType)
                {
                    runtimeTypes[i] = runtimeType;
                    count++;
                    typeMap[runtimeType] = type;
                }
                else
                {
                    runtimeTypes[i] = default;
                }
            }
        }
    }
}
