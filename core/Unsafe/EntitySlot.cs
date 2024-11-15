using Collections;
using Unmanaged;

namespace Simulation.Unsafe
{
    public struct EntitySlot
    {
        public uint entity;
        public uint parent;
        public int chunkKey;
        public ushort childCount;
        public List<uint> children;
        public ushort referenceCount;
        public List<uint> references;
        public Array<Allocation> arrays;
        public BitSet arrayTypes;
        public Array<uint> arrayLengths;
        public State state;

        public EntitySlot(uint entity)
        {
            this.entity = entity;
        }

        public enum State : byte
        {
            Enabled,
            Disabled,
            Destroyed,
            EnabledButDisabledDueToAncestor
        }
    }
}