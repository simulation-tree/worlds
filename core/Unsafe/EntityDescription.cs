using Collections;
using Unmanaged;

namespace Simulation.Unsafe
{
    public struct EntityDescription
    {
        public uint entity;
        public uint parent;
        public int chunkKey;
        public ushort childCount;
        public List<uint> children;
        public ushort referenceCount;
        public List<uint> references;
        public byte arrayCount;
        public List<Allocation> arrays;
        public List<ArrayType> arrayTypes;
        public List<uint> arrayLengths;
        public State state;

        public EntityDescription(uint entity)
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