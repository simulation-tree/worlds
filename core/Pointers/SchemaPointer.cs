using Unmanaged;

namespace Worlds.Pointers
{
    internal struct SchemaPointer
    {
        public readonly int schemaIndex;

        public byte componentCount;
        public byte arraysCount;
        public byte tagsCount;
        public int componentRowSize;
        public Definition definitionMask;
        public readonly MemoryAddress offsets;
        public readonly MemoryAddress sizes;
        public readonly MemoryAddress typeHashes;

        public SchemaPointer(int schemaIndex)
        {
            componentCount = 0;
            arraysCount = 0;
            tagsCount = 0;
            componentRowSize = 0;
            definitionMask = default;
            offsets = MemoryAddress.AllocateZeroed(Worlds.Schema.OffsetsLengthInBytes);
            sizes = MemoryAddress.AllocateZeroed(Worlds.Schema.SizesLengthInBytes);
            typeHashes = MemoryAddress.AllocateZeroed(Worlds.Schema.TypeHashesLengthInBytes);
            this.schemaIndex = schemaIndex;
        }
    }
}