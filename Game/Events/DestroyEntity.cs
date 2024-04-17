namespace Game.Requests
{
    public readonly unsafe struct DestroyEntity
    {
        public readonly delegate* unmanaged<World, EntityID, void> callback;
        public readonly EntityID entity;

        public DestroyEntity(EntityID entity, delegate* unmanaged<World, EntityID, void> callback = default)
        {
            this.entity = entity;
            this.callback = callback;
        }
    }
}
