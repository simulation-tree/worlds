using System.Collections.Generic;
using System.Linq;
using Unmanaged;

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
            appleQuery.Update();
            Assert.That(appleQuery.Count, Is.EqualTo(2));
            uint appleIndex = 0;
            for (uint i = 0; i < appleQuery.Count; i++)
            {
                Query.Result result = appleQuery.Get(i);
                if (result.ContainsComponent<Apple>())
                {
                    ref Apple apple = ref result.GetComponentRef<Apple>();
                    Assert.That(apple.bites, Is.EqualTo(0));

                    apple.bites += 4;
                    apple.bites += (byte)appleIndex;
                    appleIndex++;
                }
            }

            world.RemoveComponent<Apple>(a);
            appleQuery.Update();
            Assert.That(appleQuery.Count, Is.EqualTo(1));
            for (uint i = 0; i < appleQuery.Count; i++)
            {
                Query.Result result = appleQuery.Get(i);
                if (result.ContainsComponent<Apple>())
                {
                    ref Apple apple = ref result.GetComponentRef<Apple>();
                    Assert.That(apple.bites, Is.EqualTo(5));
                }
            }

            using Query<Apple, Berry> comboQuery = new(world);
            comboQuery.Update();
            Assert.That(comboQuery.Count, Is.EqualTo(1));
            Query.Result firstResult = comboQuery.Get(0);
            Assert.That(firstResult.ContainsComponent<Apple>(), Is.True);
            Assert.That(firstResult.ContainsComponent<Berry>(), Is.True);
            Assert.That(firstResult.GetComponentRef<Apple>().bites, Is.EqualTo(5));
            Assert.That(firstResult.GetComponentRef<Berry>().hearts, Is.EqualTo(0));

            using Query<Berry> onlyBerries = new(world, Query.Option.ExactComponentTypes);
            onlyBerries.Update();
            Assert.That(onlyBerries.Count, Is.EqualTo(1));
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