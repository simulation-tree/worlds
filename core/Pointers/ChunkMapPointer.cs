using Collections.Generic;
using Unmanaged;

namespace Worlds.Pointers
{
    internal struct ChunkMapPointer
    {
        public int count;
        public int capacity;
        public List<Chunk> chunks;
        public MemoryAddress keys;
        public MemoryAddress hashCodes;
        public MemoryAddress occupied;
        public Chunk defaultChunk;
    }
}