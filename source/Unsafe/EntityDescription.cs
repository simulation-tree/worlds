namespace Game.Unsafe
{
    public struct EntityDescription(EntityID entity, uint componentsKey)
    {
        public EntityID entity = entity;
        public uint componentsKey = componentsKey;
        public EntityCollections collections;
        public State state;

        public readonly bool IsEnabled => state == State.Enabled;
        public readonly bool IsDestroyed => state == State.Destroyed;

        public void SetEnabledState(bool value)
        {
            state = value ? State.Enabled : State.Disabled;
        }

        public enum State : byte
        {
            Enabled,
            Disabled,
            Destroyed
        }
    }
}