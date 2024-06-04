﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public class QueryTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAnyAllocation();
        }

        [Test]
        public void FindComponents()
        {
            using World world = new();
            EntityID a = world.CreateEntity();
            EntityID b = world.CreateEntity();
            EntityID c = world.CreateEntity();
            world.AddComponent(a, new Apple());
            world.AddComponent(b, new Berry());
            world.AddComponent(c, new Apple());
            world.AddComponent(c, new Berry());
            using Query<Apple> appleQuery = new(world);
            using Query<Berry> berryQuery = new(world);
            appleQuery.Fill();
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
            appleQuery.Fill();
            Assert.That(appleQuery.Count, Is.EqualTo(1));
            for (uint i = 0; i < appleQuery.Count; i++)
            {
                Query<Apple>.Result result = appleQuery[i];
                ref Apple apple = ref result.Component1;
                Assert.That(apple.bites, Is.EqualTo(5));
            }

            using Query<Apple, Berry> comboQuery = new(world);
            comboQuery.Fill();
            Assert.That(comboQuery.Count, Is.EqualTo(1));
            Query<Apple, Berry>.Result firstResult = comboQuery[0];
            Assert.That(firstResult.Component1.bites, Is.EqualTo(5));
            Assert.That(firstResult.Component2.hearts, Is.EqualTo(0));

            using Query<Berry> onlyBerries = new(world, Query.Option.ExactComponentTypes);
            onlyBerries.Fill();
            Assert.That(onlyBerries.Count, Is.EqualTo(1));
        }

        [Test]
        public void FindOnlyEnabledEntities()
        {
            using World world = new();
            EntityID a = world.CreateEntity();
            EntityID b = world.CreateEntity();
            EntityID c = world.CreateEntity();
            world.AddComponent(a, new Cherry("apple"));
            world.AddComponent(b, new Cherry("pie"));
            world.AddComponent(c, new Cherry("fortune"));
            world.SetEnabledState(a, false);
            List<Cherry> values = [];
            foreach (EntityID entity in world.GetAll<Cherry>())
            {
                values.Add(world.GetComponent<Cherry>(entity));
            }

            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.Contains(new Cherry("pie")), Is.True);
            Assert.That(values.Contains(new Cherry("fortune")), Is.True);
            values.Clear();
            foreach (EntityID entity in world.GetAll<Cherry>(Query.Option.IncludeDisabledEntities))
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
            EntityID entity1 = world.CreateEntity();
            EntityID entity2 = world.CreateEntity();
            Cherry component1 = new("apple");
            Cherry component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            world.DestroyEntity(entity1);
            world.AddComponent(entity2, new Berry(5));
            world.DestroyEntity(entity2);
            List<(EntityID, Cherry)> found = new();
            foreach (EntityID entity in world.GetAll<Cherry>())
            {
                found.Add((entity, world.GetComponent<Cherry>(entity)));
            }

            Assert.That(found.Count, Is.EqualTo(0));
        }

        [Test]
        public void EnumerateQuery()
        {
            using World world = new();
            EntityID a = world.CreateEntity();
            EntityID b = world.CreateEntity();
            EntityID c = world.CreateEntity();
            world.AddComponent(a, new Cherry("apple"));
            world.AddComponent(b, new Cherry("pie"));
            world.AddComponent(c, new Cherry("fortune"));
            using Query<Cherry> query = new(world);
            query.Fill();
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
            EntityID entity1 = world.CreateEntity();
            EntityID entity2 = world.CreateEntity();
            Cherry component1 = new("apple");
            Cherry component2 = new("banana");
            world.AddComponent(entity1, component1);
            world.AddComponent(entity2, component2);
            EntityID entity3 = world.CreateEntity();
            EntityID entity4 = world.CreateEntity();
            Berry another1 = new(5);
            Berry another2 = new(10);
            world.AddComponent(entity3, another1);
            world.AddComponent(entity4, another2);
            EntityID entity5 = world.CreateEntity();
            world.AddComponent(entity5, component1);
            world.AddComponent(entity5, another2);
            List<EntityID> simpleComponents = world.GetAll<Cherry>().ToList();
            List<EntityID> anotherComponents = world.GetAll<Berry>().ToList();
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
            using World world = new();
            using RandomGenerator rng = new(1337);
            uint sampleCount = 30000;
            uint count = 0;
            for (uint i = 0; i < sampleCount; i++)
            {
                EntityID entity = world.CreateEntity();
                if (rng.NextBool())
                {
                    world.AddComponent(entity, new Apple());

                    if (rng.NextBool())
                    {
                        world.AddComponent(entity, new Berry((byte)(i % byte.MaxValue)));
                        if (i % 4 == 0)
                        {
                            FixedString name = default;
                            name.Append(i);
                            world.AddComponent(entity, new Cherry(name));
                            count++;
                        }
                    }
                }
            }

            //benchmark query
            using Query<Apple, Berry, Cherry> query = new(world);
            query.Fill();
            List<(EntityID, Apple, Berry, Cherry)> results = [];
            Stopwatch stopwatch = Stopwatch.StartNew();
            foreach (Query<Apple, Berry, Cherry>.Result r in query)
            {
                results.Add((r.entity, r.Component1, r.Component2, r.Component3));
            }
            stopwatch.Stop();
            Console.WriteLine($"Querying {count} entities took {stopwatch.ElapsedMilliseconds}ms");

            //benchmark ForEach
            results.Clear();
            stopwatch.Restart();
            world.ForEach((in EntityID entity, ref Apple apple, ref Berry berry, ref Cherry cherry) =>
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
                    UnmanagedList<EntityID> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        EntityID entity = entities[e];
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

            public Cherry(FixedString stones)
            {
                this.stones = stones;
            }
        }
    }
}