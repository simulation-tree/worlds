using Collections.Generic;
using System;

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
            int foundApples = 0;
            foreach (var r in appleQuery)
            {
                ref Apple apple = ref r.component1;
                Assert.That(apple.bites, Is.EqualTo(0));
                apple.bites += 4;
                apple.bites += (byte)foundApples;
                foundApples++;
            }

            Assert.That(foundApples, Is.EqualTo(2));
            world.RemoveComponentType<Apple>(a);
            foundApples = 0;
            foreach (var r in appleQuery)
            {
                ref Apple apple = ref r.component1;
                Assert.That(apple.bites, Is.EqualTo(5).Or.EqualTo(4));
                foundApples++;
            }

            Assert.That(foundApples, Is.EqualTo(1));
            ComponentQuery<Apple, Berry> comboQuery = new(world);
            int foundCombos = 0;
            foreach (var r in comboQuery)
            {
                Assert.That(r.component1.bites, Is.EqualTo(5).Or.EqualTo(4));
                Assert.That(r.component2.hearts, Is.EqualTo(0));
                foundCombos++;
            }

            Assert.That(foundCombos, Is.EqualTo(1));
        }

        [Test]
        public void FindComponentsWithTag()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity(new Apple(4));
            uint b = world.CreateEntity(new Apple(5));
            uint c = world.CreateEntity(new Apple(6));
            world.AddTag<IsThing>(b);

            ComponentQuery<Apple> appleThingQuery = new(world);
            appleThingQuery.RequireTag<IsThing>();

            using List<Apple> apples = new();
            foreach (var r in appleThingQuery)
            {
                apples.Add(r.component1);
            }

            Assert.That(apples.Count, Is.EqualTo(1));
            Assert.That(apples[0].bites, Is.EqualTo(5));

            ComponentQuery<Apple> appleNoThingQuery = new(world);
            appleNoThingQuery.ExcludeTag<IsThing>();

            apples.Clear();
            foreach (var r in appleNoThingQuery)
            {
                apples.Add(r.component1);
            }

            Assert.That(apples.Count, Is.EqualTo(2));
            Assert.That(apples[0].bites, Is.EqualTo(4));
            Assert.That(apples[1].bites, Is.EqualTo(6));
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
            ComponentQuery<Apple> appleQuery = new(world);
            appleQuery.ExcludeComponent<Berry>();
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

            using List<Cherry> results = new();

            //check with this method
            foreach (uint entity in world.GetAllContaining<Cherry>())
            {
                results.Add(world.GetComponent<Cherry>(entity));
            }

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.Contains(new Cherry("pie")), Is.True);
            Assert.That(results.Contains(new Cherry("fortune")), Is.True);

            results.Clear();

            //check with a query
            ComponentQuery<Cherry> query = new(world);
            query.ExcludeDisabled(true);
            foreach (var r in query)
            {
                results.Add(r.component1);
            }

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.Contains(new Cherry("pie")), Is.True);
            Assert.That(results.Contains(new Cherry("fortune")), Is.True);

            results.Clear();

            //check if all get included with the method
            foreach (uint entity in world.GetAllContaining<Cherry>(false))
            {
                results.Add(world.GetComponent<Cherry>(entity));
            }

            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results.Contains(new Cherry("apple")), Is.True);

            results.Clear();

            //check if all get included with a query
            query.ExcludeDisabled(false);
            foreach (var r in query)
            {
                results.Add(r.component1);
            }

            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results.Contains(new Cherry("apple")), Is.True);
        }

        [Test]
        public void QueryDescendantsOfDisabledEntity()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            uint parent = world.CreateEntity();
            world.AddComponent(parent, new Apple(1));
            world.AddComponent(a, new Apple(2));
            world.AddComponent(b, new Apple(3));
            world.AddComponent(c, new Apple(4));
            world.SetParent(a, parent);
            world.SetParent(b, a);
            world.SetEnabled(parent, false);
            uint d = world.CreateEntity();
            world.AddComponent(d, new Apple(5));
            world.SetParent(d, b);

            Assert.That(world.IsEnabled(parent), Is.EqualTo(false));
            Assert.That(world.IsEnabled(a), Is.EqualTo(false));
            Assert.That(world.IsLocallyEnabled(a), Is.EqualTo(true));
            Assert.That(world.IsEnabled(b), Is.EqualTo(false));
            Assert.That(world.IsLocallyEnabled(b), Is.EqualTo(true));
            Assert.That(world.IsEnabled(c), Is.EqualTo(true));
            Assert.That(world.IsEnabled(d), Is.EqualTo(false));
            Assert.That(world.IsLocallyEnabled(d), Is.EqualTo(true));

            using List<Apple> results = new();
            ComponentQuery<Apple> query = new(world);
            foreach (var r in query)
            {
                results.Add(r.component1);
            }

            Assert.That(results.Count, Is.EqualTo(5));
            Assert.That(results.Contains(new Apple(1)), Is.True);
            Assert.That(results.Contains(new Apple(2)), Is.True);
            Assert.That(results.Contains(new Apple(3)), Is.True);
            Assert.That(results.Contains(new Apple(4)), Is.True);
            Assert.That(results.Contains(new Apple(5)), Is.True);

            results.Clear();
            query.ExcludeDisabled(true);
            foreach (var r in query)
            {
                results.Add(r.component1);
            }

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.Contains(new Apple(4)), Is.True);
        }

        [Test]
        public void QueryAgainstRedisabledDescendants()
        {
            using World world = CreateWorld();
            uint parent = world.CreateEntity();
            uint child = world.CreateEntity();
            uint grandChild = world.CreateEntity();
            world.SetParent(child, parent);
            world.SetParent(grandChild, child);

            world.AddComponent(parent, new Another(1));
            world.AddComponent(child, new Another(2));
            world.AddComponent(grandChild, new Another(3));
            world.SetEnabled(parent, false);
            world.SetEnabled(child, false);
            world.SetEnabled(grandChild, false);

            Assert.That(world.IsEnabled(parent), Is.False);
            Assert.That(world.IsEnabled(child), Is.False);
            Assert.That(world.IsEnabled(grandChild), Is.False);

            using List<Another> results = new();
            ComponentQuery<Another> query = new(world);
            foreach (var r in query)
            {
                results.Add(r.component1);
            }

            Assert.That(results.Count, Is.EqualTo(3));

            query.ExcludeDisabled(true);
            results.Clear();
            foreach (var r in query)
            {
                results.Add(r.component1);
            }

            Assert.That(results.Count, Is.EqualTo(0));

            world.SetEnabled(parent, true);

            results.Clear();
            foreach (var r in query)
            {
                results.Add(r.component1);
            }

            Assert.That(results.Count, Is.EqualTo(3));

            world.SetEnabled(parent, false);
            results.Clear();
            foreach (var r in query)
            {
                results.Add(r.component1);
            }

            Assert.That(results.Count, Is.EqualTo(0));
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

            using List<(uint, Cherry)> results = new();
            foreach (uint entity in world.GetAllContaining<Cherry>())
            {
                results.Add((entity, world.GetComponent<Cherry>(entity)));
            }

            Assert.That(results.Count, Is.EqualTo(0));
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

            using List<SimpleComponent> simpleComponents = new(4);
            using List<uint> entities = new(4);
            foreach (uint entity in world.GetAllContaining<SimpleComponent>())
            {
                simpleComponents.Add(world.GetComponent<SimpleComponent>(entity));
                entities.Add(entity);
            }

            Assert.That(simpleComponents.Count, Is.EqualTo(3));
            Assert.That(simpleComponents.Count, Is.EqualTo(entities.Count));
            Assert.That(entities.Contains(entity1), Is.True);
            Assert.That(entities.Contains(entity2), Is.True);
            Assert.That(entities.Contains(entity5), Is.True);
            entities.Clear();

            using List<Another> anothers = new(4);
            foreach (uint entity in world.GetAllContaining<Another>())
            {
                anothers.Add(world.GetComponent<Another>(entity));
                entities.Add(entity);
            }

            Assert.That(anothers.Count, Is.EqualTo(entities.Count));
            Assert.That(anothers.Count, Is.EqualTo(3));
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
        public void FindEntityWithTag()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            uint d = world.CreateEntity();
            world.AddComponent(a, new Apple());
            world.AddComponent(b, new Another());
            world.AddTag<IsThing>(b);
            world.AddTag<IsThing>(d);

            using List<uint> entities = new();
            Query query = new(world);
            query.RequireTag<IsThing>();
            foreach (uint entity in query)
            {
                entities.Add(entity);
            }

            Assert.That(entities.Count, Is.EqualTo(2));
            Assert.That(entities.Contains(b), Is.True);
            Assert.That(entities.Contains(d), Is.True);
        }

#if DEBUG
        [Test]
        public void ThrowIfVersionChanges()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            uint c = world.CreateEntity();
            world.AddComponent(a, new Apple());
            world.AddComponent(b, new Apple());
            ComponentQuery<Apple> appleQuery = new(world);
            Assert.Throws<ChunkModifiedWhileIteratingException>(() =>
            {
                foreach (var r in appleQuery)
                {
                    world.AddTag<IsThing>(r.entity);
                }
            });
        }
#endif
    }
}