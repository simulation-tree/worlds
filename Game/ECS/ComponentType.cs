﻿using System;
using Unmanaged;

namespace Game.ECS
{
    /// <summary>
    /// Represents a type of a component on an entity.
    /// </summary>
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        private static readonly RuntimeType[] runtimeTypes = new RuntimeType[MaxComponentTypes];
        private static ushort count = 0;

        public const byte MaxComponentTypes = 64;

        /// <summary>
        /// The unique value of this type.
        /// </summary>
        public readonly byte value;

        public readonly RuntimeType RuntimeType => runtimeTypes[value - 1];

        private ComponentType(byte value)
        {
            this.value = value;
        }

        public ComponentType(int index)
        {
            value = (byte)(index + 1);
        }

        public override string ToString()
        {
            return RuntimeType.ToString();
        }

        public readonly bool Is<T>() where T : unmanaged
        {
            return value == ComponentTypeHash<T>.value.value;
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentType type && Equals(type);
        }

        public bool Equals(ComponentType other)
        {
            return value == other.value;
        }

        public override int GetHashCode()
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

        public unsafe static ComponentType Get<T>() where T : unmanaged
        {
            return ComponentTypeHash<T>.value;
        }

        private static class ComponentTypeHash<T> where T : unmanaged
        {
            public static ComponentType value;

            unsafe static ComponentTypeHash()
            {
                if (count >= MaxComponentTypes)
                {
                    throw new InvalidOperationException("Too many componentTypes registered.");
                }

                value = new ComponentType((byte)(count + 1));
                runtimeTypes[count] = RuntimeType.Get<T>();
                count++;
            }
        }
    }
}
