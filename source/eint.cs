#pragma warning disable CS8981
using Simulation.Unsafe;
using System;
using System.Diagnostics;

namespace Simulation
{
    /// <summary>
    /// A <see cref="uint"/> value that refers to a real
    /// entity inside some <see cref="World"/>.
    /// <para>Its <c>default</c> state refers to no entity.</para>
    /// </summary>
    public readonly struct eint : IEquatable<eint>
    {
        internal readonly uint value;

#if DEBUG
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

        internal eint(uint value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            if (value == default)
            {
                return "null";
            }
            else return value.ToString();
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is eint iD && Equals(iD);
        }

        public readonly bool Equals(eint other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(value);
        }

        public readonly bool TryFormat(Span<char> buffer, out int valueLength)
        {
            return value.TryFormat(buffer, out valueLength);
        }

        public static bool operator ==(eint left, eint right)
        {
            return left.value == right.value;
        }

        public static bool operator !=(eint left, eint right)
        {
            return left.value != right.value;
        }

        public static bool operator ==(eint left, Entity right)
        {
            return left.value == ((eint)right).value;
        }

        public static bool operator !=(eint left, Entity right)
        {
            return left.value != ((eint)right).value;
        }

        public static bool operator ==(Entity left, eint right)
        {
            return ((eint)left).value == right.value;
        }

        public static bool operator !=(Entity left, eint right)
        {
            return ((eint)left).value != right.value;
        }

        public static explicit operator uint(eint value)
        {
            return value.value;
        }
    }
}