using Game.Events;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Game
{
    public class ObserverTests
    {
        private static readonly List<EntityID> added = [];
        private static readonly List<EntityID> removed = [];

        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAnyAllocation();
            added.Clear();
            removed.Clear();
        }

        [Test]
        public unsafe void ListenForChanges()
        {
            using World world = new();
            EntityID entity = world.CreateEntity();
            using ComponentObserver observer = new(world, RuntimeType.Get<SimpleComponent>(), &OnAdded, &OnRemoved);
            world.AddComponent(entity, new SimpleComponent());
            world.Submit(new Update());
            world.Poll();

            Assert.That(added, Is.EquivalentTo(new[] { entity }));

            world.RemoveComponent<SimpleComponent>(entity);

            world.Submit(new Update());
            world.Poll();

            Assert.That(removed, Is.EquivalentTo(new[] { entity }));
        }

        [Test]
        public unsafe void ListenForManyChanges()
        {
            using World world = new();
            EntityID[] entities = new EntityID[10];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = world.CreateEntity();
            }

            using ComponentObserver observer = new(world, RuntimeType.Get<Data>(), &OnAdded, &OnRemoved);
            for (int i = 0; i < entities.Length; i++)
            {
                world.AddComponent(entities[i], new Data(i));
            }

            world.Submit(new Update());
            world.Poll();

            Assert.That(added, Is.EquivalentTo(entities));

            for (int i = 0; i < entities.Length; i++)
            {
                world.RemoveComponent<Data>(entities[i]);
            }

            world.Submit(new Update());
            world.Poll();

            Assert.That(removed, Is.EquivalentTo(entities));
        }

        [Test]
        public unsafe void DisposeObserver()
        {
            using World world = new();
            EntityID entity = world.CreateEntity();
            ComponentObserver observer = new(world, RuntimeType.Get<SimpleComponent>(), &OnAdded, &OnRemoved);
            world.AddComponent(entity, new SimpleComponent());
            world.Submit(new Update());
            world.Poll();

            Assert.That(added, Is.EquivalentTo(new[] { entity }));

            added.Clear();
            observer.Dispose();

            world.RemoveComponent<SimpleComponent>(entity);

            world.Submit(new Update());
            world.Poll();

            Assert.That(added, Is.Empty);
            Assert.That(removed, Is.Empty);
        }

        public struct SimpleComponent
        {
        }

        public struct Data
        {
            public int value;

            public Data(int value)
            {
                this.value = value;
            }
        }

        [UnmanagedCallersOnly]
        private static void OnAdded(World world, EntityID entity)
        {
            added.Add(entity);
        }

        [UnmanagedCallersOnly]
        private static void OnRemoved(World world, EntityID entity)
        {
            removed.Add(entity);
        }
    }
}