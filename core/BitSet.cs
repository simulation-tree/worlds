using System;

namespace Simulation
{
    public unsafe struct BitSet : IEquatable<BitSet>
    {
        public const byte Capacity = byte.MaxValue;

        private fixed ulong data[4];

        public void Set(byte index)
        {
            int ulongIndex = index / 64;
            int bitPosition = index % 64;
            fixed (ulong* ptr = data)
            {
                ptr[ulongIndex] |= (1UL << bitPosition);
            }
        }

        public void Clear(byte index)
        {
            int ulongIndex = index / 64;
            int bitPosition = index % 64;
            fixed (ulong* ptr = data)
            {
                ptr[ulongIndex] &= ~(1UL << bitPosition);
            }
        }

        public readonly bool Has(byte index)
        {
            int ulongIndex = index / 64;
            int bitPosition = index % 64;

            fixed (ulong* ptr = data)
            {
                return (ptr[ulongIndex] & (1UL << bitPosition)) != 0;
            }
        }

        public readonly override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)((data[0] >> 32) ^ data[0]);
                hash = (hash * 397) ^ (int)((data[1] >> 32) ^ data[1]);
                hash = (hash * 397) ^ (int)((data[2] >> 32) ^ data[2]);
                hash = (hash * 397) ^ (int)((data[3] >> 32) ^ data[3]);
                return hash;
            }
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is BitSet set && Equals(set);
        }

        public readonly bool Equals(BitSet other)
        {
            return data[0] == other.data[0] && data[1] == other.data[1] && data[2] == other.data[2] && data[3] == other.data[3];
        }

        public static bool operator ==(BitSet left, BitSet right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BitSet left, BitSet right)
        {
            return !(left == right);
        }
    }
}