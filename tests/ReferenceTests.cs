using System;

namespace Worlds.Tests
{
    public class ReferenceTests : WorldTests
    {
        [Test]
        public void ReferenceAnotherEntity()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            Assert.That(world.ContainsReference(a, b), Is.False);
            ComponentThatReferences component = new(world.AddReference(a, b));
            Assert.That(world.GetReference(a, component.reference), Is.EqualTo(b));
            Assert.That(world.ContainsReference(a, b), Is.True);
            world.RemoveReference(a, component.reference);
            Assert.That(world.ContainsReference(a, b), Is.False);
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

            world.AddReference(a, b);
            world.AddReference(a, c);

            ReadOnlySpan<uint> referencedEntities = world.GetReferences(a);
            Assert.That(referencedEntities.Length, Is.EqualTo(2));
            Assert.That(referencedEntities[0], Is.EqualTo(b));
            Assert.That(referencedEntities[1], Is.EqualTo(c));
        }

        [Test]
        public void ModifyReferencesOnMiddleEntity()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            uint d = world.CreateEntity();

            world.AddReference(a, d);
            world.AddReference(b, c);
            world.AddReference(a, b);
            world.AddReference(b, a);
            world.AddReference(b, d);

            ReadOnlySpan<uint> referencedEntities = world.GetReferences(a);
            Assert.That(referencedEntities.Length, Is.EqualTo(2));
            Assert.That(referencedEntities[0], Is.EqualTo(d));
            Assert.That(referencedEntities[1], Is.EqualTo(b));

            referencedEntities = world.GetReferences(b);
            Assert.That(referencedEntities.Length, Is.EqualTo(3));
            Assert.That(referencedEntities[0], Is.EqualTo(c));
            Assert.That(referencedEntities[1], Is.EqualTo(a));
            Assert.That(referencedEntities[2], Is.EqualTo(d));
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

            world.AddReference(c, b);

            Assert.That(world.GetReferenceCount(c), Is.EqualTo(1));
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