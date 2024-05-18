namespace Game.Requests
{
    public readonly unsafe struct CreateEntity
    {
        public readonly delegate* unmanaged<World, EntityID, void> callback;

        public CreateEntity(delegate* unmanaged<World, EntityID, void> callback)
        {
            this.callback = callback;
        }
    }
}
