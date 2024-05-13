using System;
using System.Collections.Generic;
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

        internal EntityID(uint value)
        {
            this.value = value;
#if DEBUG
            StackTrace temp = new(3, true);
            if (temp.FrameCount > 0)
            {
                string firstFrame = temp.GetFrame(0)!.GetFileName()!;
                if (firstFrame.EndsWith("World.cs"))
                {
                    temp = new(4, true);
                }
            }

            DebugToString.createStackTraces[this] = temp;
#endif
        }

        public override string ToString()
        {
#if DEBUG
            if (DebugToString.createStackTraces.TryGetValue(this, out StackTrace? stackTrace))
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

        /// <summary>
        /// Returns a <see cref="EntityID"/> value that may or may not be valid.
        /// </summary>
        public static EntityID Assume(uint value)
        {
            return new(value);
        }

        public static bool operator ==(EntityID left, EntityID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityID left, EntityID right)
        {
            return !(left == right);
        }

#if DEBUG
        private static class DebugToString
        {
            public static readonly Dictionary<EntityID, StackTrace> createStackTraces = [];
        }
#endif
    }
}
