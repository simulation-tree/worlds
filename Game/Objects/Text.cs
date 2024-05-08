using System;
using Unmanaged;

namespace Game
{
    public unsafe struct Text : IDisposable
    {
        private Allocation allocation;

        public uint Length
        {
            get
            {
                ReadOnlySpan<char> span = allocation.AsSpan<char>();
                return (uint)span.Length;
            }
            set
            {
                uint length = Length;
                allocation.Resize(value * sizeof(char));
                Span<char> span = allocation.AsSpan<char>();
                if (value > length)
                {
                    span.Slice((int)length).Clear();
                }
            }
        }

        public Text(ReadOnlySpan<char> value)
        {
            allocation = Allocation.Create(value);
        }

        public ReadOnlySpan<char> AsSpan()
        {
            return allocation.AsSpan<char>();
        }

        public override string ToString()
        {
            return allocation.AsSpan<char>().ToString();
        }

        public readonly void Dispose()
        {
            allocation.Dispose();
        }

        public void Append(ReadOnlySpan<char> value)
        {
            uint length = Length;
            allocation.Resize((length + (uint)value.Length) * sizeof(char));
            Span<char> span = allocation.AsSpan<char>();
            value.CopyTo(span[(int)length..]);
        }

        public void Clear()
        {
            allocation.Resize(0);
        }

        public readonly int IndexOf(char value)
        {
            ReadOnlySpan<char> span = allocation.AsSpan<char>();
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

            ReadOnlySpan<char> span = allocation.AsSpan<char>();
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
