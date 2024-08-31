using Unmanaged;

namespace Simulation.Tests
{
    public class EntityReferenceTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAny();
        }

        [Test]
        public void ReferenceAnotherEntity()
        {
            using World world = new();
            uint entity1 = world.CreateEntity();
            uint entity2 = world.CreateEntity();
            ComponentThatReferences component = new(world.AddReference(entity1, entity2));
            Assert.That(world.GetReference(entity1, component.reference), Is.EqualTo(entity2));
        }

        [Test]
        public void AppendWorldWithReferencedEntities()
        {
            using World firstWorld = new();
            uint entity1 = firstWorld.CreateEntity(); //1
            uint entity2 = firstWorld.CreateEntity(); //2
            ComponentThatReferences component = new(firstWorld.AddReference(entity1, entity2)); //1->2
            firstWorld.AddComponent(entity1, component);
            firstWorld.AddComponent(entity2, new ReferencedEntity());

            using World secondWorld = new();
            uint entity3 = secondWorld.CreateEntity(); //1
            uint entity4 = secondWorld.CreateEntity(); //2
            secondWorld.Append(firstWorld); //1->2 becomes 3->4

            secondWorld.GetFirst<ComponentThatReferences>(out entity1);
            secondWorld.GetFirst<ReferencedEntity>(out entity2);
            Assert.That(secondWorld.GetReference(entity1, component.reference), Is.EqualTo(entity2));
        }

        [Test]
        public void AppendWorldWithParents()
        {
            using World firstWorld = new();
            uint parent = firstWorld.CreateEntity();
            uint child = firstWorld.CreateEntity(parent);
            firstWorld.AddComponent(parent, (short)0);
            firstWorld.AddComponent(child, (ushort)0);

            using World secondWorld = new();
            for (int i = 0; i < 4; i++)
            {
                secondWorld.CreateEntity();
            }

            secondWorld.Append(firstWorld);
            secondWorld.GetFirst<short>(out parent);
            secondWorld.GetFirst<ushort>(out child);
            Assert.That(secondWorld.GetParent(child), Is.EqualTo(parent));
        }

        public struct ReferencedEntity
        {

        }

        public struct ComponentThatReferences
        {
            public rint reference;

            public ComponentThatReferences(rint reference)
            {
                this.reference = reference;
            }
        }
    }
}