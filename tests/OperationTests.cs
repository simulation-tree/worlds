using Collections.Generic;
using System;
using Unmanaged;

namespace Worlds.Tests
{
    public class OperationTests : WorldTests
    {
#if DEBUG
        [Test]
        public void ThrowIfSelectionIsEmpty()
        {
            using World world = CreateWorld();
            using Operation operation = new(world);
            Assert.Throws<InvalidOperationException>(() => operation.AddComponent(new TestComponent(1)));
        }
#endif
        [Test]
        public void CreateOneEntity()
        {
            using World world = CreateWorld();
            using Operation operation = new(world);
            Assert.That(operation.Count, Is.EqualTo(0));
            operation.CreateSingleEntityAndSelect();
            operation.AddComponent(new TestComponent(1337));
            operation.Perform();
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
            using Operation operation = new(world);
            foreach (uint entity in world.Entities)
            {
                if (entity != randomEntity)
                {
                    operation.AppendEntityToSelection(entity);
                }
            }

            operation.DestroySelectedEntities();
            operation.Perform();

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
            using Operation operation = new(world);
            for (int i = 0; i < creationCount; i++)
            {
                operation.CreateSingleEntityAndSelect();
            }

            operation.AddComponent(new TestComponent(2));
            operation.Perform();
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
            using World world = CreateWorld();
            using Operation operation = new(world);
            operation.CreateSingleEntityAndSelect();
            operation.AddComponent(new TestComponent(4));
            operation.ClearSelection();

            operation.CreateSingleEntityAndSelect();
            operation.AddComponent(new TestComponent(5));
            operation.ClearSelection();

            operation.CreateSingleEntityAndSelect();
            operation.SetParentToPreviouslyCreatedEntity(2);
            operation.AddComponent(new TestComponent(6));
            operation.CreateArray<Character>(3);
            operation.SetArrayElement(0, (Character)'a');
            operation.SetArrayElement(1, (Character)'b');
            operation.SetArrayElement(2, (Character)'c');

            operation.Perform();

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
        public void CreateOrSetArray()
        {
            using World world = CreateWorld();
            using Operation operation = new(world);

            operation.CreateSingleEntityAndSelect();
            operation.CreateOrSetArray("hello there".AsSpan().As<char, Character>());
            operation.Perform();
            operation.Reset();

            Assert.That(world.Count, Is.EqualTo(1));
            uint entity = world[0];
            Assert.That(world.ContainsArray<Character>(entity), Is.True);
            Assert.That(world.GetArray<Character>(entity).AsSpan<char>().ToString(), Is.EqualTo("hello there"));

            operation.SetSelectedEntity(entity);
            operation.DestroySelectedEntities();
            operation.Perform();
            operation.Reset();

            Assert.That(world.Count, Is.EqualTo(0));

            entity = world.CreateEntity();
            Assert.That(world.Count, Is.EqualTo(1));

            operation.SetSelectedEntity(entity);
            operation.CreateOrSetArray("and goodbye".AsSpan().As<char, Character>());
            operation.Perform();
            operation.Reset();

            Assert.That(world.ContainsArray<Character>(entity), Is.True);
            Assert.That(world.GetArray<Character>(entity).AsSpan<char>().ToString(), Is.EqualTo("and goodbye"));
        }

        [Test]
        public void CountInstructions()
        {
            using World world = CreateWorld();
            using Operation operation = new(world);

            operation.AppendEntityToSelection(1);
            operation.AddComponent(new TestComponent(1));
            operation.ClearSelection();

            operation.AppendEntityToSelection(2);
            operation.AddComponent(new TestComponent(2));

            Assert.That(operation.Count, Is.EqualTo(5));
        }

        [Test]
        public void WriteSpanIntoArray()
        {
            using World world = CreateWorld();
            string testString = "this is not an abacus";
            using Operation operation = new(world);
            operation.CreateSingleEntityAndSelect();
            operation.CreateArray<Character>(testString.Length);
            operation.SetArrayElements(0, testString.AsSpan().As<char, Character>());

            operation.Perform();

            uint entity = world[0];
            Span<Character> list = world.GetArray<Character>(entity);
            Assert.That(list.As<Character, char>().ToString(), Is.EqualTo(testString));

            operation.Reset();
            operation.AppendEntityToSelection(entity);
            operation.SetArrayElements("this is not a".Length, " burrito".AsSpan().As<char, Character>());
            operation.Perform();

            list = world.GetArray<Character>(entity);
            Assert.That(list.As<Character, char>().ToString(), Is.EqualTo("this is not a burrito"));
        }

        [Test]
        public void ModifyArrayMultipleTimes()
        {
            using World world = CreateWorld();
            Operation operation = new(world);
            operation.CreateSingleEntityAndSelect();
            operation.CreateArray<Character>(4);
            operation.SetArrayElement(0, (Character)'a');

            operation.Perform();

            uint entity = world[0];
            Assert.That(world.ContainsArray<Character>(entity), Is.True);

            Span<Character> list = world.GetArray<Character>(entity);
            Assert.That((char)list[0], Is.EqualTo('a'));
            Assert.That((char)list[1], Is.EqualTo('\0'));
            Assert.That((char)list[2], Is.EqualTo('\0'));
            Assert.That((char)list[3], Is.EqualTo('\0'));
            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(4));

            operation.Reset();
            operation.AppendEntityToSelection(entity);
            operation.SetArrayElements<Character>(1, ['b', 'c']);

            operation.Perform();
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
            using World world = CreateWorld();
            using Operation operation = new(world);
            operation.CreateSingleEntityAndSelect();
            operation.AddComponent(new TestComponent(1));

            Assert.That(operation.Count, Is.EqualTo(2));

            operation.Reset();

            Assert.That(operation.Count, Is.EqualTo(0));
        }

