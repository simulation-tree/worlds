using System;
using Unmanaged;

namespace Simulation.Tests
{
    public class WorldTests : UnmanagedTests
    {
        [Test]
        public void CreateAndDisposeWorld()
        {
            World world = new();
            world.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void DisposeTwiceError()
        {
            World world = new();
            world.Dispose();
            Assert.Throws<NullReferenceException>(() => world.Dispose());
        }

        [Test]
        public void NonCreatedWorldError()
        {
            World world = default;
            Assert.Throws<NullReferenceException>(() => world.Dispose());
        }

        [Test]
        public void DestroyParentEntity()
        {
            using World world = new();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity(a);
            uint c = world.CreateEntity(a);
            Assert.That(world.ContainsEntity(a), Is.True);
            Assert.That(world.ContainsEntity(b), Is.True);
            Assert.That(world.ContainsEntity(c), Is.True);
            Assert.That(world.GetParent(b), Is.EqualTo(a));
            Assert.That(world.GetParent(c), Is.EqualTo(a));
            Assert.That(world.GetChildren(a).ToArray(), Has.Member(b));
            Assert.That(world.GetChildren(a).ToArray(), Has.Member(c));
            world.DestroyEntity(a);
            Assert.That(world.ContainsEntity(a), Is.False);
            Assert.That(world.ContainsEntity(b), Is.False);
            Assert.That(world.ContainsEntity(c), Is.False);
        }

        [Test]
        public void GetAddedComponent()
        {
            using World world = new();
            uint entity = world.CreateEntity();
            SimpleComponent component = new("Hello World");
            world.AddComponent(entity, component);

            Assert.That(world.GetComponent<SimpleComponent>(entity), Is.EqualTo(component));
            world.RemoveComponent<SimpleComponent>(entity);
            Assert.That(world.ContainsComponent<SimpleComponent>(entity), Is.False);
            Assert.That(world.ContainsAnyComponent<SimpleComponent>(), Is.False);
            world.AddComponent(entity, component);
            Assert.That(world.ContainsComponent<SimpleComponent>(entity), Is.True);
            Assert.That(world.ContainsAnyComponent<SimpleComponent>(), Is.True);
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);
        }

        [Test]
        public void CreateAndDestroyEntity()
        {
            using World world = new();
            uint entity = world.CreateEntity();
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);
            Assert.That(world.Count, Is.EqualTo(0));
            Assert.Throws<NullReferenceException>(() => world.GetComponent<SimpleComponent>(entity));
        }

        [Test]
        public void AddingTwoComponents()
        {
            using World world = new();
            uint entity = world.CreateEntity();
            SimpleComponent component1 = new("Hello World");
            Another component2 = new(42);
            world.AddComponent(entity, component1);
            world.AddComponent(entity, component2);
            Assert.That(world.GetComponent<SimpleComponent>(entity), Is.EqualTo(component1));
            Assert.That(world.GetComponent<Another>(entity), Is.EqualTo(component2));
            world.RemoveComponent<SimpleComponent>(entity);
            Assert.Throws<NullReferenceException>(() => world.GetComponentRef<SimpleComponent>(entity));
            Assert.That(world.GetComponent<Another>(entity), Is.EqualTo(component2));
        }

        [Test]
        public void TwoInitialComponents()
        {
            using World world = new();
            uint a = world.CreateEntity(new Another(32), new SimpleComponent("what is this?"));
            Assert.That(world.ContainsComponent<SimpleComponent>(a), Is.True);
            Assert.That(world.ContainsComponent<Another>(a), Is.True);
            uint b = world.CreateEntity(new SimpleComponent("what is this?"), new Another(32));
            Assert.That(world.ContainsComponent<SimpleComponent>(b), Is.True);
            Assert.That(world.ContainsComponent<Another>(b), Is.True);
            Assert.That(world.GetComponent<SimpleComponent>(a), Is.EqualTo(world.GetComponent<SimpleComponent>(b)));
            Assert.That(world.GetComponent<Another>(a), Is.EqualTo(world.GetComponent<Another>(b)));
            world.RemoveComponent<SimpleComponent>(a);
            Assert.That(world.ContainsComponent<SimpleComponent>(a), Is.False);
        }

        [Test]
        public void DestroyEntityTwice()
        {
            using World world = new();
            uint entity = world.CreateEntity();
            Assert.That(world.ContainsEntity(entity), Is.True);
            world.DestroyEntity(entity);
            Assert.That(world.ContainsEntity(entity), Is.False);

            uint another = world.CreateEntity();
            Assert.That(world.ContainsEntity(another), Is.True);
            world.DestroyEntity(another);
            Assert.That(world.ContainsEntity(another), Is.False);
        }

