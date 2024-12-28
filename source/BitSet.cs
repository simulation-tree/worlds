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
    public unsafe struct BitSet : IEquatable<BitSet>
    {
        /// <summary>
        /// Maximum amount of bits in the bit set.
        /// </summary>
        public const byte Capacity = 0xFF;

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
                        if (this == index)
                        {
                            count++;
                        }
                    }
#endif
                    return (byte)count;
                }
            }
        }

        /// <summary>
        /// Creates a bit set with 1s set at positions in <paramref name="positions"/>.
        /// </summary>
        public BitSet(params USpan<byte> positions)
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
                this |= index;
            }
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[Capacity];
            uint count = ToString(buffer);
            return buffer.ToString();
        }

        /// <summary>
        /// Builds a string representation of this bit set.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            uint count = 0;
            for (byte i = 0; i < Capacity; i++)
            {
                if (this == i)
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
        /// Checks if this bit set contains all bits of the <paramref name="other"/> bit set.
        /// </summary>
        public readonly bool ContainsAll(BitSet other)
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
        public readonly bool ContainsAny(BitSet other)
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
            return obj is BitSet set && Equals(set);
        }

        /// <inheritdoc/>
        public readonly bool Equals(BitSet other)
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

        public static bool operator ==(BitSet left, BitSet right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BitSet left, BitSet right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Performs a bitwise OR operation on two bit sets.
        /// </summary>
        public static BitSet operator |(BitSet left, BitSet right)
        {
            BitSet result = left;
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
        public static BitSet operator &(BitSet left, BitSet right)
        {
            BitSet result = left;
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
        public static BitSet operator ^(BitSet left, BitSet right)
        {
            BitSet result = left;
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
        public static BitSet operator ~(BitSet mask)
        {
            BitSet result = mask;
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

        /// <summary>
        /// Assigns the bit at position <paramref name="index"/> to 1.
        /// </summary>
        public static BitSet operator |(BitSet mask, byte index)
        {
#if USE_VECTOR256
            int longIndex = index / 64;
            int bitIndex = index % 64;
            ulong slot = mask.data.GetElement(longIndex);
            slot |= 1UL << bitIndex;
            mask.data = mask.data.WithElement(longIndex, slot);
#else
            switch (index)
            {
                case < 64:
                    left.a |= 1UL << index;
                    break;
                case < 128:
                    left.b |= 1UL << index - 64;
                    break;
                case < 192:
                    left.c |= 1UL << index - 128;
                    break;
                default:
                    left.d |= 1UL << index - 192;
                    break;
            }
#endif
            return mask;
        }

        /// <summary>
        /// Clears the bit at position <paramref name="index"/>.
        /// </summary>
        public static BitSet operator &(BitSet mask, byte index)
        {
#if USE_VECTOR256
            int longIndex = index / 64;
            int bitIndex = index % 64;
            ulong slot = mask.data.GetElement(longIndex);
            slot &= ~(1UL << bitIndex);
            mask.data = mask.data.WithElement(longIndex, slot);
#else
            switch (index)
            {
                case < 64:
                    left.a &= ~(1UL << index);
                    break;
                case < 128:
                    left.b &= ~(1UL << index - 64);
                    break;
                case < 192:
                    left.c &= ~(1UL << index - 128);
                    break;
                default:
                    left.d &= ~(1UL << index - 192);
                    break;
            }
#endif
            return mask;
        }

        /// <summary>
        /// Performs a bitwise XOR operation on the bit at position <paramref name="index"/>.
        /// </summary>
        public static bool operator ^(BitSet mask, byte index)
        {
#if USE_VECTOR256
            int longIndex = index / 64;
            int bitIndex = index % 64;
            ulong slot = mask.data.GetElement(longIndex);
            slot ^= 1UL << bitIndex;
            mask.data = mask.data.WithElement(longIndex, slot);
            return (slot & 1UL << bitIndex) != 0;
#else
            switch (index)
            {
                case < 64:
                    left.a ^= 1UL << index;
                    return (left.a & 1UL << index) != 0;
                case < 128:
                    left.b ^= 1UL << index - 64;
                    return (left.b & 1UL << index - 64) != 0;
                case < 192:
                    left.c ^= 1UL << index - 128;
                    return (left.c & 1UL << index - 128) != 0;
                default:
                    left.d ^= 1UL << index - 192;
                    return (left.d & 1UL << index - 192) != 0;
            }
#endif
        }

        /// <summary>
        /// Checks if the bit set contains the bit at position <paramref name="index"/>.
        /// </summary>
        public static bool operator ==(BitSet mask, byte index)
        {
#if USE_VECTOR256
            int longIndex = index / 64;
            int bitIndex = index % 64;
            return (mask.data.GetElement(longIndex) & 1UL << bitIndex) != 0;
#else
            return index switch
            {
                < 64 => (left.a & 1UL << index) != 0,
                < 128 => (left.b & 1UL << index - 64) != 0,
                < 192 => (left.c & 1UL << index - 128) != 0,
                _ => (left.d & 1UL << index - 192) != 0,
            };
#endif
        }

        /// <summary>
        /// Checks if the bit at position <paramref name="index"/> is not set.
        /// </summary>
        public static bool operator !=(BitSet mask, byte index)
        {
            return !(mask == index);
        }
    }
}