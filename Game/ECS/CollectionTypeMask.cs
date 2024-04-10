using System;
using System.Text;

namespace Game.ECS
{
    public unsafe struct CollectionTypeMask : IEquatable<CollectionTypeMask>
    {
        public ulong value;

        public readonly int Count
        {
            get
            {
                int count = 0;
                for (int i = 0; i < CollectionType.MaxTypes; i++)
                {
                    CollectionType type = new(i);
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
            for (int i = 0; i < CollectionType.MaxTypes; i++)
            {
                CollectionType type = new(i);
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
            return obj is CollectionTypeMask other && Equals(other);
        }

        public readonly bool Equals(CollectionTypeMask other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public readonly int CopyTo(Span<CollectionType> span)
        {
            int count = 0;
            for (int i = 0; i < CollectionType.MaxTypes; i++)
            {
                CollectionType type = new(i);
                if (Contains(type))
                {
                    span[count++] = type;
                }
            }

            return count;
        }

        public void Add(CollectionType type)
        {
            value |= 1UL << type.value;
        }

        public void Add<T>() where T : unmanaged
        {
            Add(CollectionType.Get<T>());
        }

        public void Remove(CollectionType type)
        {
            value &= ~(1UL << type.value);
        }

        public void Remove<T>() where T : unmanaged
        {
            Remove(CollectionType.Get<T>());
        }

        public readonly bool Contains(CollectionType type)
        {
            return (value & 1UL << type.value) != 0;
        }

        public readonly bool Contains<T>() where T : unmanaged
        {
            return Contains(CollectionType.Get<T>());
        }

        public static bool operator ==(CollectionTypeMask left, CollectionTypeMask right)
        {
            return left.value == right.value;
        }

        public static bool operator !=(CollectionTypeMask left, CollectionTypeMask right)
        {
            return left.value != right.value;
        }
    }
}
