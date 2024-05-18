using System;
using Unmanaged;

namespace Game
{
    public unsafe struct Text : IDisposable
    {
        private Allocation allocation;
        private uint length;

        public uint Length
        {
            get
            {
                return length;
            }
            set
            {
                allocation.Resize(value * sizeof(char));
                Span<char> span = allocation.AsSpan<char>(0, value);
                if (value > length)
                {
                    span.Slice((int)length).Clear();
                }

                length = value;
            }
        }

        public Text(ReadOnlySpan<char> value)
        {
            allocation = Allocation.Create(value);
            length = (uint)value.Length;
        }

        public readonly ReadOnlySpan<char> AsSpan()
        {
            return allocation.AsSpan<char>(0, length);
        }

        public readonly override string ToString()
        {
            return AsSpan().ToString();
        }

        public readonly void Dispose()
        {
            allocation.Dispose();
        }

        public void Append(ReadOnlySpan<char> value)
        {
            uint previousLength = Length;
            allocation.Resize((previousLength + (uint)value.Length) * sizeof(char));
            length = (uint)(previousLength + value.Length);
            Span<char> span = allocation.AsSpan<char>(0, length);
            value.CopyTo(span[(int)previousLength..]);
        }

        public void Clear()
        {
            allocation.Resize(0);
        }

        public readonly int IndexOf(char value)
        {
            ReadOnlySpan<char> span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == value)
                {
                    return i;
                }
            }

            return -1;
        }

        public readonly bool Contains(char value)
        {
            return IndexOf(value) != -1;
        }

        public readonly int IndexOf(ReadOnlySpan<char> value)
        {
            if (value.Length == 0)
            {
                return -1;
            }

            ReadOnlySpan<char> span = AsSpan();
            for (int i = 0; i <= span.Length - value.Length; i++)
            {
                if (span.Slice(i).StartsWith(value))
                {
                    return i;
                }
            }

            return -1;
        }

        public readonly bool Contains(ReadOnlySpan<char> value)
        {
            return IndexOf(value) != -1;
        }

        public static implicit operator ReadOnlySpan<char>(Text text)
        {
            return text.AsSpan();
        }
    }
}
