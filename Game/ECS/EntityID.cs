using System;

namespace Game
{
    /// <summary>
    /// The unique ID of an entity that is always 1 greater than its index,
    /// and is unique to the <see cref="World"/> that it originated from.
    /// </summary>
    public readonly struct EntityID : IEquatable<EntityID>
    {
        public readonly uint value;

        internal EntityID(uint value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public override bool Equals(object? obj)
        {
            return obj is EntityID iD && Equals(iD);
        }

        public bool Equals(EntityID other)
        {
            return value == other.value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(value);
        }

        public static bool operator ==(EntityID left, EntityID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityID left, EntityID right)
        {
            return !(left == right);
        }
    }
}