        [Test]
        public void CountingCreatedEntities()
        {
            using World world = CreateWorld();
            using Operation operation = new(world);
            int creatingAmount = 7;
            operation.CreateMultipleEntities(creatingAmount);
            Span<uint> createdEntities = stackalloc uint[32];
            int count = operation.GetCreatedEntities(createdEntities);
            Assert.That(count, Is.EqualTo(creatingAmount));
            operation.Perform();
            operation.Reset();
            Assert.That(world.Count, Is.EqualTo(count));
            for (int i = 0; i < creatingAmount; i++)
            {
                Assert.That(world[i], Is.EqualTo(createdEntities[i]));
            }

            for (uint i = 1; i <= creatingAmount / 2; i++)
            {
                world.DestroyEntity(i);
            }

            creatingAmount = 9;
            operation.CreateMultipleEntities(creatingAmount);
            count = operation.GetCreatedEntities(createdEntities);
            Assert.That(count, Is.EqualTo(creatingAmount));
            for (int i = 0; i < creatingAmount; i++)
            {
                Assert.That(world.GetNextCreatedEntity(i), Is.EqualTo(createdEntities[i]));
            }

            operation.Perform();
            for (int i = 0; i < creatingAmount; i++)
            {
                Assert.That(world.ContainsEntity(createdEntities[i]), Is.True, $"Entity {createdEntities[i]} was not created in the world");
            }
        }

        [Test]
        public void ResizeExistingArray()
        {
            using World world = CreateWorld();
            using Operation operation = new(world);
            operation.CreateSingleEntityAndSelect();
            operation.CreateArray("abcd".AsSpan().As<char, Character>());

            operation.Perform();

            uint entity = world[0];
            Assert.That(world.ContainsArray<Character>(entity), Is.True);
            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(4));
            Assert.That(world.GetArray<Character>(entity).AsSpan<char>().ToString(), Is.EqualTo("abcd"));

