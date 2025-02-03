namespace Worlds.Tests
{
    public readonly partial struct AnotherEntity : IEntity
    {
        public AnotherEntity(World world, Another another = default)
        {
            this.value = world.CreateEntity(another);
            this.world = world;
        }

        public readonly void Describe(ref Archetype archetype)
        {
            archetype.AddComponentType<Another>();
            archetype.AddArrayType<Byte>();
        }
    }
}