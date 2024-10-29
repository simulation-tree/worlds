using Simulation.Functions;
using System;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Simulation.Tests
{
    public class SystemTests
    {
        [Test]
        public void SimpleTest()
        {
            using (World hostWorld = new())
            {
                using (Simulator simulator = new(hostWorld))
                {
                    simulator.AddSystem<SimpleSystem>();

                    Assert.That(simulator.SystemCount, Is.EqualTo(1));

                    simulator.Update(TimeSpan.FromSeconds(0.01f));

                    Assert.That(hostWorld.Count, Is.EqualTo(2));

                    Entity firstEntity = new(hostWorld, 1);
                    Assert.That(firstEntity.GetComponent<uint>(), Is.EqualTo(4));

                    Entity beforeFinalizeEntity = new(hostWorld, 2);
                    Assert.That(beforeFinalizeEntity.GetComponent<bool>(), Is.EqualTo(false));

                    simulator.RemoveSystem<SimpleSystem>();
                }

                Entity secondEntity = new(hostWorld, 2);
                Assert.That(secondEntity.GetComponent<bool>(), Is.EqualTo(true));
            }
        }

        [Test]
        public void ReceiveMessages()
        {
            using (World world = new())
            {
                using (Simulator simulator = new(world))
                {
                    simulator.AddSystem<MessageHandlerSystem>();

                    Assert.That(simulator.SystemCount, Is.EqualTo(1));

                    bool handled = simulator.TryHandleMessage(new FixedString("test message"));
                    Assert.That(handled, Is.True);

                    handled = simulator.TryHandleMessage(new FixedString("and another one"));
                    Assert.That(handled, Is.True);

                    Assert.That(world.Count, Is.EqualTo(2));

                    Entity firstEntity = new(world, 1);
                    Entity secondEntity = new(world, 2);
                    Assert.That(firstEntity.GetComponent<FixedString>(), Is.EqualTo(new FixedString("test message")));
                    Assert.That(secondEntity.GetComponent<FixedString>(), Is.EqualTo(new FixedString("and another one")));
                }
            }
        }

        [Test, CancelAfter(1000)]
        public void SystemInsideSystem()
        {
            using (World world = new())
            {
                using (Simulator simulator = new(world))
                {
                    simulator.AddSystem<StackedSystem>();

                    Assert.That(simulator.SystemCount, Is.EqualTo(2));

                    simulator.Update(TimeSpan.FromSeconds(0.01f));

                    Assert.That(simulator.SystemCount, Is.EqualTo(2));
                    Assert.That(world.Count, Is.EqualTo(2));

                    Entity firstEntity = new(world, 1);
                    Assert.That(firstEntity.GetComponent<uint>(), Is.EqualTo(4));

                    Entity beforeFinalizeEntity = new(world, 2);
                    Assert.That(beforeFinalizeEntity.GetComponent<bool>(), Is.EqualTo(false));

                    simulator.RemoveSystem<StackedSystem>();

                    Assert.That(simulator.SystemCount, Is.EqualTo(0));
                }

                Entity secondEntity = new(world, 2);
                Assert.That(secondEntity.GetComponent<bool>(), Is.EqualTo(true));
            }
        }

        public readonly struct SimpleSystem : ISystem
        {
            private readonly uint value;

            unsafe readonly InitializeFunction ISystem.Initialize => new(&Initialize);
            unsafe readonly IterateFunction ISystem.Update => new(&Update);
            unsafe readonly FinalizeFunction ISystem.Finalize => new(&Finalize);

            public SimpleSystem()
            {
                value = 4;
            }

            [UnmanagedCallersOnly]
            private static void Initialize(SystemContainer container, World world)
            {
                if (container.World == world)
                {
                    ref SimpleSystem system = ref container.Read<SimpleSystem>();
                    Entity entity = new(container.World);
                    entity.AddComponent(system.value);
                }
            }

            [UnmanagedCallersOnly]
            private static void Update(SystemContainer container, World world, TimeSpan delta)
            {
                if (container.World == world)
                {
                    Entity entity = new(container.World);
                    entity.AddComponent(false);
                }
            }

            [UnmanagedCallersOnly]
            private static void Finalize(SystemContainer container, World world)
            {
                if (container.World == world)
                {
                    Entity entity = new(container.World, 2);
                    entity.SetComponent(true);
                }
            }
        }

        public readonly struct MessageHandlerSystem : ISystem
        {
            unsafe readonly InitializeFunction ISystem.Initialize => new(&Initialize);
            unsafe readonly IterateFunction ISystem.Update => new(&Update);
            unsafe readonly FinalizeFunction ISystem.Finalize => new(&Finalize);

            unsafe readonly uint ISystem.GetMessageHandlers(USpan<MessageHandler> buffer)
            {
                buffer[0] = MessageHandler.Create<FixedString>(new(&ReceiveEvent));
                return 1;
            }

            [UnmanagedCallersOnly]
            private static void Initialize(SystemContainer container, World world)
            {
            }

            [UnmanagedCallersOnly]
            private static void Update(SystemContainer container, World world, TimeSpan delta)
            {
            }

            [UnmanagedCallersOnly]
            private static void Finalize(SystemContainer container, World world)
            {
            }

            [UnmanagedCallersOnly]
            private static void ReceiveEvent(SystemContainer container, World world, Allocation message)
            {
                if (container.World == world)
                {
                    Entity messageEntity = new(container.World);
                    messageEntity.AddComponent(message.Read<FixedString>());
                }
            }
        }

        public readonly struct StackedSystem : ISystem
        {
            unsafe readonly InitializeFunction ISystem.Initialize => new(&Initialize);
            unsafe readonly IterateFunction ISystem.Update => new(&Update);
            unsafe readonly FinalizeFunction ISystem.Finalize => new(&Finalize);

            [UnmanagedCallersOnly]
            private static void Initialize(SystemContainer container, World world)
            {
                if (container.World == world)
                {
                    container.Simulator.AddSystem<SimpleSystem>();
                }
            }

            [UnmanagedCallersOnly]
            private static void Update(SystemContainer container, World world, TimeSpan delta)
            {
            }

            [UnmanagedCallersOnly]
            private static void Finalize(SystemContainer container, World world)
            {
                if (container.World == world)
                {
                    container.Simulator.RemoveSystem<SimpleSystem>();
                }
            }
        }
    }
}