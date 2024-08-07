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
        private readonly uint value;

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
            return value.ToString();
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
            return left.Equals(right);
        }

        public static bool operator !=(eint left, eint right)
        {
            return !(left == right);
        }

        public static eint operator +(eint left, uint right)
        {
            return new(left.value + right);
        }

        public static eint operator +(uint left, eint right)
        {
            return new(left + right.value);
        }

        public static eint operator +(eint left, int right)
        {
            return new(left.value + (uint)right);
        }

        public static eint operator +(int left, eint right)
        {
            return new((uint)left + right.value);
        }

        public static eint operator +(eint left, eint right)
        {
            return new(left.value + right.value);
        }

        public static eint operator -(eint left, uint right)
        {
            return new(left.value - right);
        }

        public static eint operator -(uint left, eint right)
        {
            return new(left - right.value);
        }

        public static eint operator -(eint left, int right)
        {
            return new(left.value - (uint)right);
        }

        public static eint operator -(int left, eint right)
        {
            return new((uint)left - right.value);
        }

        public static eint operator -(eint left, eint right)
        {
            return new(left.value - right.value);
        }

        public static implicit operator uint(eint value)
        {
            return value.value;
        }
    }
}