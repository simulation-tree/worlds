using System;
using Unmanaged;

namespace Worlds.Tests
{
    public struct Cherry : IEquatable<Cherry>
    {
        public ASCIIText256 stones;

        public Cherry(ASCIIText256 stones)
        {
            this.stones = stones;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Cherry cherry && Equals(cherry);
        }

        public readonly bool Equals(Cherry other)
        {
            return stones.Equals(other.stones);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(stones);
        }

        public static bool operator ==(Cherry left, Cherry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Cherry left, Cherry right)
        {
            return !(left == right);
        }
    }
}