using System;
using Unmanaged;

namespace Worlds.Tests
{
    public class ArrayTests : WorldTests
    {
        [Test]
        public void DestroyEntityWithArray()
        {
            World world = CreateWorld();
            uint a = world.CreateEntity();
            Values<SimpleComponent> list = world.CreateArray<SimpleComponent>(a, 2);
            list[0] = new("Hello World 1");
            list[1] = new("Hello World 2");
            world.DestroyEntity(a);

            Assert.That(world.ContainsEntity(a), Is.False);
#if DEBUG
            Assert.Throws<NullReferenceException>(() =>
            {
                world.ContainsArray<SimpleComponent>(a);
            });
#endif

            uint b = world.CreateEntity();
            Assert.That(world.ContainsEntity(b), Is.True);
            Assert.That(world.ContainsArray<SimpleComponent>(b), Is.False);

            world.CreateArray<SimpleComponent>(b, 2);

            Assert.That(world.ContainsArray<SimpleComponent>(b), Is.True);
            Assert.That(world.GetArrayLength<SimpleComponent>(b), Is.EqualTo(2));

            world.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void CreateThenEmptyArray()
        {
            World world = CreateWorld();
            uint entity = world.CreateEntity();
            world.CreateArray<SimpleComponent>(entity, 2);
            world.DestroyArray<SimpleComponent>(entity);
            Assert.That(world.ContainsArray<SimpleComponent>(entity), Is.False);
            world.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void DestroyArrayTwice()
        {
            World world = CreateWorld();
            uint entity = world.CreateEntity();
            Values<SimpleComponent> list = world.CreateArray<SimpleComponent>(entity, 4);
            list[0] = new("apple");
            Assert.That(list.Length, Is.EqualTo(4));
            world.DestroyEntity(entity);
            uint another = world.CreateEntity(); //same as `entity`
            Assert.That(another, Is.EqualTo(entity));
            Values<SimpleComponent> anotherList = world.CreateArray<SimpleComponent>(another, 1);
            anotherList[0] = new("banana");
            Assert.That(anotherList.Length, Is.EqualTo(1));
            world.DestroyEntity(another);
            world.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void ResizeArray()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            Values<SimpleComponent> list = world.CreateArray<SimpleComponent>(a, 2);

            Assert.That(list.Length, Is.EqualTo(2));

            list[0] = new("apple");
            list[1] = new("banana");

            list.Length = 4;

            Assert.That(world.GetArrayLength<SimpleComponent>(a), Is.EqualTo(4));
            Assert.That(list[0].data.ToString(), Is.EqualTo("apple"));
            Assert.That(list[1].data.ToString(), Is.EqualTo("banana"));
        }
    }
}