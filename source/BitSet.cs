using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
#if NET
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
        public const byte Capacity = byte.MaxValue;

#if NET
        [FieldOffset(0)]
        private Vector256<ulong> data;
#endif

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
#if NET
                    count += BitOperations.PopCount(a);
                    count += BitOperations.PopCount(b);
                    count += BitOperations.PopCount(c);
                    count += BitOperations.PopCount(d);
#else
                    for (int index = 0; index < Capacity; index++)
                    {
                        if (Contains((byte)index))
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
        public BitSet(params byte[] positions)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            ThrowIfOutOfRange((uint)positions.Length);
            for (uint i = 0; i < positions.Length; i++)
            {
                byte index = positions[i];
                Set(index);
            }
        }

        /// <summary>
        /// Creates a bit set with 1s set at positions in <paramref name="positions"/>.
        /// </summary>
        public BitSet(USpan<byte> positions)
        {
            a = default;
            b = default;
            c = default;
            d = default;
            ThrowIfOutOfRange(positions.Length);
            for (uint i = 0; i < positions.Length; i++)
            {
                byte index = positions[i];
                Set(index);
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
#if NET
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
        }

        /// <summary>
        /// Resets all bits to 0.
        /// </summary>
        public void Clear()
        {
            a = 0;
            b = 0;
            c = 0;
            d = 0;
        }

        /// <summary>
        /// Sets the bit at position <paramref name="index"/> to 0.
        /// </summary>
        public void Clear(byte index)
        {
#if NET
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
        }

        /// <summary>
        /// Checks if the bit at position <paramref name="index"/> is set to 1.
        /// </summary>
        public readonly bool Contains(byte index)
        {
#if NET
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
        /// Checks if this bit set contains all bits of the <paramref name="other"/> bit set.
        /// </summary>
        public readonly bool ContainsAll(BitSet other)
        {
#if NET
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
#if NET
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
            unchecked
            {
                ulong hash = a ^ b ^ c ^ d;
                return (int)hash ^ (int)(hash >> 32);
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
#if NET
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