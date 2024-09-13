using Unmanaged;
using Unmanaged.Collections;

namespace Simulation.Unsafe
{
    public struct EntityDescription
    {
        public uint entity;
        public uint parent;
        public int componentsKey;
        public UnmanagedList<Allocation> arrays;
        public UnmanagedList<RuntimeType> arrayTypes;
        public UnmanagedList<uint> arrayLengths;
        public UnmanagedList<uint> children;
        public UnmanagedList<uint> references;
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