using Collections;
using Unmanaged;

namespace Simulation.Unsafe
{
    public struct EntityDescription
    {
        public uint entity;
        public uint parent;
        public int componentsKey;
        public List<Allocation> arrays;
        public List<RuntimeType> arrayTypes;
        public List<uint> arrayLengths;
        public List<uint> children;
        public List<uint> references;
        public State state;

        public enum State : byte
        {
            Enabled,
            Disabled,
            Destroyed,
            EnabledButDisabledDueToAncestor
        }
    }
}