using Collections.Generic;
using Unmanaged;

namespace Worlds.Pointers
{
    internal struct ChunkMap
    {
        internal readonly Worlds.Schema schema;
        internal int count;
        internal int capacity;
        internal List<Worlds.Chunk> chunks;
        internal MemoryAddress keys;
        internal MemoryAddress hashCodes;
        internal MemoryAddress occupied;

        internal unsafe ChunkMap(Worlds.Schema schema)
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