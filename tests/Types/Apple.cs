using System;

namespace Worlds.Tests
{
    public struct Apple : IEquatable<Apple>
    {
        public byte bites;

        public Apple(byte bites)
        {
            this.bites = bites;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Apple apple && Equals(apple);
        }

        public readonly bool Equals(Apple other)
        {
            return bites == other.bites;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(bites);
        }

        public static bool operator ==(Apple left, Apple right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Apple left, Apple right)
        {
            return !(left == right);
        }
    }
}