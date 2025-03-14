using Unmanaged;

namespace Worlds.Pointers
{
    public struct Schema
    {
        public readonly int schemaIndex;

        internal byte componentCount;
        internal byte arraysCount;
        internal byte tagsCount;
        internal int componentRowSize;
        internal Definition definitionMask;
        internal readonly MemoryAddress offsets;
        internal readonly MemoryAddress sizes;
        internal readonly MemoryAddress typeHashes;

        internal Schema(int schemaIndex)
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