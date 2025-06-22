namespace Worlds.Pointers
{
    internal unsafe struct SchemaPointer
    {
        public int schemaIndex;
        public int componentCount;
        public int arraysCount;
        public int tagsCount;
        public uint componentRowSize;
        public Definition definitionMask;
        public uint* componentOffsets;
        public int* sizes;
        public long* typeHashes;
    }
}