#if !DEBUG
#define IGNORE_STACKTRACES
#endif

using Game.Unsafe;
using System;
using System.Diagnostics;

namespace Game
{
    /// <summary>
    /// The unique ID of an entity that is always 1 greater than its index,
    /// and is unique to the <see cref="World"/> that it originated from.
    /// </summary>
    public readonly struct EntityID : IEquatable<EntityID>
    {
        public readonly uint value;

        public EntityID(uint value)
        {
            this.value = value;
        }

        public override string ToString()
        {
#if !IGNORE_STACKTRACES
            if (UnsafeWorld.createStackTraces.TryGetValue(this, out StackTrace? stackTrace))
            {
                return $"{value} ({stackTrace})";
            }
            else
            {
                return value.ToString();
            }
#else
            return value.ToString();
#endif
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is EntityID iD && Equals(iD);
        }

        public readonly bool Equals(EntityID other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
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