using Collections;
using Collections.Generic;

namespace Worlds.Pointers
{
    internal struct ChunkPointer
    {
        public const int FirstEntity = 1;

        public uint lastEntity;
        public int count;
        public int version;
        public BitMask componentTypes;
        public BitMask arrayTypes;
        public BitMask tagTypes;
        public Schema schema;
        public List<uint> entities;
        public List components;

        public readonly Definition Definition => new(componentTypes, arrayTypes, tagTypes);
    }
}