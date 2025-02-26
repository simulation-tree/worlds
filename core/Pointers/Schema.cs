using Unmanaged;

namespace Worlds.Pointers
{
    public struct Schema
    {
        public readonly uint schemaIndex;

        internal byte componentCount;
        internal byte arraysCount;
        internal byte tagsCount;
        internal BitMask tagsMask;
        internal readonly Allocation sizes;
        internal readonly Allocation typeHashes;

        internal Schema(uint schemaIndex)
        {
            componentCount = 0;
            arraysCount = 0;
            tagsCount = 0;
            sizes = Allocation.CreateZeroed(Worlds.Schema.SizesLengthInBytes);
            typeHashes = Allocation.CreateZeroed(Worlds.Schema.TypeHashesLengthInBytes);
            this.schemaIndex = schemaIndex;
        }
    }
}