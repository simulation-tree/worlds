using Collections.Generic;
using Unmanaged;

namespace Worlds.Pointers
{
    internal struct ChunkMap
    {
        public readonly Worlds.Schema schema;
        public int count;
        public int capacity;
        public List<Worlds.Chunk> chunks;
        public MemoryAddress keys;
        public MemoryAddress hashCodes;
        public MemoryAddress occupied;
        public Worlds.Chunk defaultChunk;

        public unsafe ChunkMap(Worlds.Schema schema)
        {
            this.schema = schema;
            count = 0;
            capacity = 32;
            chunks = new(capacity);
            keys = MemoryAddress.Allocate(capacity * sizeof(ChunkKey));
            hashCodes = MemoryAddress.Allocate(capacity * sizeof(ulong));
            occupied = MemoryAddress.AllocateZeroed(capacity);
        }
    }
}