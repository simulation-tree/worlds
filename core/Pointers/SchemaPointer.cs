namespace Worlds.Pointers
{
    internal unsafe struct SchemaPointer
    {
        public uint* componentOffsets;
        public int* sizes;
        public int schemaIndex;
        public int componentCount;
        public int arraysCount;
        public int tagsCount;
        public uint componentRowSize;
        public Definition definitionMask;
        public long* typeHashes;
    }
}