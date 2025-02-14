using System;

namespace Worlds.Tests
{
    public readonly struct Stress : IEquatable<Stress>
    {
        public readonly byte first;
        public readonly ushort second;
        public readonly uint third;
        public readonly float fourth;
        public readonly Cherry cherry;

        public readonly override bool Equals(object? obj)
        {
            return obj is Stress stress && Equals(stress);
        }

        public readonly bool Equals(Stress other)
        {
            return first == other.first && second == other.second && third == other.third && fourth == other.fourth && cherry.Equals(other.cherry);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(first, second, third, fourth, cherry);
        }

        public static bool operator ==(Stress left, Stress right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Stress left, Stress right)
        {
            return !(left == right);
        }
    }
}