namespace Worlds.Tests
{
    public class ParentingTests : WorldTests
    {
        [Test]
        public void SetParent()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            world.SetParent(b, a);
            Assert.That(world.GetParent(b), Is.EqualTo(a));
        }

        [Test]
        public void NotParented()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            Assert.That(world.GetParent(entity), Is.EqualTo(default(uint)));
        }

        [Test]
        public void CountChildren()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();

            Assert.That(world.GetChildCount(a), Is.EqualTo(0));

            world.SetParent(c, a);

            Assert.That(world.GetChildCount(a), Is.EqualTo(1));

            world.SetParent(c, b);

            Assert.That(world.GetChildCount(a), Is.EqualTo(0));
            Assert.That(world.GetChildCount(b), Is.EqualTo(1));

            world.DestroyEntity(c);

            Assert.That(world.GetChildCount(b), Is.EqualTo(0));

            world.SetParent(b, a);

            Assert.That(world.GetChildCount(a), Is.EqualTo(1));

            world.DestroyEntity(a);
            uint d = world.CreateEntity();
            uint e = world.CreateEntity();

            Assert.That(world.GetChildCount(d), Is.EqualTo(0));

            world.SetParent(e, d);

            Assert.That(world.GetChildCount(d), Is.EqualTo(1));
        }

        [Test]
        public void ChildrenUpdateAfterChildGetsDestroyed()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();

            Assert.That(world.GetChildCount(a), Is.EqualTo(0));

            world.SetParent(b, a);

            Assert.That(world.GetChildCount(a), Is.EqualTo(1));

            world.DestroyEntity(b);

            Assert.That(world.GetChildCount(a), Is.EqualTo(0));
        }
    }
}