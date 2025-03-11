using System;
using System.Runtime.InteropServices;

namespace Worlds
{
    /// <summary>
    /// Represents a 256 bit mask.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct BitMask : IEquatable<BitMask>
    {
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

        [FieldOffset(0)]
        private ulong a;

        [FieldOffset(8)]
        private ulong b;

        [FieldOffset(16)]
        private ulong c;

        [FieldOffset(24)]
        private ulong d;

        /// <summary>
        /// Amount of bits set to 1.
        /// </summary>
        public readonly int Count
        {
            get
            {
                int count = 0;
                for (int index = 0; index < Capacity; index++)
                {
                    if (Contains(index))
                    {
                        count++;
                    }
                }

                return (int)count;
            }
        }

        /// <summary>
        /// Checks if empty.
        /// </summary>
        public readonly bool IsEmpty => a == 0 && b == 0 && c == 0 && d == 0;

        public BitMask(int b1)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            Set(b1);
        }

        public BitMask(int b1, int b2)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            Set(b1);
            Set(b2);
        }

        public BitMask(int b1, int b2, int b3)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            Set(b1);
            Set(b2);
            Set(b3);
        }

        public BitMask(int b1, int b2, int b3, int b4)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
        }

        public BitMask(int b1, int b2, int b3, int b4, int b5)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
        }

        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
        }

        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
        }

        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            Set(b1);
            Set(b2);
            Set(b3);
            Set(b4);
            Set(b5);
            Set(b6);
            Set(b7);
            Set(b8);
        }

        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9)
        {
            a = default;
            b = default;
            c = default;
            d = default;
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

        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10)
        {
            a = default;
            b = default;
            c = default;
            d = default;
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

        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11)
        {
            a = default;
            b = default;
            c = default;
            d = default;
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

        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11, int b12)
        {
            a = default;
            b = default;
            c = default;
            d = default;
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

        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11, int b12, int b13)
        {
            a = default;
            b = default;
            c = default;
            d = default;
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

        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11, int b12, int b13, int b14)
        {
            a = default;
            b = default;
            c = default;
            d = default;
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

        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11, int b12, int b13, int b14, int b15)
        {
            a = default;
            b = default;
            c = default;
            d = default;
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

        public BitMask(int b1, int b2, int b3, int b4, int b5, int b6, int b7, int b8, int b9, int b10, int b11, int b12, int b13, int b14, int b15, int b16)
        {
            a = default;
            b = default;
            c = default;
            d = default;
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
        /// Checks if this bit set contains all bits of the <paramref name="other"/> bit set.
        /// </summary>
        public readonly bool ContainsAll(BitMask other)
        {
            return (a & other.a) == other.a && (b & other.b) == other.b && (c & other.c) == other.c && (d & other.d) == other.d;
        }

        /// <summary>
        /// Checks if this bit set contains any of the bits of the <paramref name="other"/> bit set.
        /// </summary>
        public readonly bool ContainsAny(BitMask other)
        {
            return (a & other.a) != 0 || (b & other.b) != 0 || (c & other.c) != 0 || (d & other.d) != 0;
        }

        /// <summary>
        /// Checks if the bit at position <paramref name="index"/> is 1.
        /// </summary>
        public readonly bool Contains(int index)
        {
            return index switch
            {
                < 64 => (a & 1UL << index) != 0,
                < 128 => (b & 1UL << index - 64) != 0,
                < 192 => (c & 1UL << index - 128) != 0,
                _ => (d & 1UL << index - 192) != 0,
            };
        }

        /// <summary>
        /// Sets the bit at position <paramref name="index"/> to 1.
        /// </summary>
        public void Set(int index)
        {
            switch (index)
            {
                case < 64:
                    a |= 1UL << index;
                    break;
                case < 128:
                    b |= 1UL << index - 64;
                    break;
                case < 192:
                    c |= 1UL << index - 128;
                    break;
                default:
                    d |= 1UL << index - 192;
                    break;
            }
        }

        /// <summary>
        /// Resets the entire bit mask to <see langword="default"/>.
        /// </summary>
        public void Clear()
        {
            a = default;
            b = default;
            c = default;
            d = default;
        }

        /// <summary>
        /// Resets the bit at position <paramref name="index"/> to 0.
        /// </summary>
        public void Clear(int index)
        {
            switch (index)
            {
                case < 64:
                    a &= ~(1UL << index);
                    break;
                case < 128:
                    b &= ~(1UL << index - 64);
                    break;
                case < 192:
                    c &= ~(1UL << index - 128);
                    break;
                default:
                    d &= ~(1UL << index - 192);
                    break;
            }
        }

        /// <summary>
        /// Retrieves a cheap hashcode prone to collisions.
        /// </summary>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                ulong hash = a ^ b ^ c ^ d;
                return (int)hash ^ (int)(hash >> 32);
            }
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is BitMask set && Equals(set);
        }

        /// <inheritdoc/>
        public readonly bool Equals(BitMask other)
        {
            return a == other.a && b == other.b && c == other.c && d == other.d;
        }

        public static bool operator ==(BitMask left, BitMask right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BitMask left, BitMask right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Performs a bitwise OR operation on two bit sets.
        /// </summary>
        public static BitMask operator |(BitMask left, BitMask right)
        {
            BitMask result = left;
            result.a |= right.a;
            result.b |= right.b;
            result.c |= right.c;
            result.d |= right.d;
            return result;
        }

        /// <summary>
        /// Performs a bitwise AND operation on two bit sets.
        /// </summary>
        public static BitMask operator &(BitMask left, BitMask right)
        {
            BitMask result = left;
            result.a &= right.a;
            result.b &= right.b;
            result.c &= right.c;
            result.d &= right.d;
            return result;
        }

        /// <summary>
        /// Performs a bitwise XOR operation on two bit sets.
        /// </summary>
        public static BitMask operator ^(BitMask left, BitMask right)
        {
            BitMask result = left;
            result.a ^= right.a;
            result.b ^= right.b;
            result.c ^= right.c;
            result.d ^= right.d;
            return result;
        }

        /// <summary>
        /// Inverts the bit set.
        /// </summary>
        public static BitMask operator ~(BitMask mask)
        {
            BitMask result = mask;
            result.a = ~result.a;
            result.b = ~result.b;
            result.c = ~result.c;
            result.d = ~result.d;
            return result;
        }
    }
}