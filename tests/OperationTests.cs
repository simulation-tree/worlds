using Collections.Generic;
using System;
using Unmanaged;

namespace Worlds.Tests
{
    public class OperationTests : WorldTests
    {
        [Test]
        public void CreateOneEntity()
        {
            using World world = CreateWorld();
            using Operation operation = new();
            Assert.That(operation.Count, Is.EqualTo(0));
            operation.CreateEntityAndSelect();
            operation.AddComponent(new TestComponent(1337));
            operation.Perform(world);
            uint entity = world[0];
            Assert.That(world.ContainsComponent<TestComponent>(entity), Is.True);
            Assert.That(world.GetComponent<TestComponent>(entity).value, Is.EqualTo(1337));
        }

        [Test]
        public void DestroyExistingEntities()
        {
            using RandomGenerator rng = new();
            using World world = CreateWorld();
            for (uint i = 0; i < 30; i++)
            {
                world.CreateEntity();
            }

            uint randomEntity = rng.NextUInt(1, 30);
            using Operation operation = new();
            foreach (uint entity in world.Entities)
            {
                if (entity != randomEntity)
                {
                    operation.SelectEntity(entity);
                }
            }

            operation.DestroySelected();
            operation.Perform(world);

            Assert.That(world.Count, Is.EqualTo(1));
            Assert.That(world[0], Is.EqualTo(randomEntity));
        }

        [Test]
        public void CreateComponentFromBytes()
        {
            using World world = CreateWorld();
            uint a = world.CreateEntity();
            uint b = world.CreateEntity();
            int componentType = world.Schema.GetComponentType<TestComponent>();
            world.AddComponentBytes(a, componentType, [2, 0, 0, 0]);
            world.AddComponentBytes(b, componentType, [3, 0, 0, 0]);
            Assert.That(world.ContainsComponent<TestComponent>(a), Is.True);
            Assert.That(world.GetComponent<TestComponent>(a).value, Is.EqualTo(2));
            Assert.That(world.ContainsComponent<TestComponent>(b), Is.True);
            Assert.That(world.GetComponent<TestComponent>(b).value, Is.EqualTo(3));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(8)]
        [TestCase(10)]
        [TestCase(12)]
        [TestCase(40)]
        public void CreateManyWithData(int creationCount)
        {
            using World world = CreateWorld();
            using Operation operation = new();
            for (int i = 0; i < creationCount; i++)
            {
                operation.CreateEntityAndSelect();
            }

            operation.AddComponent(new TestComponent(2));
            operation.Perform(world);
            Assert.That(world.Count, Is.EqualTo(creationCount));
            using List<uint> createdEntities = new();
            foreach (uint entity in world.Entities)
            {
                createdEntities.Add(entity);
                Assert.That(world.ContainsComponent<TestComponent>(entity), Is.True);
                Assert.That(world.GetComponent<TestComponent>(entity).value, Is.EqualTo(2), $"Entity {entity} was expected to have a value of 2");
            }
        }

        [Test]
        public void CreateThreeObjects()
        {
            using Schema schema = CreateSchema();
            using Operation operation = new();
            operation.CreateEntityAndSelect();
            operation.AddComponent(new TestComponent(4));
            operation.ClearSelection();

            operation.CreateEntityAndSelect();
            operation.AddComponent(new TestComponent(5));
            operation.ClearSelection();

            operation.CreateEntityAndSelect();
            operation.SetParentToPreviouslyCreatedEntity(2);
            operation.AddComponent(new TestComponent(6));
            operation.CreateArray<Character>(3);
            operation.SetArrayElement(0, (Character)'a');
            operation.SetArrayElement(1, (Character)'b');
            operation.SetArrayElement(2, (Character)'c');

            using World world = CreateWorld();
            operation.Perform(world);

            uint firstEntity = world[0];
            uint secondEntity = world[1];
            uint thirdEntity = world[2];

            Assert.That(world.GetComponent<TestComponent>(firstEntity).value, Is.EqualTo(4));
            Assert.That(world.GetComponent<TestComponent>(secondEntity).value, Is.EqualTo(5));
            Assert.That(world.GetComponent<TestComponent>(thirdEntity).value, Is.EqualTo(6));
            Assert.That(world.GetArray<Character>(thirdEntity).AsSpan<char>().ToString(), Is.EqualTo("abc"));
            Assert.That(world.GetParent(firstEntity), Is.EqualTo(default(uint)));
            Assert.That(world.GetParent(secondEntity), Is.EqualTo(default(uint)));
            Assert.That(world.GetParent(thirdEntity), Is.EqualTo(firstEntity));
        }

        [Test]
        public void CountInstructions()
        {
            using Operation operation = new();

            operation.SelectEntity(1);
            operation.AddComponent(new TestComponent(1));
            operation.ClearSelection();

            operation.SelectEntity(2);
            operation.AddComponent(new TestComponent(2));

            Assert.That(operation.Count, Is.EqualTo(5));
        }

