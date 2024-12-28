using Collections;
using System;
using System.Diagnostics;

namespace Worlds.Tests
{
    public class QueryTests : WorldTests
    {
        [Test]
        public void FindComponents()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            world.AddComponent(a, new Apple());
            world.AddComponent(b, new Berry());
            world.AddComponent(c, new Apple());
            world.AddComponent(c, new Berry());
            ComponentQuery<Apple> appleQuery = new(world);
            ComponentQuery<Berry> berryQuery = new(world);
            uint foundApples = 0;
            foreach (var r in appleQuery)
            {
                ref Apple apple = ref r.component1;
                Assert.That(apple.bites, Is.EqualTo(0));
                apple.bites += 4;
                apple.bites += (byte)foundApples;
                foundApples++;
            }

            Assert.That(foundApples, Is.EqualTo(2));
            world.RemoveComponent<Apple>(a);
            foundApples = 0;
            foreach (var r in appleQuery)
            {
                ref Apple apple = ref r.component1;
                Assert.That(apple.bites, Is.EqualTo(5).Or.EqualTo(4));
                foundApples++;
            }

            Assert.That(foundApples, Is.EqualTo(1));
            ComponentQuery<Apple, Berry> comboQuery = new(world);
            uint foundCombos = 0;
            foreach (var r in comboQuery)
            {
                Assert.That(r.component1.bites, Is.EqualTo(5).Or.EqualTo(4));
                Assert.That(r.component2.hearts, Is.EqualTo(0));
                foundCombos++;
            }

            Assert.That(foundCombos, Is.EqualTo(1));
        }

        [Test]
        public void QueryWithExclusion()
        {
            using World world = CreateWorld();

            uint a = world.CreateEntity();
            world.AddComponent(a, new Apple());

            uint b = world.CreateEntity();
            world.AddComponent(b, new Berry());

            uint c = world.CreateEntity();
            world.AddComponent(c, new Apple());
            world.AddComponent(c, new Berry());

            uint d = world.CreateEntity();
            world.AddComponent(d, new Apple());

            using List<uint> entities = new();
            ComponentQuery<Apple> appleQuery = new(world, world.Schema.GetComponents<Berry>());
            foreach (var r in appleQuery)
            {
                entities.Add(r.entity);
            }

            Assert.That(entities.Count, Is.EqualTo(2));
            Assert.That(entities[0], Is.EqualTo(a));
            Assert.That(entities[1], Is.EqualTo(d));
        }

