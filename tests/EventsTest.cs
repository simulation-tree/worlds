﻿using Collections;
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
            static void OnCreatedOrDestroyed(World world, uint entity, ChangeType type, ulong userData)
            {
                List<(uint, bool)> events = new((void*)userData);
                if (type == ChangeType.Added)
                {
                    events.Add((entity, true));
                }
                else
                {
                    events.Add((entity, false));
                }
            }
        }

        [Test]
        public void EntityComponentChanges()
        {
            using List<(uint entity, ComponentType component, bool added)> events = new();
            using World world = CreateWorld();
            world.ListenToEntityDataChanges(new(&OnComponentChanged), (ulong)events.Address);

            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            uint d = world.CreateEntity();
            world.AddComponent(d, new Another());

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].entity, Is.EqualTo(d));
            Assert.That(events[0].component, Is.EqualTo(world.Schema.GetComponent<Another>()));
            Assert.That(events[0].added, Is.True);
            events.Clear();

            world.RemoveComponent<Another>(d);

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].entity, Is.EqualTo(d));
            Assert.That(events[0].component, Is.EqualTo(world.Schema.GetComponent<Another>()));
            Assert.That(events[0].added, Is.False);

            [UnmanagedCallersOnly]
            static void OnComponentChanged(World world, uint entity, byte type, DataType dataType, ChangeType changeType, ulong userData)
            {
                if (dataType == DataType.Component)
                {
                    List<(uint, ComponentType, bool)> events = new((void*)userData);
                    if (changeType == ChangeType.Added)
                    {
                        events.Add((entity, new(type), true));
                    }
                    else
                    {
                        events.Add((entity, new(type), false));
                    }
                }
            }
        }
    }
}