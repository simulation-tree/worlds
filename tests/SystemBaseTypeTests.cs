using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public class SystemBaseTypeTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAnyAllocation();
        }

        [Test]
        public void ListenToEvent()
        {
            using World world = new();
            TestSystem system = new(world);
            world.Submit(new TestEvent(42));
            world.Poll();
            Assert.That(system.received, Has.Count.EqualTo(1));
            Assert.That(system.received[0].data, Is.EqualTo(42));
            world.Submit(new TestEvent(43));
            world.Poll();
            Assert.That(system.received, Has.Count.EqualTo(2));
            Assert.That(system.received[1].data, Is.EqualTo(43));
            Span<TestEvent> received = stackalloc TestEvent[(int)system.received.Count];
            system.received.CopyTo(received);
            system.Dispose();
            world.Submit(new TestEvent(44));
            world.Poll();
            Assert.That(received.ToArray(), Has.Length.EqualTo(2));
            Assert.That(received[0].data, Is.EqualTo(42));
            Assert.That(received[1].data, Is.EqualTo(43));
        }

        [Test]
        public void MultipleSystems()
        {
            using World world = new();
            using TestSystem system1 = new(world);
            using TestSystem system2 = new(world);
            world.Submit(new TestEvent(42));
            world.Poll();
            Assert.Multiple(() =>
            {
                Assert.That(system1.received, Has.Count.EqualTo(1));
                Assert.That(system2.received, Has.Count.EqualTo(1));
            });
        }

        public readonly struct TestEvent
        {
            public readonly uint data;

            public TestEvent(uint data)
            {
                this.data = data;
            }
        }

        public class TestSystem : SystemBase
        {
            public readonly UnmanagedList<TestEvent> received;

            public TestSystem(World world) : base(world)
            {
                received = new();
                Listen<TestEvent>(OnEvent);
            }

            protected override void OnDisposed()
            {
                received.Dispose();
            }

            private void OnEvent(TestEvent e)
            {
                received.Add(e);
            }
        }
    }
}