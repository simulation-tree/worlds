using Collections;
using Collections.Generic;

namespace Worlds.Pointers
{
    internal struct ChunkPointer
    {
        public uint lastEntity;
        public int count;
        public Definition definition;
        public Schema schema;
        public List<uint> entities;
        public List components;
    }
}