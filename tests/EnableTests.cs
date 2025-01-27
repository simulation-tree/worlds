namespace Worlds.Tests
{
    public class EnableTests : WorldTests
    {
        [Test]
        public void BecomeDisabled()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            world.SetEnabled(a, false);
            Assert.That(world.IsEnabled(a), Is.False);
        }

        [Test]
        public void EnabledByDefault()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            Assert.That(world.IsEnabled(entity), Is.True);
        }

        [Test]
        public void ChildrenBecomeDisabledWhenParentDoes()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            world.SetParent(b, a);
            world.SetEnabled(a, false);
            Assert.That(world.IsEnabled(a), Is.False);
            Assert.That(world.IsEnabled(b), Is.False);
            Assert.That(world.IsLocallyEnabled(b), Is.True);
        }

        [Test]
        public void GrandChildIsDisabledBecauseOfParent()
        {
            using World world = CreateWorld();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            uint d = world.CreateEntity();
            uint a = world.CreateEntity();
            world.SetParent(b, a);
            world.SetParent(c, b);
            world.SetEnabled(a, false);

            Assert.That(world.IsEnabled(a), Is.EqualTo(false));
            Assert.That(world.IsEnabled(b), Is.EqualTo(false));
            Assert.That(world.IsLocallyEnabled(b), Is.EqualTo(true));
            Assert.That(world.IsEnabled(c), Is.EqualTo(false));
            Assert.That(world.IsLocallyEnabled(c), Is.EqualTo(true));
            Assert.That(world.IsEnabled(d), Is.EqualTo(true));
        }

        [Test]
        public void CountChildrenAfterCreatingEntities()
        {
            using World world = CreateWorld();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            uint a = world.CreateEntity();
            world.SetParent(b, a);

            Assert.That(world.GetChildCount(a), Is.EqualTo(1));
            Assert.That(world.GetChildCount(b), Is.EqualTo(0));
            Assert.That(world.GetChildCount(c), Is.EqualTo(0));

            world.SetParent(c, b);

            Assert.That(world.GetChildCount(a), Is.EqualTo(1));
            Assert.That(world.GetChildCount(b), Is.EqualTo(1));
            Assert.That(world.GetChildCount(c), Is.EqualTo(0));
        }
    }
}