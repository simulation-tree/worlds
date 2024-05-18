using System;
using System.Collections.Generic;
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