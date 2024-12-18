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
            using World world = new();
            Entity a = new(world, 0);
            Assert.That(a, Is.EqualTo(default(Entity)));
        }
    }
}