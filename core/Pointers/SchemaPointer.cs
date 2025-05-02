namespace Worlds.Pointers
{
    internal unsafe struct SchemaPointer
    {
        public int schemaIndex;
        public int componentCount;
        public int arraysCount;
        public int tagsCount;
        public int componentRowSize;
        public Definition definitionMask;
        public int* componentOffsets;
        public int* sizes;
        public long* typeHashes;
    }
}