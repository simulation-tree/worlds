#if !DEBUG
#define IGNORE_STACKTRACES
#endif

using Simulation.Unsafe;
using System;
using System.Diagnostics;

namespace Simulation
{
    /// <summary>
    /// The unique ID of an entity that is always 1 greater than its index,
    /// and is unique to the <see cref="World"/> that it originated from.
    /// </summary>
    public readonly struct EntityID(uint value) : IEquatable<EntityID>
    {
        public readonly uint value = value;

#if !IGNORE_STACKTRACES
        public StackTrace? Creation
        {
            get
            {
                if (UnsafeWorld.createStackTraces.TryGetValue(this, out StackTrace? stackTrace))
                {
                    return stackTrace;
                }
                else
                {
                    return null;
                }
            }
        }
#endif

        public override string ToString()
        {
            return value.ToString();
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