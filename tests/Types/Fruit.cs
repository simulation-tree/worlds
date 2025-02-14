using System;

namespace Worlds.Tests
{
    public readonly struct Fruit : IEquatable<Fruit>
    {
        public readonly int data;

        public Fruit(int data)
        {
            this.data = data;
        }

        public override bool Equals(object? obj)
        {
            return obj is Fruit fruit && Equals(fruit);
        }

        public bool Equals(Fruit other)
        {
            return data == other.data;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(data);
        }

        public static bool operator ==(Fruit left, Fruit right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Fruit left, Fruit right)
        {
            return !(left == right);
        }
    }
}