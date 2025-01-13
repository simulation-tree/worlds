﻿namespace Worlds.Tests
{
    public class EntityReferenceTests : WorldTests
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
        public void AppendWorldWithReferencedEntities()
        {
            using World firstWorld = CreateWorld();
            uint entity1 = firstWorld.CreateEntity(); //1
            uint entity2 = firstWorld.CreateEntity(); //2
            ComponentThatReferences component = new(firstWorld.AddReference(entity1, entity2)); //1->2
            firstWorld.AddComponent(entity1, component);
            firstWorld.AddComponent(entity2, new ReferencedEntity());

            using World secondWorld = CreateWorld();
            uint entity3 = secondWorld.CreateEntity(); //1
            uint entity4 = secondWorld.CreateEntity(); //2
            secondWorld.Append(firstWorld); //1->2 becomes 3->4

            secondWorld.GetFirstComponent<ComponentThatReferences>(out entity1);
            secondWorld.GetFirstComponent<ReferencedEntity>(out entity2);
            Assert.That(secondWorld.GetReference(entity1, component.reference), Is.EqualTo(entity2));
        }

        [Test]
        public void AppendWorldWithParents()
        {
            using World firstWorld = CreateWorld();
            uint parent = firstWorld.CreateEntity();
            uint child = firstWorld.CreateEntity();
            firstWorld.SetParent(child, parent);
            firstWorld.AddComponent(parent, (Integer)0);
            firstWorld.AddComponent(child, (Float)0);

            using World secondWorld = CreateWorld();
            for (uint i = 0; i < 4; i++)
            {
                secondWorld.CreateEntity();
            }

            secondWorld.Append(firstWorld);
            secondWorld.GetFirstComponent<Integer>(out parent);
            secondWorld.GetFirstComponent<Float>(out child);
            Assert.That(secondWorld.GetParent(child), Is.EqualTo(parent));
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
    }
}