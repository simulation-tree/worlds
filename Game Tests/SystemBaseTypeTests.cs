using Unmanaged.Collections;

namespace Game
{
    public class SystemBaseTypeTests
    {
        [Test]
        public void ListenToEvent()
        {
            using World world = new();
            TestSystem system = new(world);
            world.Submit(new TestEvent(42));
            world.Poll();
            Assert.That(system.received.Count, Is.EqualTo(1));
            Assert.That(system.received[0].data, Is.EqualTo(42));
            world.Submit(new TestEvent(43));
            world.Poll();
            Assert.That(system.received.Count, Is.EqualTo(2));
            Assert.That(system.received[1].data, Is.EqualTo(43));
            system.Dispose();
            world.Submit(new TestEvent(44));
            world.Poll();
            Assert.That(system.received.Count, Is.EqualTo(2));
        }

        [Test]
        public void MultipleSystems()
        {
            using World world = new();
            using TestSystem system1 = new(world);
            using TestSystem system2 = new(world);
            world.Submit(new TestEvent(42));
            world.Poll();
            Assert.That(system1.received.Count, Is.EqualTo(1));
            Assert.That(system2.received.Count, Is.EqualTo(1));
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

            protected override void Disposed()
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