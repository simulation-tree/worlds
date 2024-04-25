using Game.Events;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Game
{
    public class ListenerTests
    {
        private static readonly List<int> received = new();

        [SetUp]
        public void SetUp()
        {
            received.Clear();
        }

        [Test]
        public unsafe void SimpleListener()
        {
            using World world = new();
            world.Listen<SimpleEvent>(&OnSimpleEvent);
            world.Submit(new SimpleEvent { x = 1 });
            world.Submit(new SimpleEvent { x = 2 });
            world.Poll();

            Assert.That(received, Is.EquivalentTo(new[] { 1, 2 }));
        }

        [UnmanagedCallersOnly]
        private static void OnSimpleEvent(World world, Container message)
        {
            ref SimpleEvent simpleEvent = ref message.AsRef<SimpleEvent>();
            received.Add(simpleEvent.x);
        }

        public struct SimpleEvent
        {
            public int x;
        }

        [Test]
        public unsafe void AddingListenerThenRemoving()
        {
            using World world = new();
            Listener listener = world.Listen(RuntimeType.Get<SimpleEvent>(), &OneEventOnly);
            world.Submit(new SimpleEvent { x = 1 });
            world.Poll();

            Assert.That(received, Is.EquivalentTo(new[] { 1 }));

            listener.Dispose();
            world.Submit(new SimpleEvent { x = 2 });
            world.Poll();

            Assert.That(received, Is.EquivalentTo(new[] { 1 }));
        }

        [UnmanagedCallersOnly]
        private static void OneEventOnly(World world, Container message)
        {
            ref SimpleEvent simpleEvent = ref message.AsRef<SimpleEvent>();
            received.Add(simpleEvent.x);
        }

        [Test]
        public unsafe void OneMethodTwoListeners()
        {
            using World world = new();
            Listener listener1 = world.Listen(RuntimeType.Get<SimpleEvent>(), &OnSimpleEvent);
            Listener listener2 = world.Listen(RuntimeType.Get<SimpleEvent>(), &OnSimpleEvent);
            world.Submit(new SimpleEvent { x = 1 });
            world.Poll();

            Assert.That(received, Is.EquivalentTo(new[] { 1, 1 }));

            listener1.Dispose();

            received.Clear();
            world.Submit(new SimpleEvent { x = 2 });
            world.Poll();
            Assert.That(received, Is.EquivalentTo(new[] { 2 }));

            listener2.Dispose();

            received.Clear();
            world.Submit(new SimpleEvent { x = 3 });
            world.Poll();
            Assert.That(received, Is.Empty);
        }
    }

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