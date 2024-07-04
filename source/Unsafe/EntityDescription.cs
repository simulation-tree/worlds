using Unmanaged.Collections;

namespace Simulation.Unsafe
{
    public struct EntityDescription(uint entity, uint parent, uint componentsKey)
    {
        public uint entity = entity;
        public uint parent = parent;
        public uint componentsKey = componentsKey;
        public EntityCollections collections;
        public UnmanagedList<uint> children;
        public State state;

        public readonly bool IsEnabled => state == State.Enabled;
        public readonly bool IsSelfEnabled => state == State.Enabled || state == State.AncestorDisabledEnabled;
        public readonly bool IsDestroyed => state == State.Destroyed;

        public void SetEnabledState(bool value)
        {
            state = value ? State.Enabled : State.Disabled;
        }

        public enum State : byte
        {
            Enabled,
            Disabled,
            AncestorDisabledEnabled,
            AncestorDisabledDisabled,
            Destroyed
        }
    }
}