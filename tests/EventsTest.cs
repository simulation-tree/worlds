using Collections.Generic;
using System.Runtime.InteropServices;
using Worlds.Functions;

namespace Worlds.Tests
{
    public unsafe class EventsTest : WorldTests
    {
        [Test]
        public void EntityCreationAndDestruction()
        {
            using List<(uint entity, bool created)> events = new();
            using World world = CreateWorld();
            world.ListenToEntityCreationOrDestruction(new(&OnCreatedOrDestroyed), (ulong)events.Address);

            uint a = world.CreateEntity();
            uint b = world.CreateEntity();

            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].entity, Is.EqualTo(a));
            Assert.That(events[1].entity, Is.EqualTo(b));
            events.Clear();

            world.DestroyEntity(a);
            uint c = world.CreateEntity();

            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].entity, Is.EqualTo(a));
            Assert.That(events[1].entity, Is.EqualTo(c));

            [UnmanagedCallersOnly]
            static void OnCreatedOrDestroyed(EntityCreatedOrDestroyed.Input input)
            {
                List<(uint, bool)> events = new((void*)input.userData);
                if (input.isCreated)
                {
                    events.Add((input.entity, true));
                }
                else
                {
                    events.Add((input.entity, false));
                }
            }
        }

        [Test]
        public void EntityComponentChanges()
        {
            using List<(uint entity, int component, bool added)> events = new();
            using World world = CreateWorld();
            world.ListenToEntityDataChanges(new(&OnComponentChanged), (ulong)events.Address);

            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            uint d = world.CreateEntity();
            world.AddComponent(d, new Another());

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].entity, Is.EqualTo(d));
            Assert.That(events[0].component, Is.EqualTo(world.Schema.GetComponentType<Another>()));
            Assert.That(events[0].added, Is.True);
            events.Clear();

            world.RemoveComponent<Another>(d);

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].entity, Is.EqualTo(d));
            Assert.That(events[0].component, Is.EqualTo(world.Schema.GetComponentType<Another>()));
            Assert.That(events[0].added, Is.False);

            [UnmanagedCallersOnly]
            static void OnComponentChanged(EntityDataChanged.Input input)
            {
                if (input.type.IsComponent)
                {
                    List<(uint, int, bool)> events = new((void*)input.userData);
                    if (input.isPositive)
                    {
                        events.Add((input.entity, input.type.index, true));
                    }
                    else
                    {
                        events.Add((input.entity, input.type.index, false));
                    }
                }
            }
        }
    }
}