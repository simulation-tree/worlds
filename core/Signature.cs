using System;

namespace Worlds
{
    public unsafe struct Signature
    {
        private fixed byte bytes[10];

        public readonly uint Version
        {
            get
            {
                fixed (byte* ptr = bytes)
                {
                    return *(uint*)(ptr + 6);
                }
            }
        }

#if NET
        [Obsolete("Default constructor not supported", true)]
        public Signature()
        {
            throw new NotSupportedException();
        }
#endif

        public Signature(uint version)
        {
            fixed (byte* ptr = bytes)
            {
                ptr[0] = (byte)'#';
                ptr[1] = (byte)'W';
                ptr[2] = (byte)'O';
                ptr[3] = (byte)'R';
                ptr[4] = (byte)'L';
                ptr[5] = (byte)'D';
                *(uint*)(ptr + 6) = version;
            }
        }
    }
}