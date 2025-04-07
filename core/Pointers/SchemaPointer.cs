using Unmanaged;

namespace Worlds.Pointers
{
    internal struct SchemaPointer
    {
        public int schemaIndex;
        public byte componentCount;
        public byte arraysCount;
        public byte tagsCount;
        public int componentRowSize;
        public Definition definitionMask;
        public MemoryAddress componentOffsets;
        public MemoryAddress sizes;
        public MemoryAddress typeHashes;
    }
}