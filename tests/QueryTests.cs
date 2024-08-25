using System;
using System.Collections.Generic;
using System.Linq;
using Unmanaged;
using static Simulation.Tests.WorldTests;
using Unmanaged.Collections;

namespace Simulation.Tests
{
    public class QueryTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAny();
        }

        [Test]
        public void FindComponents()
        {
            using World world = new();
            eint a = world.CreateEntity();
            eint b = world.CreateEntity();
            eint c = world.CreateEntity();
            world.AddComponent(a, new Apple());
            world.AddComponent(b, new Berry());
            world.AddComponent(c, new Apple());
            world.AddComponent(c, new Berry());
            using Query<Apple> appleQuery = new(world);
            using Query<Berry> berryQuery = new(world);
            appleQuery.Update();
            Assert.That(appleQuery.Count, Is.EqualTo(2));
            uint appleIndex = 0;
            for (uint i = 0; i < appleQuery.Count; i++)
            {
                Query<Apple>.Result result = appleQuery[i];
                ref Apple apple = ref result.Component1;
                Assert.That(apple.bites, Is.EqualTo(0));

                apple.bites += 4;
                apple.bites += (byte)appleIndex;
                appleIndex++;
            }

            world.RemoveComponent<Apple>(a);
            appleQuery.Update();
            Assert.That(appleQuery.Count, Is.EqualTo(1));
            for (uint i = 0; i < appleQuery.Count; i++)
            {
                Query<Apple>.Result result = appleQuery[i];
                ref Apple apple = ref result.Component1;
                Assert.That(apple.bites, Is.EqualTo(5));
            }

            using Query<Apple, Berry> comboQuery = new(world);
            comboQuery.Update();
            Assert.That(comboQuery.Count, Is.EqualTo(1));
            Query<Apple, Berry>.Result firstResult = comboQuery[0];
            Assert.That(firstResult.Component1.bites, Is.EqualTo(5));
            Assert.That(firstResult.Component2.hearts, Is.EqualTo(0));

            using Query<Berry> onlyBerries = new(world);
            onlyBerries.Update(Query.Option.ExactComponentTypes);
            Assert.That(onlyBerries.Count, Is.EqualTo(1));
        }

        [Test]
        public void FindOnlyEnabledEntities()
        {
            using World world = new();
            eint a = world.CreateEntity();
            eint b = world.CreateEntity();
            eint c = world.CreateEntity();
            world.AddComponent(a, new Cherry("apple"));
            world.AddComponent(b, new Cherry("pie"));
            world.AddComponent(c, new Cherry("fortune"));
            world.SetEnabled(a, false);
            List<Cherry> values = [];
            foreach (eint entity in world.GetAll<Cherry>(Query.Option.OnlyEnabledEntities))
            {
                values.Add(world.GetComponent<Cherry>(entity));
            }

            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.Contains(new Cherry("pie")), Is.True);
            Assert.That(values.Contains(new Cherry("fortune")), Is.True);
            values.Clear();
            foreach (eint entity in world.GetAll<Cherry>(Query.Option.IncludeDisabledEntities))
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
            eint entity1 = world.CreateEntity();
            eint entity2 = world.CreateEntity();
            Cherry component1 = new("apple");
            Cherry component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            world.DestroyEntity(entity1);
            world.AddComponent(entity2, new Berry(5));
            world.DestroyEntity(entity2);
            List<(eint, Cherry)> found = new();
            foreach (eint entity in world.GetAll<Cherry>())
            {
                found.Add((entity, world.GetComponent<Cherry>(entity)));
            }

            Assert.That(found.Count, Is.EqualTo(0));
        }

        [Test]
        public void ComponentBuffer()
        {
            using World world = new();
            eint entity1 = world.CreateEntity();
            eint entity2 = world.CreateEntity();
            SimpleComponent component1 = new("apple");
            SimpleComponent component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            eint entity3 = world.CreateEntity();
            eint entity4 = world.CreateEntity();
            Another another1 = new(5);
            Another another2 = new(10);
            world.AddComponent(entity3, another1);
            world.AddComponent(entity4, another2);
            eint entity5 = world.CreateEntity();
            world.AddComponent(entity5, component1);
            world.AddComponent(entity5, another2);
            using UnmanagedList<SimpleComponent> buffer = UnmanagedList<SimpleComponent>.Create();
            using UnmanagedList<eint> entities = UnmanagedList<eint>.Create();
            world.Fill(buffer, entities);
            Assert.That(buffer.Count, Is.EqualTo(3));
            var entitiesSpan = entities.AsSpan();
            Assert.That(entities.Contains(entity1), Is.True);
            Assert.That(entities.Contains(entity2), Is.True);
            Assert.That(entities.Contains(entity5), Is.True);
            entities.Clear();
            using UnmanagedList<Another> anotherBuffer = UnmanagedList<Another>.Create();
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
            eint a = world.CreateEntity();
            eint b = world.CreateEntity();
            eint c = world.CreateEntity();
            world.AddComponent(a, new Cherry("apple"));
            world.AddComponent(b, new Cherry("pie"));
            world.AddComponent(c, new Cherry("fortune"));
            using Query<Cherry> query = new(world);
            query.Update();
            Assert.That(query.Count, Is.EqualTo(3));
            uint count = 0;
            foreach (Query<Cherry>.Result result in query)
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
            eint entity1 = world.CreateEntity();
            eint entity2 = world.CreateEntity();
            Cherry component1 = new("apple");
            Cherry component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            eint entity3 = world.CreateEntity();
            eint entity4 = world.CreateEntity();
            Berry another1 = new(5);
            Berry another2 = new(10);
            world.AddComponent(entity3, another1);
            world.AddComponent(entity4, another2);
            eint entity5 = world.CreateEntity();
            world.AddComponent(entity5, component1);
            world.AddComponent(entity5, another2);
            List<eint> simpleComponents = world.GetAll<Cherry>().ToList();
            List<eint> anotherComponents = world.GetAll<Berry>().ToList();
            Assert.That(simpleComponents.Count, Is.EqualTo(3));
            Assert.That(anotherComponents.Count, Is.EqualTo(3));
            Assert.That(simpleComponents.Contains(entity1), Is.True);
            Assert.That(simpleComponents.Contains(entity2), Is.True);
            Assert.That(simpleComponents.Contains(entity5), Is.True);
            Assert.That(anotherComponents.Contains(entity3), Is.True);
            Assert.That(anotherComponents.Contains(entity4), Is.True);
            Assert.That(anotherComponents.Contains(entity5), Is.True);
        }

        /*
        //query wins long term
        //commented out because creation itself takes a whopping amount of time...
        [Test]
        public void BenchmarkMethods()
        {
            using World world = new();
            uint sampleCount = 50000;
            uint count = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (uint i = 0; i < sampleCount; i++)
            {
                eint entity = world.CreateEntity();
                if ((i % 9 == 0) || (i % 2 == 0))
                {
                    world.AddComponent(entity, new Apple());
                    world.AddComponent(entity, new Berry((byte)(i % byte.MaxValue)));
                    if (i % 3 == 0)
                    {
                        FixedString name = default;
                        name.Append(i);
                        world.AddComponent(entity, new Cherry(name));
                        count++;
                    }
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Creating {count} entities took {stopwatch.ElapsedMilliseconds}ms");

            //benchmark query
            using Query<Apple, Berry, Cherry> query = new(world);
            List<(eint, Apple, Berry, Cherry)> results = [];
            stopwatch.Restart();
            query.Fill();
            foreach (Query<Apple, Berry, Cherry>.Result r in query)
            {
                results.Add((r.entity, r.Component1, r.Component2, r.Component3));
            }
            stopwatch.Stop();
            Console.WriteLine($"Querying {count} entities took {stopwatch.ElapsedMilliseconds}ms");

            //benchmark ForEach
            results.Clear();
            stopwatch.Restart();
            world.ForEach((in eint entity, ref Apple apple, ref Berry berry, ref Cherry cherry) =>
            {
                results.Add((entity, apple, berry, cherry));
            });

            stopwatch.Stop();
            Console.WriteLine($"ForEach {count} entities took {stopwatch.ElapsedMilliseconds}ms");

            //benchmarking manually iterating
            results.Clear();
            UnmanagedDictionary<uint, ComponentChunk> chunks = world.ComponentChunks;
            Span<RuntimeType> typesSpan = [RuntimeType.Get<Apple>(), RuntimeType.Get<Berry>(), RuntimeType.Get<Cherry>()];
            stopwatch.Restart();
            for (int i = 0; i < chunks.Count; i++)
            {
                uint key = chunks.Keys[i];
                ComponentChunk chunk = chunks[key];
                if (chunk.ContainsTypes(typesSpan))
                {
                    UnmanagedList<eint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        eint entity = entities[e];
                        if (world.IsEnabled(entity))
                        {
                            ref Apple apple = ref chunk.GetComponentRef<Apple>(e);
                            ref Berry berry = ref chunk.GetComponentRef<Berry>(e);
                            ref Cherry cherry = ref chunk.GetComponentRef<Cherry>(e);
                            results.Add((entity, apple, berry, cherry));
                        }
                    }
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Manual iteration {count} entities took {stopwatch.ElapsedMilliseconds}ms");
        }
        */

        public struct Apple
        {
            public byte bites;
        }

        public struct Berry
        {
            public byte hearts;

            public Berry(byte hearts)
            {
                this.hearts = hearts;
            }
        }

        public struct Cherry
        {
            public FixedString stones;

            public Cherry(ReadOnlySpan<char> stones)
            {
                this.stones = new(stones);
            }

            public Cherry(FixedString stones)
            {
                this.stones = stones;
            }
        }
    }
}