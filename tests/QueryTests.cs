using Collections;
using System;
using System.Diagnostics;
using System.Linq;
using Unmanaged;

namespace Worlds.Tests
{
    public class QueryTests : WorldTests
    {
        [Test]
        public void FindComponents()
        {
            using World world = new();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            world.AddComponent(a, new Apple());
            world.AddComponent(b, new Berry());
            world.AddComponent(c, new Apple());
            world.AddComponent(c, new Berry());
            using ComponentQuery<Apple> appleQuery = new();
            using ComponentQuery<Berry> berryQuery = new();
            appleQuery.Update(world);
            Assert.That(appleQuery.Count, Is.EqualTo(2));
            uint appleIndex = 0;
            for (uint i = 0; i < appleQuery.Count; i++)
            {
                var result = appleQuery[i];
                ref Apple apple = ref result.Component1;
                Assert.That(apple.bites, Is.EqualTo(0));

                apple.bites += 4;
                apple.bites += (byte)appleIndex;
                appleIndex++;
            }

            world.RemoveComponent<Apple>(a);
            appleQuery.Update(world);
            Assert.That(appleQuery.Count, Is.EqualTo(1));
            for (uint i = 0; i < appleQuery.Count; i++)
            {
                var result = appleQuery[i];
                ref Apple apple = ref result.Component1;
                Assert.That(apple.bites, Is.EqualTo(5));
            }

            using ComponentQuery<Apple, Berry> comboQuery = new();
            comboQuery.Update(world);
            Assert.That(comboQuery.Count, Is.EqualTo(1));
            ComponentQuery<Apple, Berry>.Result firstResult = comboQuery[0];
            Assert.That(firstResult.Component1.bites, Is.EqualTo(5));
            Assert.That(firstResult.Component2.hearts, Is.EqualTo(0));
        }

        [Test]
        public void FindOnlyEnabledEntities()
        {
            using World world = new();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            world.AddComponent(a, new Cherry("apple"));
            world.AddComponent(b, new Cherry("pie"));
            world.AddComponent(c, new Cherry("fortune"));
            world.SetEnabled(a, false);
            System.Collections.Generic.List<Cherry> values = [];
            foreach (uint entity in world.GetAll<Cherry>(true))
            {
                values.Add(world.GetComponent<Cherry>(entity));
            }

            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.Contains(new Cherry("pie")), Is.True);
            Assert.That(values.Contains(new Cherry("fortune")), Is.True);
            values.Clear();
            foreach (uint entity in world.GetAll<Cherry>())
            {
                values.Add(world.GetComponent<Cherry>(entity));
            }

            Assert.That(values.Count, Is.EqualTo(3));
            Assert.That(values.Contains(new Cherry("apple")), Is.True);
        }

        [Test]
        public void QueryComponentsAfterDestroyingEntities()
        {
            using World world = new();
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
            foreach (uint entity in world.GetAll<Cherry>())
            {
                found.Add((entity, world.GetComponent<Cherry>(entity)));
            }

            Assert.That(found.Count, Is.EqualTo(0));
        }

