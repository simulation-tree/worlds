using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    public class WorldTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAny();
        }

        [Test]
        public void CreateAndDisposeWorld()
        {
            World world = new();
            world.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void DisposeTwiceError()
        {
            World world = new();
            world.Dispose();
            Assert.Throws<NullReferenceException>(() => world.Dispose());
        }

        [Test]
        public void NonCreatedWorldError()
        {
            World world = default;
            Assert.Throws<NullReferenceException>(() => world.Dispose());
        }

        [Test]
        public void DestroyParentEntity()
        {
            using World world = new();
            eint a = world.CreateEntity();
            eint b = world.CreateEntity(a);
            eint c = world.CreateEntity(a);
            Assert.That(world.ContainsEntity(a), Is.True);
            Assert.That(world.ContainsEntity(b), Is.True);
            Assert.That(world.ContainsEntity(c), Is.True);
            Assert.That(world.GetParent(b), Is.EqualTo(a));
            Assert.That(world.GetParent(c), Is.EqualTo(a));
            Assert.That(world.GetChildren(a).ToArray(), Has.Member(b));
            Assert.That(world.GetChildren(a).ToArray(), Has.Member(c));
            world.DestroyEntity(a);
            Assert.That(world.ContainsEntity(a), Is.False);
            Assert.That(world.ContainsEntity(b), Is.False);
            Assert.That(world.ContainsEntity(c), Is.False);
        }

        [Test]
        public void GetAddedComponent()
        {
            using World world = new();
            eint entity = world.CreateEntity();
            SimpleComponent component = new("Hello World");
            world.AddComponent(entity, component);

            Assert.That(world.GetComponent<SimpleComponent>(entity), Is.EqualTo(component));
            world.RemoveComponent<SimpleComponent>(entity);
            Assert.That(world.ContainsComponent<SimpleComponent>(entity), Is.False);
            Assert.That(world.ContainsComponent<SimpleComponent>(), Is.False);
            world.AddComponent(entity, component);
            Assert.That(world.ContainsComponent<SimpleComponent>(entity), Is.True);
            Assert.That(world.ContainsComponent<SimpleComponent>(), Is.True);
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);
        }

        [Test]
        public void CreateAndDestroyEntity()
        {
            using World world = new();
            eint entity = world.CreateEntity();
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);
            Assert.That(world.Count, Is.EqualTo(0));
            Assert.Throws<NullReferenceException>(() => world.GetComponent<SimpleComponent>(entity));
        }

        [Test]
        public void TwoComponents()
        {
            using World world = new();
            eint entity = world.CreateEntity();
            SimpleComponent component1 = new("Hello World");
            Another component2 = new(42);
            world.AddComponent(entity, component1);
            world.AddComponent(entity, component2);
            Assert.That(world.GetComponent<SimpleComponent>(entity), Is.EqualTo(component1));
            Assert.That(world.GetComponent<Another>(entity), Is.EqualTo(component2));
            world.RemoveComponent<SimpleComponent>(entity);
            Assert.Throws<NullReferenceException>(() => world.GetComponentRef<SimpleComponent>(entity));
            Assert.That(world.GetComponent<Another>(entity), Is.EqualTo(component2));
        }

        [Test]
        public void DestroyEntityTwice()
        {
            using World world = new();
            eint entity = world.CreateEntity();
            Assert.That(world.ContainsEntity(entity), Is.True);
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);

            eint another = world.CreateEntity();
            Assert.That(world.ContainsEntity(another), Is.True);
            world.DestroyEntity(another);
            Assert.That(world.ContainsEntity(another), Is.False);
        }

        [Test]
        public void EnablingAndDisabling()
        {
            using World world = new();
            eint a = world.CreateEntity();
            eint b = world.CreateEntity();
            world.AddComponent(a, new SimpleComponent("Hello World"));
            Assert.That(world.IsEnabled(a), Is.True);
            Assert.That(world.IsEnabled(b), Is.True);
            world.SetEnabled(a, false);
            Assert.That(world.IsEnabled(a), Is.False);
            Assert.That(world.IsEnabled(b), Is.True);
            world.SetEnabled(a, true);
            Assert.That(world.IsEnabled(a), Is.True);
        }

        [Test]
        public void DestroyEntityWithCollection()
        {
            World world = new();
            eint entity = world.CreateEntity();
            UnmanagedList<SimpleComponent> list = world.CreateList<SimpleComponent>(entity);
            list.Add(new("Hello World 1"));
            list.Add(new("Hello World 2"));
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);
            Assert.That(list.IsDisposed, Is.True);
            world.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void DestroyCollectionTwice()
        {
            World world = new();
            eint entity = world.CreateEntity();
            UnmanagedList<SimpleComponent> list = world.CreateList<SimpleComponent>(entity);
            list.Add(new("apple"));
            world.DestroyEntity(entity);
            Assert.That(list.IsDisposed, Is.True);
            eint another = world.CreateEntity();
            UnmanagedList<SimpleComponent> anotherList = world.CreateList<SimpleComponent>(another);
            anotherList.Add(new("banana"));
            Assert.That(anotherList.Count, Is.EqualTo(1));
            world.DestroyEntity(another);
            Assert.That(anotherList.IsDisposed, Is.True);
            world.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void ComponentBuffer()
        {
            using World world = new();
            eint entity1 = world.CreateEntity();
            eint entity2 = world.CreateEntity();
            SimpleComponent component1 = new("apple");
            SimpleComponent component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            eint entity3 = world.CreateEntity();
            eint entity4 = world.CreateEntity();
            Another another1 = new(5);
            Another another2 = new(10);
            world.AddComponent(entity3, another1);
            world.AddComponent(entity4, another2);
            eint entity5 = world.CreateEntity();
            world.AddComponent(entity5, component1);
            world.AddComponent(entity5, another2);
            using UnmanagedList<SimpleComponent> buffer = UnmanagedList<SimpleComponent>.Create();
            using UnmanagedList<eint> entities = UnmanagedList<eint>.Create();
            world.Fill(buffer, entities);
            Assert.That(buffer.Count, Is.EqualTo(3));
            var entitiesSpan = entities.AsSpan();
            Assert.That(entities.Contains(entity1), Is.True);
            Assert.That(entities.Contains(entity2), Is.True);
            Assert.That(entities.Contains(entity5), Is.True);
            entities.Clear();
            using UnmanagedList<Another> anotherBuffer = UnmanagedList<Another>.Create();
            world.Fill(anotherBuffer, entities);
            Assert.That(anotherBuffer.Count, Is.EqualTo(3));
            Assert.That(entities.Contains(entity3), Is.True);
            Assert.That(entities.Contains(entity4), Is.True);
            Assert.That(entities.Contains(entity5), Is.True);
        }

        [Test]
        public void PopulateWorldThenClear()
        {
            using World world = new();
            using RandomGenerator rng = RandomGenerator.Create();
            uint realEntities = 0;
            for (int i = 0; i < 100; i++)
            {
                eint entity = world.CreateEntity();
                if (rng.NextBool())
                {
                    world.AddComponent(entity, new SimpleComponent("apple"));
                }

                if (rng.NextBool())
                {
                    world.AddComponent(entity, new Another(5));
                }

                if (rng.NextBool())
                {
                    world.CreateList<char>(entity);
                    uint length = rng.NextUInt(1, 10);
                    for (uint j = 0; j < length; j++)
                    {
                        world.AddToList(entity, (char)rng.NextInt('a', 'z'));
                    }
                }

                if (rng.NextBool())
                {
                    world.DestroyEntity(entity);
                }
                else
                {
                    realEntities++;
                }
            }

            Assert.That(world.Count, Is.EqualTo(realEntities));
            world.Clear();
            Assert.That(world.Count, Is.EqualTo(0));
            for (uint i = 0; i < 100; i++)
            {
                uint entity = i + 1;
                Assert.That(world.ContainsEntity(entity), Is.False);
            }
        }

        public struct SimpleComponent
        {
            public FixedString data;

            public SimpleComponent(ReadOnlySpan<char> data)
            {
                this.data = new(data);
            }
        }

        public struct Another
        {
            public uint data;

            public Another(uint data)
            {
                this.data = data;
            }
        }
    }

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
            eint entity1 = world.CreateEntity();
            eint entity2 = world.CreateEntity();
            ComponentThatReferences component = new(world.AddReference(entity1, entity2));
            Assert.That(world.GetReference(entity1, component.reference), Is.EqualTo(entity2));
        }

        [Test]
        public void AppendWorldWithReferencedEntities()
        {
            using World firstWorld = new();
            eint entity1 = firstWorld.CreateEntity(); //1
            eint entity2 = firstWorld.CreateEntity(); //2
            ComponentThatReferences component = new(firstWorld.AddReference(entity1, entity2)); //1->2
            firstWorld.AddComponent(entity1, component);
            firstWorld.AddComponent(entity2, new ReferencedEntity());

            using World secondWorld = new();
            eint entity3 = secondWorld.CreateEntity(); //1
            eint entity4 = secondWorld.CreateEntity(); //2
            secondWorld.Append(firstWorld); //1->2 becomes 3->4

            secondWorld.GetFirst<ComponentThatReferences>(out entity1);
            secondWorld.GetFirst<ReferencedEntity>(out entity2);
            Assert.That(secondWorld.GetReference(entity1, component.reference), Is.EqualTo(entity2));
        }

        [Test]
        public void AppendWorldWithParents()
        {
            using World firstWorld = new();
            eint parent = firstWorld.CreateEntity();
            eint child = firstWorld.CreateEntity(parent);
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