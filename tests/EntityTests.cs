namespace Worlds.Tests
{
    public class EntityTests : WorldTests
    {
        [Test]
        public void CompareAgainstDefault()
        {
            Entity a = default;
            Assert.That(a == default, Is.True);
        }

        [Test]
        public void InitializeExistingEntity()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            Entity entity = new(world, a);
            entity.Dispose();
            Assert.That(entity.IsDisposed, Is.True);
            Assert.That(world.ContainsEntity(a), Is.False);
        }

        [Test]
        public void CreateEntityWithComponents()
        {
            using World world = CreateWorld();
            Entity a = Entity.Create(world, new Apple(32), new Another(10000));
            Assert.That(a, Is.Not.EqualTo(default(Entity)));
            Assert.That(a.ContainsComponent<Apple>(), Is.True);
            Assert.That(a.ContainsComponent<Another>(), Is.True);
            Assert.That(a.GetComponent<Apple>(), Is.EqualTo(new Apple(32)));
            Assert.That(a.GetComponent<Another>(), Is.EqualTo(new Another(10000)));
        }

        [Test]
        public void CreateAnotherEntity()
        {
            using World world = CreateWorld();
            AnotherEntity entity = new(world, new Another(1337));
            Assert.That(entity.GetComponent<Another>().data, Is.EqualTo(1337));
        }

        [Test]
        public void BecomeAnother()
        {
            using World world = CreateWorld();
            Entity entity = new(world);
            Assert.That(entity.Is<AnotherEntity>(), Is.False);
            entity.Become<AnotherEntity>();
            Assert.That(entity.ContainsComponent<Another>(), Is.True);
            Assert.That(entity.Is<AnotherEntity>(), Is.True);
        }
    }
}