        [Test]
        public void ComponentBuffer()
        {
            using World world = new();
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
        public void EnumerateQuery()
        {
            using World world = new();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            world.AddComponent(a, new Cherry("apple"));
            world.AddComponent(b, new Cherry("pie"));
            world.AddComponent(c, new Cherry("fortune"));
            using ComponentQuery<Cherry> query = new();
            query.Update(world);
            Assert.That(query.Count, Is.EqualTo(3));
            uint count = 0;
            foreach (var result in query)
            {
                ref Cherry cherry = ref result.Component1;
                cherry.stones = "cherry";
                count++;
            }

            Assert.That(count, Is.EqualTo(3));
            Assert.That(world.GetComponent<Cherry>(a).stones.ToString(), Is.EqualTo("cherry"));
            Assert.That(world.GetComponent<Cherry>(b).stones.ToString(), Is.EqualTo("cherry"));
            Assert.That(world.GetComponent<Cherry>(c).stones.ToString(), Is.EqualTo("cherry"));
        }

        [Test]
        public void QueryMultipleComponents()
        {
            using World world = new();
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
            System.Collections.Generic.List<uint> simpleComponents = world.GetAll<Cherry>().ToList();
            System.Collections.Generic.List<uint> anotherComponents = world.GetAll<Berry>().ToList();
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
        public void IndexOfResult()
        {
            using World world = new();

            Definition definition = new([ComponentType.Get<Apple>(), ComponentType.Get<Berry>(), ComponentType.Get<Cherry>()], []);
            uint entity1 = world.CreateEntity(definition);
            uint entity2 = world.CreateEntity(definition);
            uint entity3 = world.CreateEntity(definition);
            uint entity4 = world.CreateEntity();

            using ComponentQuery<Apple, Berry, Cherry> query = new();
            query.Update(world);

            Assert.That(query.Count, Is.EqualTo(3));
            Assert.That(query.TryIndexOf(1, out uint index), Is.True);
            Assert.That(index, Is.EqualTo(0));
            Assert.That(query.TryIndexOf(2, out index), Is.True);
            Assert.That(index, Is.EqualTo(1));
            Assert.That(query.TryIndexOf(3, out index), Is.True);
            Assert.That(index, Is.EqualTo(2));
            Assert.That(query.TryIndexOf(4, out index), Is.False);
        }

        [Test]
        public void BenchmarkMethods()
        {
            using World world = new();
            uint sampleCount = 512;
            uint count = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (uint i = 0; i < sampleCount; i++)
            {
                if ((i % 9 == 0) || (i % 2 == 0))
                {
                    if (i % 3 == 0)
                    {
                        Definition definition = new([ComponentType.Get<Apple>(), ComponentType.Get<Berry>(), ComponentType.Get<Cherry>()], []);
                        world.CreateEntity(definition);
                    }
                    else
                    {
                        Definition definition = new([ComponentType.Get<Apple>(), ComponentType.Get<Berry>()], []);
                        world.CreateEntity(definition);
                    }

                    count++;
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Creating {count} entities took {stopwatch.ElapsedMilliseconds}ms");

            System.Collections.Generic.List<(uint, Apple, Berry, Cherry)> results = [];

            //benchmark query
            using ComponentQuery<Apple, Berry, Cherry> query = new();
            stopwatch.Restart();
            query.Update(world);
            stopwatch.Stop();
            Console.WriteLine($"ComponentQuery took {stopwatch.ElapsedTicks}t");
            stopwatch.Restart();
            foreach (var r in query)
            {
                results.Add((r.entity, r.Component1, r.Component2, r.Component3));
            }

            stopwatch.Stop();
            Console.WriteLine($"    Iterating took {stopwatch.ElapsedTicks}t");

            //benchmark definition query
            using DefinitionQuery defQuery = new(new([ComponentType.Get<Apple>(), ComponentType.Get<Berry>(), ComponentType.Get<Cherry>()], []));
            results.Clear();
            stopwatch.Restart();
            defQuery.Update(world);
            stopwatch.Stop();
            Console.WriteLine($"DefinitionQuery took {stopwatch.ElapsedTicks}t");
            stopwatch.Restart();
            foreach (var r in defQuery)
            {
                //results.Add((r.value, r.Component1, r.Component2, r.Component3));
            }

            stopwatch.Stop();
            Console.WriteLine($"    Iterating took {stopwatch.ElapsedTicks}t");

            //benchmark ForEach
            results.Clear();
            stopwatch.Restart();
            world.ForEach((in uint entity, ref Apple apple, ref Berry berry, ref Cherry cherry) =>
            {
                results.Add((entity, apple, berry, cherry));
            });

            stopwatch.Stop();
            Console.WriteLine($"ForEach took {stopwatch.ElapsedTicks}t");

            //benchmarking manually iterating
            results.Clear();
            Dictionary<int, ComponentChunk> chunks = world.ComponentChunks;
            USpan<ComponentType> typesSpan = [ComponentType.Get<Apple>(), ComponentType.Get<Berry>(), ComponentType.Get<Cherry>()];
            stopwatch.Restart();
            for (uint i = 0; i < chunks.Count; i++)
            {
                int key = chunks.Keys[i];
                ComponentChunk chunk = chunks[key];
                if (chunk.ContainsAllTypes(typesSpan))
                {
                    List<uint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        uint entity = entities[e];
                        ref Apple apple = ref chunk.GetComponentRef<Apple>(e);
                        ref Berry berry = ref chunk.GetComponentRef<Berry>(e);
                        ref Cherry cherry = ref chunk.GetComponentRef<Cherry>(e);
                        results.Add((entity, apple, berry, cherry));
                    }
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Manual iteration took {stopwatch.ElapsedTicks}t");
        }
    }
}