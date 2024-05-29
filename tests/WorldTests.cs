using System;
using System.Collections.Generic;
using System.Linq;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public class WorldTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAnyAllocation();
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
        public void GetAddedComponent()
        {
            using World world = new();
            EntityID entity = world.CreateEntity();
            SimpleComponent component = new("Hello World");
            world.AddComponent(entity, component);

            Assert.That(world.GetComponent<SimpleComponent>(entity), Is.EqualTo(component));
        }

        [Test]
        public void CreateAndDestroyEntity()
        {
            using World world = new();
            EntityID entity = world.CreateEntity();
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);
            Assert.That(world.Count, Is.EqualTo(0));
            Assert.Throws<NullReferenceException>(() => world.GetComponent<SimpleComponent>(entity));
        }

        [Test]
        public void TwoComponents()
        {
            using World world = new();
            EntityID entity = world.CreateEntity();
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
            EntityID entity = world.CreateEntity();
            Assert.That(world.ContainsEntity(entity), Is.True);
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);

            EntityID another = world.CreateEntity();
            Assert.That(world.ContainsEntity(another), Is.True);
            world.DestroyEntity(another);
            Assert.That(world.ContainsEntity(another), Is.False);
        }

        [Test]
        public void DestroyEntityWithCollection()
        {
            World world = new();
            EntityID entity = world.CreateEntity();
            UnmanagedList<SimpleComponent> list = world.CreateCollection<SimpleComponent>(entity);
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
            EntityID entity = world.CreateEntity();
            UnmanagedList<SimpleComponent> list = world.CreateCollection<SimpleComponent>(entity);
            list.Add(new("apple"));
            world.DestroyEntity(entity);
            Assert.That(list.IsDisposed, Is.True);
            EntityID another = world.CreateEntity();
            UnmanagedList<SimpleComponent> anotherList = world.CreateCollection<SimpleComponent>(another);
            anotherList.Add(new("banana"));
            Assert.That(anotherList.Count, Is.EqualTo(1));
            world.DestroyEntity(another);
            Assert.That(anotherList.IsDisposed, Is.True);
            world.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void QueryComponentsAfterDestroyingEntities()
        {
            using World world = new();
            EntityID entity1 = world.CreateEntity();
            EntityID entity2 = world.CreateEntity();
            SimpleComponent component1 = new("apple");
            SimpleComponent component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            world.DestroyEntity(entity1);
            world.AddComponent(entity2, new Another(5));
            world.DestroyEntity(entity2);
            List<(EntityID, SimpleComponent)> found = new();
            world.QueryComponents((in EntityID entity, ref SimpleComponent component) =>
            {
                found.Add((entity, component));
            });

            Assert.That(found.Count, Is.EqualTo(0));
        }

        [Test]
        public void QueryMultipleComponents()
        {
            using World world = new();
            EntityID entity1 = world.CreateEntity();
            EntityID entity2 = world.CreateEntity();
            SimpleComponent component1 = new("apple");
            SimpleComponent component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            EntityID entity3 = world.CreateEntity();
            EntityID entity4 = world.CreateEntity();
            Another another1 = new(5);
            Another another2 = new(10);
            world.AddComponent(entity3, another1);
            world.AddComponent(entity4, another2);
            EntityID entity5 = world.CreateEntity();
            world.AddComponent(entity5, component1);
            world.AddComponent(entity5, another2);
            List<EntityID> simpleComponents = world.Query<SimpleComponent>().ToList();
            List<EntityID> anotherComponents = world.Query<Another>().ToList();
            Assert.That(simpleComponents.Count, Is.EqualTo(3));
            Assert.That(anotherComponents.Count, Is.EqualTo(3));
            Assert.That(simpleComponents.Contains(entity1), Is.True);
            Assert.That(simpleComponents.Contains(entity2), Is.True);
            Assert.That(simpleComponents.Contains(entity5), Is.True);
            Assert.That(anotherComponents.Contains(entity3), Is.True);
            Assert.That(anotherComponents.Contains(entity4), Is.True);
            Assert.That(anotherComponents.Contains(entity5), Is.True);
        }

        [Test]
        public void ComponentBuffer()
        {
            using World world = new();
            EntityID entity1 = world.CreateEntity();
            EntityID entity2 = world.CreateEntity();
            SimpleComponent component1 = new("apple");
            SimpleComponent component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            EntityID entity3 = world.CreateEntity();
            EntityID entity4 = world.CreateEntity();
            Another another1 = new(5);
            Another another2 = new(10);
            world.AddComponent(entity3, another1);
            world.AddComponent(entity4, another2);
            EntityID entity5 = world.CreateEntity();
            world.AddComponent(entity5, component1);
            world.AddComponent(entity5, another2);
            using UnmanagedList<SimpleComponent> buffer = new();
            using UnmanagedList<EntityID> entities = new();
            world.Fill(buffer, entities);
            Assert.That(buffer.Count, Is.EqualTo(3));
            var entitiesSpan = entities.AsSpan();
            Assert.That(entities.Contains(entity1), Is.True);
            Assert.That(entities.Contains(entity2), Is.True);
            Assert.That(entities.Contains(entity5), Is.True);
            entities.Clear();
            using UnmanagedList<Another> anotherBuffer = new();
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
            using RandomGenerator rng = new();
            uint realEntities = 0;
            for (int i = 0; i < 100; i++)
            {
                EntityID entity = world.CreateEntity();
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
                    world.CreateCollection<char>(entity);
                    uint length = rng.NextUInt(1, 10);
                    for (uint j = 0; j < length; j++)
                    {
                        world.AddToCollection(entity, (char)rng.NextInt('a', 'z'));
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
                EntityID entity = new(i + 1);
                Assert.That(world.ContainsEntity(entity), Is.False);
            }
        }

        public struct SimpleComponent
        {
            public FixedString data;

            public SimpleComponent(FixedString data)
            {
                this.data = data;
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
}