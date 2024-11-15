using System;
using System.Diagnostics;
using System.Numerics;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Represents a 256 bit mask.
    /// </summary>
    public unsafe struct BitSet : IEquatable<BitSet>
    {
        public const byte Capacity = byte.MaxValue;

        private fixed ulong data[4];

        /// <summary>
        /// Amount of bits set to 1.
        /// </summary>
        public readonly byte Count
        {
            get
            {
                byte amount = 0;
                fixed (ulong* ptr = data)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        amount += (byte)BitOperations.PopCount(ptr[i]);
                    }
                }

                return amount;
            }
        }

        public BitSet(params byte[] values)
        {
            ThrowIfOutOfRange((uint)values.Length);

            fixed (ulong* ptr = data)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    byte index = values[i];
                    int ulongIndex = index / 64;
                    int bitPosition = index % 64;
                    ptr[ulongIndex] |= 1UL << bitPosition;
                }
            }
        }

        public BitSet(USpan<byte> values)
        {
            ThrowIfOutOfRange(values.Length);

            fixed (ulong* ptr = data)
            {
                for (uint i = 0; i < values.Length; i++)
                {
                    byte index = values[i];
                    int ulongIndex = index / 64;
                    int bitPosition = index % 64;
                    ptr[ulongIndex] |= 1UL << bitPosition;
                }
            }
        }

        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[Capacity];
            uint count = ToString(buffer);
            return buffer.ToString();
        }

        public readonly uint ToString(USpan<char> buffer)
        {
            uint count = 0;
            for (byte i = 0; i < Capacity; i++)
            {
                if (Contains(i))
                {
                    buffer[count++] = '1';
                }
                else
                {
                    buffer[count++] = '0';
                }
            }

            return count;
        }

        /// <summary>
        /// Sets the bit at position <paramref name="index"/> to 1.
        /// </summary>
        public void Set(byte index)
        {
            int ulongIndex = index / 64;
            int bitPosition = index % 64;
            fixed (ulong* ptr = data)
            {
                ptr[ulongIndex] |= 1UL << bitPosition;
            }
        }

        /// <summary>
        /// Resets all bits to 0.
        /// </summary>
        public void Clear()
        {
            fixed (ulong* ptr = data)
            {
                ptr[0] = 0;
                ptr[1] = 0;
                ptr[2] = 0;
                ptr[3] = 0;
            }
        }

        /// <summary>
        /// Sets the bit at position <paramref name="index"/> to 0.
        /// </summary>
        public void Clear(byte index)
        {
            int ulongIndex = index / 64;
            int bitPosition = index % 64;
            fixed (ulong* ptr = data)
            {
                ptr[ulongIndex] &= ~(1UL << bitPosition);
            }
        }

        /// <summary>
        /// Checks if the bit at position <paramref name="index"/> is set to 1.
        /// </summary>
        public readonly bool Contains(byte index)
        {
            int ulongIndex = index / 64;
            int bitPosition = index % 64;

            fixed (ulong* ptr = data)
            {
                return (ptr[ulongIndex] & (1UL << bitPosition)) != 0;
            }
        }

        /// <summary>
        /// Checks if this bit set contains all bits of the <paramref name="other"/> bit set.
        /// </summary>
        public readonly bool ContainsAll(BitSet other)
        {
            fixed (ulong* ptr = data)
            {
                ulong* otherPtr = other.data;
                for (int i = 0; i < 4; i++)
                {
                    if ((ptr[i] & otherPtr[i]) != otherPtr[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is BitSet set && Equals(set);
        }

        /// <inheritdoc/>
        public readonly bool Equals(BitSet other)
        {
            return data[0] == other.data[0] && data[1] == other.data[1] && data[2] == other.data[2] && data[3] == other.data[3];
        }

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfRange(uint length)
        {
            if (length >= Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"The index must be less than {Capacity}");
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(BitSet left, BitSet right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(BitSet left, BitSet right)
        {
            return !(left == right);
        }
    }
}