namespace Worlds.Tests
{
    public class EntityTagTests : WorldTests
    {
        [Test]
        public void AddThenRemoveTag()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            world.AddTag<IsThing>(entity);

            Assert.That(world.ContainsTag<IsThing>(entity), Is.True);

            world.RemoveTag<IsThing>(entity);

            Assert.That(world.ContainsTag<IsThing>(entity), Is.False);
        }
    }
}