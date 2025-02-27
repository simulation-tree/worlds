using System;
using System.Runtime.InteropServices;
using Unmanaged;

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
        public const uint Capacity = 256;

        /// <summary>
        /// Minimum most bit position.
        /// </summary>
        public const byte MinValue = 0;

        /// <summary>
        /// Maximum most bit position.
        /// </summary>
        public const byte MaxValue = 255;

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
        public readonly byte Count
        {
            get
            {
                unchecked
                {
                    int count = 0;
                    for (uint index = 0; index < Capacity; index++)
                    {
                        if (Contains(index))
                        {
                            count++;
                        }
                    }

                    return (byte)count;
                }
            }
        }

        /// <summary>
        /// Checks if empty.
        /// </summary>
        public readonly bool IsEmpty => a == 0 && b == 0 && c == 0 && d == 0;

        public BitMask(byte b1)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            Set(b1);
        }

        public BitMask(byte b1, byte b2)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            Set(b1);
            Set(b2);
        }

        public BitMask(byte b1, byte b2, byte b3)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            Set(b1);
            Set(b2);
            Set(b3);
        }

        public BitMask(byte b1, byte b2, byte b3, byte b4)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11, byte b12)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11, byte b12, byte b13)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11, byte b12, byte b13, byte b14)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11, byte b12, byte b13, byte b14, byte b15)
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

        public BitMask(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11, byte b12, byte b13, byte b14, byte b15, byte b16)
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
            USpan<char> buffer = stackalloc char[(int)Capacity];
            uint length = ToString(buffer);
            return buffer.GetSpan(length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this bit set.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            uint length = 0;
            for (uint i = 0; i < Capacity; i++)
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
        public readonly bool Contains(byte index)
        {
            unchecked
            {
                return index switch
                {
                    < 64 => (a & 1UL << index) != 0,
                    < 128 => (b & 1UL << index - 64) != 0,
                    < 192 => (c & 1UL << index - 128) != 0,
                    _ => (d & 1UL << index - 192) != 0,
                };
            }
        }

        /// <summary>
        /// Checks if the bit at position <paramref name="index"/> is 1.
        /// </summary>
        public readonly bool Contains(uint index)
        {
            unchecked
            {
                return index switch
                {
                    < 64 => (a & 1UL << (int)index) != 0,
                    < 128 => (b & 1UL << (int)index - 64) != 0,
                    < 192 => (c & 1UL << (int)index - 128) != 0,
                    _ => (d & 1UL << (int)index - 192) != 0,
                };
            }
        }

        /// <summary>
        /// Sets the bit at position <paramref name="index"/> to 1.
        /// </summary>
        public void Set(byte index)
        {
            unchecked
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
        }

        /// <summary>
        /// Sets the bit at position <paramref name="index"/> to 1.
        /// </summary>
        public void Set(uint index)
        {
            unchecked
            {
                switch (index)
                {
                    case < 64:
                        a |= 1UL << (int)index;
                        break;
                    case < 128:
                        b |= 1UL << (int)index - 64;
                        break;
                    case < 192:
                        c |= 1UL << (int)index - 128;
                        break;
                    default:
                        d |= 1UL << (int)index - 192;
                        break;
                }
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
        public void Clear(byte index)
        {
            unchecked
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
        }

        /// <summary>
        /// Resets the bit at position <paramref name="index"/> to 0.
        /// </summary>
        public void Clear(uint index)
        {
            unchecked
            {
                switch (index)
                {
                    case < 64:
                        a &= ~(1UL << (int)index);
                        break;
                    case < 128:
                        b &= ~(1UL << (int)index - 64);
                        break;
                    case < 192:
                        c &= ~(1UL << (int)index - 128);
                        break;
                    default:
                        d &= ~(1UL << (int)index - 192);
                        break;
                }
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