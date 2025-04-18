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

        [InlineArray(BitMask.Capacity)]
        private struct Buffer
        {
            private Values element0;
        }
    }
}