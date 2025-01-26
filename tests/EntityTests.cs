namespace Worlds.Tests
{
    public class EntityTests : WorldTests
    {
        [Test]
        public void CompareAgainstDefault()
        {
            Entity a = default;
            Assert.That(a, Is.EqualTo(default(Entity)));
        }

        [Test]
        public void CompareAgainstDefaultDespiteWorld()
        {
            using World world = CreateWorld();
            Entity a = new(world, 0);
            Assert.That(a, Is.EqualTo(default(Entity)));
        }

        [Test]
        public void CreateEntityWithComponents()
        {
            using World world = CreateWorld();
            Entity a = new Entity<Apple, Another>(world, new Apple(32), new Another(10000));
            Assert.That(a, Is.Not.EqualTo(default(Entity)));
            Assert.That(a.ContainsComponent<Apple>(), Is.True);
            Assert.That(a.ContainsComponent<Another>(), Is.True);
            Assert.That(a.GetComponent<Apple>(), Is.EqualTo(new Apple(32)));
            Assert.That(a.GetComponent<Another>(), Is.EqualTo(new Another(10000)));
        }
    }
}