        [Test]
        public void EnablingAndDisabling()
        {
            using World world = new();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            world.AddComponent(a, new SimpleComponent("Hello World"));
            Assert.That(world.IsEnabled(a), Is.True);
            Assert.That(world.IsEnabled(b), Is.True);
            world.SetEnabled(a, false);
            Assert.That(world.IsEnabled(a), Is.False);
            Assert.That(world.IsEnabled(b), Is.True);
            world.SetEnabled(a, true);
            Assert.That(world.IsEnabled(a), Is.True);
        }

        [Test]
        public void DestroyEntityWithArray()
        {
            World world = new();
            uint entity = world.CreateEntity();
            USpan<SimpleComponent> list = world.CreateArray<SimpleComponent>(entity, 2);
            list[0] = new("Hello World 1");
            list[1] = new("Hello World 2");
            world.DestroyEntity(entity);

            Assert.That(world.ContainsEntity(entity), Is.False);
            Assert.Throws<NullReferenceException>(() =>
            {
                world.ContainsArray<SimpleComponent>(entity);
            });

            world.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void DestroyArrayTwice()
        {
            World world = new();
            uint entity = world.CreateEntity();
            USpan<SimpleComponent> list = world.CreateArray<SimpleComponent>(entity, 4);
            list[0] = new("apple");
            Assert.That(list.Length, Is.EqualTo(4));
            world.DestroyEntity(entity);
            uint another = world.CreateEntity(); //same as `entity`
            USpan<SimpleComponent> anotherList = world.CreateArray<SimpleComponent>(another, 1);
            anotherList[0] = new("banana");
            Assert.That(anotherList.Length, Is.EqualTo(1));
            world.DestroyEntity(another);
            world.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void PopulateWorldThenClear()
        {
            using World world = new();
            using RandomGenerator rng = RandomGenerator.Create();
            uint realEntities = 0;
            for (uint i = 0; i < 100; i++)
            {
                uint entity = world.CreateEntity();
                if (rng.NextBool())
                {
                    world.AddComponent(entity, new SimpleComponent("apple"));
                }

                if (rng.NextBool())
                {
                    world.AddComponent(entity, new Another(5));
                }

                if (rng.NextBool())
                {
                    uint length = rng.NextUInt(1, 10);
                    USpan<char> array = world.CreateArray<char>(entity, length);
                    for (uint j = 0; j < length; j++)
                    {
                        array[j] = (char)rng.NextInt('a', 'z');
                    }
                }

                if (rng.NextBool())
                {
                    world.DestroyEntity(entity);
                }
                else
                {
                    realEntities++;
                }
            }

            Assert.That(world.Count, Is.EqualTo(realEntities));
            world.Clear();
            Assert.That(world.Count, Is.EqualTo(0));
            for (uint i = 0; i < 100; i++)
            {
                uint entity = i + 1;
                Assert.That(world.ContainsEntity(entity), Is.False);
            }
        }

        [Test]
        public void CloneEntity()
        {
            using World world = new();
            uint entity = world.CreateEntity();
            world.AddComponent(entity, new SimpleComponent("apple"));
            world.AddComponent(entity, new Another(5));
            USpan<char> array = world.CreateArray<char>(entity, 5);
            array[0] = 'a';
            array[1] = 'b';
            array[2] = 'c';
            array[3] = 'd';
            array[4] = 'e';
            USpan<uint> data = world.CreateArray<uint>(entity, 3);
            data[0] = 1337;
            data[1] = 666;
            data[2] = 500513;

            uint clone = world.CloneEntity(entity);
            Assert.That(world.ContainsEntity(clone), Is.True);
            Assert.That(world.ContainsComponent<SimpleComponent>(clone), Is.True);
            Assert.That(world.ContainsComponent<Another>(clone), Is.True);
            Assert.That(world.GetComponent<SimpleComponent>(clone).data, Is.EqualTo(new FixedString("apple")));
            Assert.That(world.GetComponent<Another>(clone).data, Is.EqualTo(5));
            USpan<char> cloneArray = world.GetArray<char>(clone);
            Assert.That(cloneArray.Length, Is.EqualTo(5));
            Assert.That(cloneArray[0], Is.EqualTo('a'));
            Assert.That(cloneArray[1], Is.EqualTo('b'));
            Assert.That(cloneArray[2], Is.EqualTo('c'));
            Assert.That(cloneArray[3], Is.EqualTo('d'));
            Assert.That(cloneArray[4], Is.EqualTo('e'));
            USpan<uint> cloneData = world.GetArray<uint>(clone);
            Assert.That(cloneData.Length, Is.EqualTo(3));
            Assert.That(cloneData[0], Is.EqualTo(1337));
            Assert.That(cloneData[1], Is.EqualTo(666));
            Assert.That(cloneData[2], Is.EqualTo(500513));
        }
    }
}