            operation.Reset();
            operation.AppendEntityToSelection(entity);
            operation.ResizeArray<Character>(8);
            operation.Perform();

            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(8));
        }

        [Test]
        public void AddingReferences()
        {
            using World world = CreateWorld();
            using Operation operation = new(world);
            operation.CreateSingleEntity();
            operation.CreateSingleEntityAndSelect();
            operation.AddReferenceTowardsPreviouslyCreatedEntity(1);
            operation.Perform();
            operation.Reset();
            uint a = world[0];
            uint b = world[1];
            Assert.That(world.GetReferenceCount(a), Is.EqualTo(0));
            Assert.That(world.GetReferenceCount(b), Is.EqualTo(1));
            Assert.That(world.GetReference(b, (rint)1), Is.EqualTo(a));

            uint c = world.CreateEntity();
            Span<rint> referencesArray = stackalloc rint[17];
            for (int i = 0; i < 17; i++)
            {
                operation.ClearSelection();
                operation.CreateSingleEntityAndSelect();
                operation.AddComponent(new TestComponent(i));
                operation.SetParent(c);
                operation.CreateArray(("this is " + i).AsSpan().As<char, Character>());
                operation.SetSelectedEntity(c);
                operation.AddReferenceTowardsPreviouslyCreatedEntity(0);
                referencesArray[i] = (rint)(i + 1);
            }

            operation.CreateOrSetArray(referencesArray);
            operation.Perform();
            ReadOnlySpan<uint> references = world.GetReferences(c);
            Span<uint> children = stackalloc uint[32];
            int childCount = world.CopyChildrenTo(c, children);
            Assert.That(childCount, Is.EqualTo(references.Length));
            for (int i = 0; i < childCount; i++)
            {
                Assert.That(children[i], Is.EqualTo(references[i]));
                uint childEntity = children[i];
                rint reference = world.GetArrayElement<rint>(c, i);
                uint referencedEntity = world.GetReference(c, reference);
                Assert.That(referencedEntity, Is.EqualTo(childEntity));
                Assert.That(world.ContainsComponent<TestComponent>(childEntity), Is.True);
                Assert.That(world.GetComponent<TestComponent>(childEntity).value, Is.EqualTo(i));
                Assert.That(world.ContainsArray<Character>(referencedEntity), Is.True);
                Assert.That(world.GetArray<Character>(referencedEntity).AsSpan<char>().ToString(), Is.EqualTo("this is " + i));
            }
        }

        [Test]
        public void AddOneComponentToSelectedEntity()
        {
            using World world = CreateWorld();
            using Operation operation = new(world);
            operation.CreateSingleEntityAndSelect();
            operation.AddComponent(new TestComponent(1));
            Assert.That(operation.Count, Is.EqualTo(2));
            operation.AddOrSetComponent(new TestComponent(5));
            Assert.That(operation.Count, Is.EqualTo(3));
            operation.Perform();
            operation.Reset();
            Assert.That(operation.Count, Is.EqualTo(0));
            Assert.That(world.Count, Is.EqualTo(1));
            uint firstEntity = world[0];
            Assert.That(world.ContainsComponent<TestComponent>(firstEntity), Is.True);
            Assert.That(world.GetComponent<TestComponent>(firstEntity).value, Is.EqualTo(5));
            world.SetComponent(firstEntity, new TestComponent(25));
            Assert.That(world.ContainsComponent<TestComponent>(firstEntity), Is.True);
            Assert.That(world.GetComponent<TestComponent>(firstEntity).value, Is.EqualTo(25));
            Assert.That(world.ContainsComponent<Another>(firstEntity), Is.False);

            operation.AppendEntityToSelection(firstEntity);
            operation.AddOrSetComponent(new Another(231));
            operation.Perform();
            operation.Reset();
            Assert.That(world.ContainsComponent<Another>(firstEntity), Is.True);
            Assert.That(world.GetComponent<Another>(firstEntity).data, Is.EqualTo(231));
            Assert.That(world.ContainsComponent<TestComponent>(firstEntity), Is.True);
            Assert.That(world.GetComponent<TestComponent>(firstEntity).value, Is.EqualTo(25));

            operation.AppendEntityToSelection(firstEntity);
            operation.AddOrSetComponent(new Another(1234));
            operation.Perform();
            operation.Reset();
            Assert.That(world.ContainsComponent<Another>(firstEntity), Is.True);
            Assert.That(world.GetComponent<Another>(firstEntity).data, Is.EqualTo(1234));
        }

        [Test]
        public void AddThenRemoveComponents()
        {
            using World world = CreateWorld();
            using Operation operation = new(world);
            for (uint i = 0; i < 5; i++)
            {
                operation.CreateSingleEntity();
            }

            Assert.That(operation.Count, Is.EqualTo(5));
            for (uint i = 0; i < 5; i++)
            {
                operation.SelectPreviouslyCreatedEntity(i);
            }

            operation.AddComponent(new TestComponent(1));
            operation.AddComponent(new SimpleComponent("what"));
            operation.Perform();

            for (uint i = 1; i <= 5; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(i), Is.True);
                Assert.That(world.ContainsComponent<SimpleComponent>(i), Is.True);
            }

            operation.Reset();
            for (uint i = 1; i <= 5; i++)
            {
                operation.AppendEntityToSelection(i);
            }

            operation.RemoveComponentType<TestComponent>();
            operation.Perform();

            for (uint i = 1; i <= 5; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(i), Is.False);
                Assert.That(world.ContainsComponent<SimpleComponent>(i), Is.True);
            }

            operation.Reset();
            for (uint i = 1; i <= 5; i++)
            {
                operation.AppendEntityToSelection(i);
            }

            operation.RemoveComponentType<SimpleComponent>();
            operation.Perform();

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

            using Operation operation = new(world);
            operation.AppendMultipleEntitiesToSelection(entities);
            operation.AddComponent<TestComponent>(default);
            operation.AddComponent<SimpleComponent>(default);
            operation.Perform();
            for (int i = 0; i < entities.Length; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(entities[i]), Is.True);
                Assert.That(world.ContainsComponent<SimpleComponent>(entities[i]), Is.True);
            }

            operation.Reset();
            operation.AppendMultipleEntitiesToSelection(entities);
            operation.RemoveComponentType<TestComponent>();
            operation.RemoveComponentType<SimpleComponent>();
            operation.Perform();
            for (int i = 0; i < entities.Length; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(entities[i]), Is.False);
                Assert.That(world.ContainsComponent<SimpleComponent>(entities[i]), Is.False);
            }
        }
    }
}