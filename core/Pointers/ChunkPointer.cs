using Collections;
using Collections.Generic;

namespace Worlds.Pointers
{
    internal struct ChunkPointer
    {
        public const int FirstEntity = 1;

        public BitMask componentTypes;
        public BitMask arrayTypes;
        public BitMask tagTypes;
        public List<uint> entities;
        public List components;
        public int count;
        public uint lastEntity;
        public int version;
        public Schema schema;
    }
}