using Unmanaged;

namespace Worlds.Pointers
{
    public struct Schema
    {
        public readonly int schemaIndex;

        internal byte componentCount;
        internal byte arraysCount;
        internal byte tagsCount;
        internal Definition definitionMask;
        internal readonly MemoryAddress sizes;
        internal readonly MemoryAddress typeHashes;

        internal Schema(int schemaIndex)
        {
            componentCount = 0;
            arraysCount = 0;
            tagsCount = 0;
            definitionMask = default;
            sizes = MemoryAddress.AllocateZeroed(Worlds.Schema.SizesLengthInBytes);
            typeHashes = MemoryAddress.AllocateZeroed(Worlds.Schema.TypeHashesLengthInBytes);
            this.schemaIndex = schemaIndex;
        }
    }
}