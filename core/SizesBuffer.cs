using System.Runtime.CompilerServices;

namespace Worlds
{
#if NET
    [InlineArray(BitMask.Capacity)]
    internal struct SizesBuffer
    {
        private ushort element0;
    }
#else
    internal unsafe struct SizesBuffer
    {
        private fixed ushort elements[BitMask.Capacity];

        public ref ushort this[int index] => ref elements[index];
    }
#endif
}