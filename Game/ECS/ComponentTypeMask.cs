using System;
using System.Text;

namespace Game
{
    /// <summary>
    /// A mask containing component types.
    /// </summary>
    public unsafe struct ComponentTypeMask : IEquatable<ComponentTypeMask>
    {
        public const int MaxValues = ComponentType.MaxTypes;

        private ulong value;

        /// <summary>
        /// Size in bytes encapsulating all component types in this mask.
        /// </summary>
        public readonly uint Size
        {
            get
            {
                uint size = 0;
                for (int i = 0; i < MaxValues; i++)
                {
                    ComponentType type = new(i + 1);
                    if (Contains(type))
                    {
                        size += type.RuntimeType.size;
                    }
                }

                return size;
            }
        }

        /// <summary>
        /// Amount of types in this mask.
        /// </summary>
        public readonly uint Count
        {
            get
            {
                uint count = 0;
                for (int i = 0; i < MaxValues; i++)
                {
                    ComponentType type = new(i + 1);
                    if (Contains(type))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public override readonly string ToString()
        {
            StringBuilder builder = new();
            for (int i = 0; i < MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                if (Contains(type))
                {
                    builder.Append(type.RuntimeType.Type.Name);
                    builder.Append(", ");
                }
            }

            if (builder.Length > 0)
            {
                builder.Length -= 2;
            }

            return builder.ToString();
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is ComponentTypeMask other && Equals(other);
        }

        public readonly bool Equals(ComponentTypeMask other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public readonly int CopyTo(Span<ComponentType> span)
        {
            int count = 0;
            for (int i = 0; i < MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                if (Contains(type))
                {
                    span[count++] = type;
                }
            }

            return count;
        }

        public void Add(ComponentType type)
        {
            value |= 1UL << type.value;
        }

        public void Add<T>() where T : unmanaged
        {
            Add(ComponentType.Get<T>());
        }

        public void Remove(ComponentType type)
        {
            value &= ~(1UL << type.value);
        }

        public void Remove<T>() where T : unmanaged
        {
            Remove(ComponentType.Get<T>());
        }

        public readonly bool Contains(ComponentType type)
        {
            return (value & 1UL << type.value) != 0;
        }

        /// <returns><c>true</c> if the given value is contained.</returns>
        public readonly bool Contains(ComponentTypeMask other)
        {
            return (value & other.value) == other.value;
        }

        public readonly bool Contains<T>() where T : unmanaged
        {
            return Contains(ComponentType.Get<T>());
        }

        public static bool operator ==(ComponentTypeMask left, ComponentTypeMask right)
        {
            return left.value == right.value;
        }

        public static bool operator !=(ComponentTypeMask left, ComponentTypeMask right)
        {
            return left.value != right.value;
        }

        public static ComponentTypeMask Get(ComponentType type1)
        {
            ComponentTypeMask mask = default;
            mask.Add(type1);
            return mask;
        }

        public static ComponentTypeMask Get(ComponentType type1, ComponentType type2)
        {
            ComponentTypeMask mask = default;
            mask.Add(type1);
            mask.Add(type2);
            return mask;
        }

        public static ComponentTypeMask Get<T1>() where T1 : unmanaged
        {
            ComponentTypeMask mask = default;
            mask.Add(ComponentType.Get<T1>());
            return mask;
        }

        public static ComponentTypeMask Get<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            ComponentTypeMask mask = default;
            mask.Add(ComponentType.Get<T1>());
            mask.Add(ComponentType.Get<T2>());
            return mask;
        }

        public static ComponentTypeMask Get<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            ComponentTypeMask mask = default;
            mask.Add(ComponentType.Get<T1>());
            mask.Add(ComponentType.Get<T2>());
            mask.Add(ComponentType.Get<T3>());
            return mask;
        }

        public static ComponentTypeMask Get<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            ComponentTypeMask mask = default;
            mask.Add(ComponentType.Get<T1>());
            mask.Add(ComponentType.Get<T2>());
            mask.Add(ComponentType.Get<T3>());
            mask.Add(ComponentType.Get<T4>());
            return mask;
        }

        public static ComponentTypeMask Get<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            ComponentTypeMask mask = default;
            mask.Add(ComponentType.Get<T1>());
            mask.Add(ComponentType.Get<T2>());
            mask.Add(ComponentType.Get<T3>());
            mask.Add(ComponentType.Get<T4>());
            mask.Add(ComponentType.Get<T5>());
            return mask;
        }

        public static ComponentTypeMask Get<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            ComponentTypeMask mask = default;
            mask.Add(ComponentType.Get<T1>());
            mask.Add(ComponentType.Get<T2>());
            mask.Add(ComponentType.Get<T3>());
            mask.Add(ComponentType.Get<T4>());
            mask.Add(ComponentType.Get<T5>());
            mask.Add(ComponentType.Get<T6>());
            return mask;
        }
    }
}
