using Game.Events;
using System.Runtime.InteropServices;

namespace Game
{
    public class ObserverTests
    {
        private static readonly List<EntityID> added = [];
        private static readonly List<EntityID> removed = [];

        [Test]
        public unsafe void ListenForChanges()
        {
            using World world = new();
            EntityID entity = world.CreateEntity();
            using ComponentObserver observer = new(world, ComponentType.Get<SimpleComponent>(), &OnAdded, &OnRemoved);
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
        public unsafe void DisposeObserver()
        {
            using World world = new();
            EntityID entity = world.CreateEntity();
            ComponentObserver observer = new(world, ComponentType.Get<SimpleComponent>(), &OnAdded, &OnRemoved);
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