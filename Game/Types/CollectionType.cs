using System;
using Unmanaged;

namespace Game
{
    /// <summary>
    /// Represents a collection type using the element type.
    /// </summary>
    public readonly struct CollectionType : IEquatable<CollectionType>
    {
        private static readonly RuntimeType[] runtimeTypes = new RuntimeType[MaxTypes];
        private static ushort count = 0;

        /// <summary>
        /// Maximum amount of collection types permitted in use.
        /// </summary>
        public const byte MaxTypes = 16;

        public readonly byte value;

        public readonly RuntimeType RuntimeType => runtimeTypes[value - 1];

        private CollectionType(byte value)
        {
            this.value = value;
        }

        public CollectionType(int index)
        {
            value = (byte)(index + 1);
        }

        public override string ToString()
        {
            string typeName = RuntimeType.Type.Name;
            Span<char> temp = stackalloc char[256];
            typeName.CopyTo(temp);
            int length = typeName.Length;
            temp[length] = '[';
            temp[length + 1] = ']';
            return new string(temp[..(length + 2)]);
        }

        public override bool Equals(object? obj)
        {
            return obj is CollectionType type && Equals(type);
        }

        public bool Equals(CollectionType other)
        {
            return value == other.value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static bool operator ==(CollectionType left, CollectionType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CollectionType left, CollectionType right)
        {
            return !(left == right);
        }

        public unsafe static CollectionType Get<T>() where T : unmanaged
        {
            return HashValue<T>.value;
        }

        private static class HashValue<T> where T : unmanaged
        {
            public static CollectionType value;

            static HashValue()
            {
                if (count >= MaxTypes)
                {
                    throw new InvalidOperationException("Too many collection types registered.");
                }

                value = new CollectionType((byte)(count + 1));
                runtimeTypes[count] = RuntimeType.Get<T>();
                count++;
            }
        }
    }
}
