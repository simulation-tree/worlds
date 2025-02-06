#if NET
#define USE_VECTOR256
#endif

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
#if USE_VECTOR256
using System.Runtime.Intrinsics;
#endif
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Represents a 256 bit mask.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct BitMask : IEquatable<BitMask>
    {
        /// <summary>
        /// Maximum amount of bits in the bit set.
        /// </summary>
        public const byte Capacity = 255;

#if USE_VECTOR256
        [FieldOffset(0)]
        private Vector256<ulong> data;
#else

        [FieldOffset(0)]
        private ulong a;

        [FieldOffset(8)]
        private ulong b;

        [FieldOffset(16)]
        private ulong c;

        [FieldOffset(24)]
        private ulong d;
#endif

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
#if USE_VECTOR256
                    count += BitOperations.PopCount(data.GetElement(0));
                    count += BitOperations.PopCount(data.GetElement(1));
                    count += BitOperations.PopCount(data.GetElement(2));
                    count += BitOperations.PopCount(data.GetElement(3));
#else
                    for (byte index = 0; index < Capacity; index++)
                    {
                        if (Contains(index))
                        {
                            count++;
                        }
                    }
#endif
                    return (byte)count;
                }
            }
        }

#if NET
        /// <summary>
        /// Creates a bit set with 1s set at positions in <paramref name="positions"/>.
        /// </summary>
        public BitMask(params USpan<byte> positions)
        {
#if !USE_VECTOR256
            a = default;
            b = default;
            c = default;
            d = default;
#endif

            ThrowIfOutOfRange(positions.Length);
            for (uint i = 0; i < positions.Length; i++)
            {
                byte index = positions[i];
                Set(index);
            }
        }
#else
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
#endif

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[Capacity];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this bit set.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            uint length = 0;
            for (byte i = 0; i < Capacity; i++)
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
#if USE_VECTOR256
            return (data & other.data) == other.data;
#else
            return (a & other.a) == other.a &&
                   (b & other.b) == other.b &&
                   (c & other.c) == other.c &&
                   (d & other.d) == other.d;
#endif
        }

        /// <summary>
        /// Checks if this bit set contains any of the bits of the <paramref name="other"/> bit set.
        /// </summary>
        public readonly bool ContainsAny(BitMask other)
        {
#if USE_VECTOR256
            return !(data & other.data).Equals(default);
#else
            return (a & other.a) != 0 ||
                   (b & other.b) != 0 ||
                   (c & other.c) != 0 ||
                   (d & other.d) != 0;
#endif
        }

        /// <summary>
        /// Checks if the bit at position <paramref name="index"/> is 1.
        /// </summary>
        public readonly bool Contains(byte index)
        {
#if USE_VECTOR256
            int longIndex = index / 64;
            int bitIndex = index % 64;
            return (data.GetElement(longIndex) & 1UL << bitIndex) != 0;
#else
            return index switch
            {
                < 64 => (a & 1UL << index) != 0,
                < 128 => (b & 1UL << index - 64) != 0,
                < 192 => (c & 1UL << index - 128) != 0,
                _ => (d & 1UL << index - 192) != 0,
            };
#endif
        }

        /// <summary>
        /// Sets the bit at position <paramref name="index"/> to 1.
        /// </summary>
        public BitMask Set(byte index)
        {
#if USE_VECTOR256
            int longIndex = index / 64;
            int bitIndex = index % 64;
            ulong slot = data.GetElement(longIndex);
            slot |= 1UL << bitIndex;
            data = data.WithElement(longIndex, slot);
#else
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
#endif
            return this;
        }

        /// <summary>
        /// Resets the bit at position <paramref name="index"/> to 0.
        /// </summary>
        public BitMask Clear(byte index)
        {
#if USE_VECTOR256
            int longIndex = index / 64;
            int bitIndex = index % 64;
            ulong slot = data.GetElement(longIndex);
            slot &= ~(1UL << bitIndex);
            data = data.WithElement(longIndex, slot);
#else
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
#endif
            return this;
        }

        /// <summary>
        /// Retrieves a cheap hashcode prone to collisions.
        /// </summary>
        public readonly override int GetHashCode()
        {
#if USE_VECTOR256
            return data.GetHashCode();
#else
            unchecked
            {
                ulong hash = a ^ b ^ c ^ d;
                return (int)hash ^ (int)(hash >> 32);
            }
#endif
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is BitMask set && Equals(set);
        }

        /// <inheritdoc/>
        public readonly bool Equals(BitMask other)
        {
#if USE_VECTOR256
            return data == other.data;
#else
            return a == other.a && b == other.b && c == other.c && d == other.d;
#endif
        }

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfRange(uint length)
        {
            if (length >= Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"The index must be less than {Capacity}");
            }
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
#if USE_VECTOR256
            result.data |= right.data;
#else
            result.a |= right.a;
            result.b |= right.b;
            result.c |= right.c;
            result.d |= right.d;
#endif
            return result;
        }

        /// <summary>
        /// Performs a bitwise AND operation on two bit sets.
        /// </summary>
        public static BitMask operator &(BitMask left, BitMask right)
        {
            BitMask result = left;
#if USE_VECTOR256
            result.data &= right.data;
#else
            result.a &= right.a;
            result.b &= right.b;
            result.c &= right.c;
            result.d &= right.d;
#endif
            return result;
        }

        /// <summary>
        /// Performs a bitwise XOR operation on two bit sets.
        /// </summary>
        public static BitMask operator ^(BitMask left, BitMask right)
        {
            BitMask result = left;
#if USE_VECTOR256
            result.data ^= right.data;
#else
            result.a ^= right.a;
            result.b ^= right.b;
            result.c ^= right.c;
            result.d ^= right.d;
#endif
            return result;
        }

        /// <summary>
        /// Inverts the bit set.
        /// </summary>
        public static BitMask operator ~(BitMask mask)
        {
            BitMask result = mask;
#if USE_VECTOR256
            result.data = ~result.data;
#else
            result.a = ~result.a;
            result.b = ~result.b;
            result.c = ~result.c;
            result.d = ~result.d;
#endif
            return result;
        }
    }
}