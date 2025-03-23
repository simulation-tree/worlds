using Collections.Generic;
using Unmanaged;

namespace Worlds.Pointers
{
    internal struct ChunkMapPointer
    {
        public readonly Schema schema;
        public int count;
        public int capacity;
        public List<Chunk> chunks;
        public MemoryAddress keys;
        public MemoryAddress hashCodes;
        public MemoryAddress occupied;
        public Chunk defaultChunk;

        public unsafe ChunkMapPointer(Schema schema)
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