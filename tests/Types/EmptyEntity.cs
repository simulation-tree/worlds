namespace Worlds.Tests
{
    public readonly partial struct EmptyEntity : IEntity
    {
        public EmptyEntity(World world)
        {
            this.world = world;
            this.value = world.CreateEntity();
        }

        readonly void IEntity.Describe(ref Archetype archetype)
        {
        }
    }
}