        [Test]
        public void FindOnlyEnabledEntities()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            world.AddComponent(a, new Cherry("apple"));
            world.AddComponent(b, new Cherry("pie"));
            world.AddComponent(c, new Cherry("fortune"));
            world.SetEnabled(a, false);
            System.Collections.Generic.List<Cherry> values = [];
            foreach (uint entity in world.GetAllContaining<Cherry>(true))
            {
                values.Add(world.GetComponent<Cherry>(entity));
            }

            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.Contains(new Cherry("pie")), Is.True);
            Assert.That(values.Contains(new Cherry("fortune")), Is.True);
            values.Clear();
            foreach (uint entity in world.GetAllContaining<Cherry>())
            {
                values.Add(world.GetComponent<Cherry>(entity));
            }

            Assert.That(values.Count, Is.EqualTo(3));
            Assert.That(values.Contains(new Cherry("apple")), Is.True);
        }

        [Test]
        public void QueryComponentsAfterDestroyingEntities()
        {
            using World world = CreateWorld();
            uint entity1 = world.CreateEntity();
            uint entity2 = world.CreateEntity();
            Cherry component1 = new("apple");
            Cherry component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            world.DestroyEntity(entity1);
            world.AddComponent(entity2, new Berry(5));
            world.DestroyEntity(entity2);
            System.Collections.Generic.List<(uint, Cherry)> found = new();
            foreach (uint entity in world.GetAllContaining<Cherry>())
            {
                found.Add((entity, world.GetComponent<Cherry>(entity)));
            }

            Assert.That(found.Count, Is.EqualTo(0));
        }

        [Test]
        public void ComponentBuffer()
        {
            using World world = CreateWorld();
            uint entity1 = world.CreateEntity();
            uint entity2 = world.CreateEntity();
            SimpleComponent component1 = new("apple");
            SimpleComponent component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            uint entity3 = world.CreateEntity();
            uint entity4 = world.CreateEntity();
            Another another1 = new(5);
            Another another2 = new(10);
            world.AddComponent(entity3, another1);
            world.AddComponent(entity4, another2);
            uint entity5 = world.CreateEntity();
            world.AddComponent(entity5, component1);
            world.AddComponent(entity5, another2);
            using List<SimpleComponent> buffer = new(4);
            using List<uint> entities = new(4);
            world.Fill(buffer, entities);
            Assert.That(buffer.Count, Is.EqualTo(3));
            var entitiesSpan = entities.AsSpan();
            Assert.That(entities.Contains(entity1), Is.True);
            Assert.That(entities.Contains(entity2), Is.True);
            Assert.That(entities.Contains(entity5), Is.True);
            entities.Clear();
            using List<Another> anotherBuffer = new(4);
            world.Fill(anotherBuffer, entities);
            Assert.That(anotherBuffer.Count, Is.EqualTo(3));
            Assert.That(entities.Contains(entity3), Is.True);
            Assert.That(entities.Contains(entity4), Is.True);
            Assert.That(entities.Contains(entity5), Is.True);
        }

        [Test]
        public void EnumerateQueryResults()
        {
            using World world = CreateWorld();

            uint first = world.CreateEntity();
            world.AddComponent(first, new Apple(232));

            uint a = world.CreateEntity();
            world.AddComponent(a, new Cherry("apple"));

            uint b = world.CreateEntity();
            world.AddComponent(b, new Cherry("pie"));

            uint second = world.CreateEntity();
            world.AddComponent(second, new Apple(123));

            uint c = world.CreateEntity();
            world.AddComponent(c, new Cherry("fortune"));

            ComponentQuery<Cherry> cherryQuery = new(world);
            using List<uint> resultEntities = new();
            using List<Cherry> resultComponents = new();
            foreach (var r in cherryQuery)
            {
                resultEntities.Add(r.entity);
                resultComponents.Add(r.component1);

                r.component1.stones = "cherry";
            }

            Assert.That(resultEntities.Count, Is.EqualTo(3));
            Assert.That(resultComponents.Count, Is.EqualTo(3));
            Assert.That(resultEntities.Contains(a), Is.True);
            Assert.That(resultEntities.Contains(b), Is.True);
            Assert.That(resultEntities.Contains(c), Is.True);
            Assert.That(resultComponents.Contains(new Cherry("apple")), Is.True);
            Assert.That(resultComponents.Contains(new Cherry("pie")), Is.True);
            Assert.That(resultComponents.Contains(new Cherry("fortune")), Is.True);

            foreach (var r in cherryQuery)
            {
                Assert.That(r.component1.stones.ToString(), Is.EqualTo("cherry"));
            }

            ComponentQuery<Apple> appleQuery = new(world);
            using List<uint> appleEntities = new();
            using List<Apple> appleComponents = new();
            foreach (var r in appleQuery)
            {
                appleEntities.Add(r.entity);
                appleComponents.Add(r.component1);
            }

            Assert.That(appleEntities.Count, Is.EqualTo(2));
            Assert.That(appleComponents.Count, Is.EqualTo(2));
            Assert.That(appleEntities.Contains(first), Is.True);
            Assert.That(appleEntities.Contains(second), Is.True);
            Assert.That(appleComponents.Contains(new Apple(232)), Is.True);
            Assert.That(appleComponents.Contains(new Apple(123)), Is.True);
        }

        [Test]
        public void QueryMultipleComponents()
        {
            using World world = CreateWorld();
            uint entity1 = world.CreateEntity();
            uint entity2 = world.CreateEntity();
            Cherry component1 = new("apple");
            Cherry component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            uint entity3 = world.CreateEntity();
            uint entity4 = world.CreateEntity();
            Berry another1 = new(5);
            Berry another2 = new(10);
            world.AddComponent(entity3, another1);
            world.AddComponent(entity4, another2);
            uint entity5 = world.CreateEntity();
            world.AddComponent(entity5, component1);
            world.AddComponent(entity5, another2);
            using List<uint> simpleComponents = new(world.GetAllContaining<Cherry>());
            using List<uint> anotherComponents = new(world.GetAllContaining<Berry>());
            Assert.That(simpleComponents.Count, Is.EqualTo(3));
            Assert.That(anotherComponents.Count, Is.EqualTo(3));
            Assert.That(simpleComponents.Contains(entity1), Is.True);
            Assert.That(simpleComponents.Contains(entity2), Is.True);
            Assert.That(simpleComponents.Contains(entity5), Is.True);
            Assert.That(anotherComponents.Contains(entity3), Is.True);
            Assert.That(anotherComponents.Contains(entity4), Is.True);
            Assert.That(anotherComponents.Contains(entity5), Is.True);
        }

        [Test]
        public void BenchmarkMethods()
        {
            using World world = CreateWorld();
            BitSet componentTypes = world.Schema.GetComponents<Apple, Berry, Cherry>();
            BitSet otherComponentTypes = world.Schema.GetComponents<Apple, Berry>();
            ComponentType appleType = world.Schema.GetComponent<Apple>();
            ComponentType berryType = world.Schema.GetComponent<Berry>();
            ComponentType cherryType = world.Schema.GetComponent<Cherry>();
            uint sampleCount = 90000;
            uint count = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (uint i = 0; i < sampleCount; i++)
            {
                if ((i % 9 == 0) || (i % 2 == 0))
                {
                    if (i % 3 == 0)
                    {
                        Definition definition = new(componentTypes, default);
                        world.CreateEntity(definition);
                    }
                    else
                    {
                        Definition definition = new(otherComponentTypes, default);
                        world.CreateEntity(definition);
                    }

                    count++;
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Creating {count} entities took {stopwatch.ElapsedTicks / 10000.0}ms");

            using List<(uint, Apple, Berry, Cherry)> results = new();

            //benchmark query
            ComponentQuery<Apple, Berry, Cherry> query = new(world);
            stopwatch.Restart();
            {
                foreach (var r in query)
                {
                    results.Add((r.entity, r.component1, r.component2, r.component3));
                }
            }
            stopwatch.Stop();
            Console.WriteLine($"ComponentQuery took {stopwatch.ElapsedTicks / 10000.0}ms");

            //benchmarking manually iterating
            results.Clear();
            stopwatch.Restart();
            {
                Dictionary<BitSet, ComponentChunk> chunks = world.ComponentChunks;
                foreach (BitSet key in chunks.Keys)
                {
                    if ((key & componentTypes) == componentTypes)
                    {
                        ComponentChunk chunk = chunks[key];
                        List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            ref Apple apple = ref chunk.GetComponent<Apple>(e, appleType);
                            ref Berry berry = ref chunk.GetComponent<Berry>(e, berryType);
                            ref Cherry cherry = ref chunk.GetComponent<Cherry>(e, cherryType);
                            results.Add((entity, apple, berry, cherry));
                        }
                    }
                }
            }
            stopwatch.Stop();
            Console.WriteLine($"Manual iteration took {stopwatch.ElapsedTicks / 10000.0}ms");
        }
    }
}