        [Test]
        public void WriteSpanIntoArray()
        {
            string testString = "this is not an abacus";
            using Operation operation = new();
            operation.CreateEntityAndSelect();
            operation.CreateArray<Character>(testString.Length);
            operation.SetArrayElements(0, testString.AsSpan().As<char, Character>());

            using World world = CreateWorld();
            operation.Perform(world);

            uint entity = world[0];
            Span<Character> list = world.GetArray<Character>(entity);
            Assert.That(list.As<Character, char>().ToString(), Is.EqualTo(testString));
        }

        [Test]
        public void ModifyArrayMultipleTimes()
        {
            Operation operation = new();
            operation.CreateEntityAndSelect();
            operation.CreateArray<Character>(4);
            operation.SetArrayElement(0, (Character)'a');

            using World world = CreateWorld();
            operation.Perform(world);

            uint entity = world[0];
            Assert.That(world.ContainsArray<Character>(entity), Is.True);

            Span<Character> list = world.GetArray<Character>(entity);
            Assert.That((char)list[0], Is.EqualTo('a'));
            Assert.That((char)list[1], Is.EqualTo('\0'));
            Assert.That((char)list[2], Is.EqualTo('\0'));
            Assert.That((char)list[3], Is.EqualTo('\0'));
            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(4));

            operation.Reset();
            operation.SelectEntity(entity);
            operation.SetArrayElements<Character>(1, ['b', 'c']);

            operation.Perform(world);
            list = world.GetArray<Character>(entity);
            Assert.That((char)list[0], Is.EqualTo('a'));
            Assert.That((char)list[1], Is.EqualTo('b'));
            Assert.That((char)list[2], Is.EqualTo('c'));
            Assert.That((char)list[3], Is.EqualTo('\0'));

            operation.Dispose();
        }

        [Test]
        public void ClearingInstructions()
        {
            using Operation operation = new();
            operation.CreateEntityAndSelect();
            operation.AddComponent(new TestComponent(1));

            Assert.That(operation.Count, Is.EqualTo(2));

            operation.Reset();

            Assert.That(operation.Count, Is.EqualTo(0));
        }

        [Test]
        public void ResizeExistingArray()
        {
            using Operation operation = new();
            operation.CreateEntityAndSelect();
            operation.CreateArray("abcd".AsSpan().As<char, Character>());

            using World world = CreateWorld();
            operation.Perform(world);

            uint entity = world[0];
            Assert.That(world.ContainsArray<Character>(entity), Is.True);
            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(4));

            operation.Reset();
            operation.SelectEntity(entity);
            operation.ResizeArray<Character>(8);

            operation.Perform(world);

            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(8));
        }

        [Test]
        public void AddOneComponentToSelectedEntity()
        {
            using Operation operation = new();
            operation.CreateEntityAndSelect();
            operation.AddComponent(new TestComponent(1));

            Assert.That(operation.Count, Is.EqualTo(2));
        }

        [Test]
        public void AddThenRemoveComponents()
        {
            using Operation operation = new();
            for (uint i = 0; i < 5; i++)
            {
                operation.CreateEntity();
            }

            Assert.That(operation.Count, Is.EqualTo(5));
            for (uint i = 0; i < 5; i++)
            {
                operation.SelectPreviouslyCreatedEntity(i);
            }

            operation.AddComponent(new TestComponent(1));
            operation.AddComponent(new SimpleComponent("what"));

            using World world = CreateWorld();
            operation.Perform(world);

            for (uint i = 1; i <= 5; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(i), Is.True);
                Assert.That(world.ContainsComponent<SimpleComponent>(i), Is.True);
            }

            operation.Reset();
            for (uint i = 1; i <= 5; i++)
            {
                operation.SelectEntity(i);
            }

            operation.RemoveComponentType<TestComponent>();

            operation.Perform(world);

            for (uint i = 1; i <= 5; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(i), Is.False);
                Assert.That(world.ContainsComponent<SimpleComponent>(i), Is.True);
            }

            operation.Reset();
            for (uint i = 1; i <= 5; i++)
            {
                operation.SelectEntity(i);
            }

            operation.RemoveComponentType<SimpleComponent>();

            operation.Perform(world);

            for (uint i = 1; i <= 5; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(i), Is.False);
                Assert.That(world.ContainsComponent<SimpleComponent>(i), Is.False);
            }
        }

        [Test]
        public void SimplerAddThenRemove()
        {
            using World world = CreateWorld();
            Span<uint> entities = stackalloc uint[100];
            world.CreateEntities(entities);

            Assert.That(world.Count, Is.EqualTo(entities.Length));

            using Operation operation = new();
            operation.SelectEntities(entities);
            operation.AddComponent<TestComponent>(default);
            operation.AddComponent<SimpleComponent>(default);

            operation.Perform(world);
            for (int i = 0; i < entities.Length; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(entities[i]), Is.True);
                Assert.That(world.ContainsComponent<SimpleComponent>(entities[i]), Is.True);
            }

            operation.Reset();
            operation.SelectEntities(entities);
            operation.RemoveComponentType<TestComponent>();
            operation.RemoveComponentType<SimpleComponent>();

            operation.Perform(world);
            for (int i = 0; i < entities.Length; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(entities[i]), Is.False);
                Assert.That(world.ContainsComponent<SimpleComponent>(entities[i]), Is.False);
            }
        }
    }
}