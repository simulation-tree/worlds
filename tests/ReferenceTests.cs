namespace Worlds.Tests
{
    public class ReferenceTests : WorldTests
    {
        [Test]
        public void ReferenceAnotherEntity()
        {
            using World world = CreateWorld();
            uint entity1 = world.CreateEntity();
            uint entity2 = world.CreateEntity();
            ComponentThatReferences component = new(world.AddReference(entity1, entity2));
            Assert.That(world.GetReference(entity1, component.reference), Is.EqualTo(entity2));
        }

        [Test]
        public void DefaultValueCantExist()
        {
            using World world = CreateWorld();
            uint entity = world.CreateEntity();
            Assert.That(world.ContainsReference(entity, default(rint)), Is.False);
        }

        [Test]
        public void CloneEntityWithOneReference()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            rint reference = world.AddReference(a, b);
            Assert.That(world.GetReferenceCount(a), Is.EqualTo(1));
            uint c = world.CloneEntity(a);
            Assert.That(world.GetReferenceCount(c), Is.EqualTo(1));
            Assert.That(world.GetReference(c, reference), Is.EqualTo(world.GetReference(a, reference)));
        }

        [Test]
        public void CountReferences()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();

            Assert.That(world.GetReferenceCount(a), Is.EqualTo(0));

            world.AddReference(a, b);

            Assert.That(world.GetReferenceCount(a), Is.EqualTo(1));

            world.AddReference(a, c);

            Assert.That(world.GetReferenceCount(a), Is.EqualTo(2));

            world.RemoveReference(a, b);

            Assert.That(world.GetReferenceCount(a), Is.EqualTo(1));

            world.RemoveReference(a, c);

            Assert.That(world.GetReferenceCount(a), Is.EqualTo(0));
        }

        [Test]
        public void DestroyingThenCreating()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            world.AddReference(a, b);
            world.DestroyEntity(a);

            uint c = world.CreateEntity();

            Assert.That(c, Is.EqualTo(a));
            Assert.That(world.GetReferenceCount(c), Is.EqualTo(0));
        }

        [Test]
        public void CreateNewEntitiesAfterReferencing()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            world.AddReference(a, b);

            Assert.That(world.GetReferenceCount(a), Is.EqualTo(1));

            uint c = world.CreateEntity();

            Assert.That(world.GetReferenceCount(a), Is.EqualTo(1));

            world.AddReference(a, c);

            Assert.That(world.GetReferenceCount(a), Is.EqualTo(2));
        }
    }
}