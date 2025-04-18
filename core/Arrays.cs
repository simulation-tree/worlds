using System.Runtime.CompilerServices;

namespace Worlds
{
    internal struct Arrays
    {
        private Buffer buffer;

        public Values this[int index]
        {
            readonly get => buffer[index];
            set => buffer[index] = value;
        }

#if NET
        [InlineArray(BitMask.Capacity)]
        private struct Buffer
        {
            private Values element0;
        }
#else
        private unsafe struct Buffer
        {
            private fixed ulong buffer[BitMask.Capacity];

            public Values this[int index]
            {
                readonly get => new((nint)buffer[index]);
                set => buffer[index] = (ulong)(nint)value.pointer;
            }
        }
#endif
    }
}