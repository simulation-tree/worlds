using System;
using System.Collections.Generic;
using Unmanaged;

namespace Simulation.Tests
{
    public class SystemBaseTypeTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAny();
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
            TestSystem system1 = new(world);
            TestSystem system2 = new(world);
            world.Submit(new TestEvent(42));
            world.Poll();
            Assert.Multiple(() =>
            {
                Assert.That(system1.received, Has.Count.EqualTo(1));
                Assert.That(system2.received, Has.Count.EqualTo(1));
            });

            system1.Dispose();
            world.Submit(new TestEvent(43));
            world.Poll();
            Assert.Multiple(() =>
            {
                Assert.That(system1.received, Has.Count.EqualTo(1));
                Assert.That(system2.received, Has.Count.EqualTo(2));
                Assert.That(system2.received[1].data, Is.EqualTo(43));
            });

            system2.Dispose();
        }

        public class TestSystem : SystemBase
        {
            public readonly List<TestEvent> received = [];

            public TestSystem(World world) : base(world)
            {
                Subscribe<TestEvent>(OnEvent);
            }

            private void OnEvent(TestEvent e)
            {
                received.Add(e);
            }
        }
    }

    public readonly struct TestEvent
    {
        public readonly uint data;

        public TestEvent(uint data)
        {
            this.data = data;
        }
    }
}