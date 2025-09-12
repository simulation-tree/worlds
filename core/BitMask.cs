using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Worlds
{
    /// <summary>
    /// Represents a 256 bit mask.
    /// </summary>
    public struct BitMask : IEquatable<BitMask>
    {
        /// <summary>
        /// An empty bitmask.
        /// </summary>
        public static readonly BitMask Default = default;

        /// <summary>
        /// Maximum amount of bits that can be stored.
        /// </summary>
        public const int Capacity = 256;

        /// <summary>
        /// Minimum most bit position.
        /// </summary>
        public const int MinValue = 0;

        /// <summary>
        /// Maximum most bit position.
        /// </summary>
        public const int MaxValue = 255;

        internal Vector256<ulong> value;

        /// <summary>
        /// Amount of bits set to 1.
        /// </summary>
        public readonly int Count
        {
            get
            {
                return BitOperations.PopCount(value.GetElement(0)) +
                       BitOperations.PopCount(value.GetElement(1)) +
                       BitOperations.PopCount(value.GetElement(2)) +
                       BitOperations.PopCount(value.GetElement(3));
            }
        }

        /// <summary>
        /// Checks if empty.
        /// </summary>
        public readonly bool IsEmpty => value == Vector256<ulong>.Zero;

        internal BitMask(Vector256<ulong> value)
        {
            this.value = value;
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1)
        {
            value = default;
            Set(b1);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2)
        {
            value = default;
            Set(b1);
            Set(b2);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
            Set(b8);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
            Set(b8);
            Set(b9);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
            Set(b8);
            Set(b9);
            Set(b10);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
            Set(b8);
            Set(b9);
            Set(b10);
            Set(b11);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11, int b12)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
            Set(b8);
            Set(b9);
            Set(b10);
            Set(b11);
            Set(b12);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11, int b12, int b13)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
            Set(b8);
            Set(b9);
            Set(b10);
            Set(b11);
            Set(b12);
            Set(b13);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11, int b12, int b13, int b14)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
            Set(b8);
            Set(b9);
            Set(b10);
            Set(b11);
            Set(b12);
            Set(b13);
            Set(b14);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11, int b12, int b13, int b14, int b15)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
            Set(b8);
            Set(b9);
            Set(b10);
            Set(b11);
            Set(b12);
            Set(b13);
            Set(b14);
            Set(b15);
        }

        /// <summary>
        /// Creates a bit mask with the given positions set.
        /// </summary>
        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11, int b12, int b13, int b14, int b15, int b16)
        {
            value = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
            Set(b8);
            Set(b9);
            Set(b10);
            Set(b11);
            Set(b12);
            Set(b13);
            Set(b14);
            Set(b15);
            Set(b16);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[Capacity];
            int length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this bit set.
        /// </summary>
        public readonly int ToString(Span<char> buffer)
        {
            int length = 0;
            for (int i = 0; i < Capacity; i++)
            {
                if (Contains(i))
                {
                    length += i.ToString(buffer.Slice(length));
                    buffer[length++] = ',';
                    buffer[length++] = ' ';
                }
            }

            if (length > 0)
            {
                length -= 2;
            }

            return length;
        }

        /// <summary>
        /// Copies all set indices to the given <paramref name="destination"/> span.
        /// </summary>
        /// <returns>Amount of set bits</returns>
        public readonly int CopyTo(Span<int> destination)
        {
            int count = 0;
            for (int longIndex = 0; longIndex < 4; longIndex++)
            {
                ulong bits = value.GetElement(longIndex);
                int baseIndex = longIndex * 64;
                while (bits != 0)
                {
                    int bitIndex = BitOperations.TrailingZeroCount(bits);
                    destination[count++] = baseIndex + bitIndex;
                    bits &= bits - 1;
                }
            }

            return count;
        }

        /// <summary>
        /// Checks if this bit set contains all bits of the <paramref name="other"/> bit set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ContainsAll(BitMask other)
        {
            return (value & other.value) == other.value;
        }

        /// <summary>
        /// Checks if this bit set contains any of the bits of the <paramref name="other"/> bit set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ContainsAny(BitMask other)
        {
            return (value & other.value) != Vector256<ulong>.Zero;
        }

        /// <summary>
        /// Checks if the bit at position <paramref name="index"/> is 1.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe readonly bool Contains(int index)
        {
            fixed (Vector256<ulong>* ptr = &value)
            {
                ulong* p = (ulong*)ptr;
                int vectorIndex = index >> 6;
                int bitOffset = index & 63;
                ulong mask = 1UL << bitOffset;
                return (p[vectorIndex] & mask) != 0;
            }
        }

        /// <summary>
        /// Sets the bit at position <paramref name="index"/> to 1.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index)
        {
            int vectorIndex = index >> 6;
            int bitOffset = index & 63;
            ulong mask = 1UL << bitOffset;
            value = value.WithElement(vectorIndex, value[vectorIndex] | mask);
        }

        /// <summary>
        /// Resets the entire bit mask to <see langword="default"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            value = default;
        }

        /// <summary>
        /// Resets the bit at position <paramref name="index"/> to 0.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(int index)
        {
            int vectorIndex = index >> 6;
            int bitOffset = index & 63;
            ulong mask = 1UL << bitOffset;
            value = value.WithElement(vectorIndex, value[vectorIndex] & ~mask);
        }

        /// <summary>
        /// Retrieves a <see cref="long"/> precision hash code.
        /// </summary>
        public readonly long GetLongHashCode()
        {
            Vector128<ulong> folded = value.GetLower() ^ value.GetUpper();
            return (long)folded.GetElement(0) ^ (long)folded.GetElement(1);
        }

        /// <summary>
        /// Retrieves a cheap, but fast hash code prone to collisions.
        /// </summary>
        public readonly override int GetHashCode()
        {
            return value.GetHashCode();
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is BitMask set && Equals(set);
        }

        /// <inheritdoc/>
        public readonly bool Equals(BitMask other)
        {
            return value == other.value;
        }

        /// <inheritdoc/>
        public static bool operator ==(BitMask left, BitMask right)
        {
            return left.value == right.value;
        }

        /// <inheritdoc/>
        public static bool operator !=(BitMask left, BitMask right)
        {
            return left.value != right.value;
        }

        /// <summary>
        /// Performs a bitwise OR operation on two bit sets.
        /// </summary>
        public static BitMask operator |(BitMask left, BitMask right)
        {
            return new BitMask(left.value | right.value);
        }

        /// <summary>
        /// Performs a bitwise AND operation on two bit sets.
        /// </summary>
        public static BitMask operator &(BitMask left, BitMask right)
        {
            return new BitMask(left.value & right.value);
        }

        /// <summary>
        /// Performs a bitwise XOR operation on two bit sets.
        /// </summary>
        public static BitMask operator ^(BitMask left, BitMask right)
        {
            return new BitMask(left.value ^ right.value);
        }

        /// <summary>
        /// Inverts the bit set.
        /// </summary>
        public static BitMask operator ~(BitMask mask)
        {
            return new BitMask(~mask.value);
        }
    }
}