using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Simulation
{
    public class ListenerTests
    {
        private static readonly List<int> received = new();

        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAny();
        }

        [SetUp]
        public void SetUp()
        {
            received.Clear();
        }

        [Test]
        public unsafe void SimpleListener()
        {
            using World world = new();
            world.CreateListener<SimpleEvent>(&OnSimpleEvent);
            world.Submit(new SimpleEvent { x = 1 });
            world.Submit(new SimpleEvent { x = 2 });
            world.Poll();

            Assert.That(received, Is.EquivalentTo(new[] { 1, 2 }));
        }

        [UnmanagedCallersOnly]
        private static void OnSimpleEvent(World world, Container message)
        {
            ref SimpleEvent simpleEvent = ref message.Read<SimpleEvent>();
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
            Listener listener = world.CreateListener(RuntimeType.Get<SimpleEvent>(), &OneEventOnly);
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
            ref SimpleEvent simpleEvent = ref message.Read<SimpleEvent>();
            received.Add(simpleEvent.x);
        }

        [Test]
        public unsafe void OneMethodTwoListeners()
        {
            using World world = new();
            Listener listener1 = world.CreateListener(RuntimeType.Get<SimpleEvent>(), &OnSimpleEvent);
            Listener listener2 = world.CreateListener(RuntimeType.Get<SimpleEvent>(), &OnSimpleEvent);
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
}