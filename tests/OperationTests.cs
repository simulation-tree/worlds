using Collections;
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
            operation.CreateEntity();
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
        public void CreateManyWithData()
        {
            using World world = CreateWorld();
            using Operation operation = new();
            operation.CreateEntities(40);
            operation.AddComponent(new TestComponent(2));
            operation.Perform(world);
            using List<uint> createdEntities = new();
            foreach (uint entity in world.Entities)
            {
                createdEntities.Add(entity);
                Assert.That(world.GetComponent<TestComponent>(entity).value, Is.EqualTo(2));
            }

            Assert.That(createdEntities.Count, Is.EqualTo(40));
        }

        [Test]
        public void CreateThreeObjects()
        {
            using Schema schema = CreateSchema();
            using Operation operation = new();
            operation.CreateEntity();
            operation.AddComponent(new TestComponent(4));
            operation.ClearSelection();

            operation.CreateEntity();
            operation.AddComponent(new TestComponent(5));
            operation.ClearSelection();

            operation.CreateEntity();
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
            Assert.That(world.GetArray<Character>(thirdEntity).As<char>().ToString(), Is.EqualTo("abc"));
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
            operation.CreateEntity();
            operation.CreateArray<Character>((uint)testString.Length);
            operation.SetArrayElements(0, new USpan<char>(testString).As<Character>());

            using World world = CreateWorld();
            operation.Perform(world);

            uint entity = world[0];
            USpan<Character> list = world.GetArray<Character>(entity);
            Assert.That(list.As<char>().ToString(), Is.EqualTo(testString));
        }

        [Test]
        public void ModifyArrayMultipleTimes()
        {
            Operation operation = new();
            operation.CreateEntity();
            operation.CreateArray<Character>(4);
            operation.SetArrayElement(0, (Character)'a');

            using World world = CreateWorld();
            operation.Perform(world);

            uint entity = world[0];
            Assert.That(world.ContainsArray<Character>(entity), Is.True);

            USpan<Character> list = world.GetArray<Character>(entity);
            Assert.That((char)list[0], Is.EqualTo('a'));
            Assert.That((char)list[1], Is.EqualTo('\0'));
            Assert.That((char)list[2], Is.EqualTo('\0'));
            Assert.That((char)list[3], Is.EqualTo('\0'));
            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(4));

            operation.Clear();
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
        public void ResizeExistingArray()
        {
            using Operation operation = new();
            operation.CreateEntity();
            operation.CreateArray(new USpan<char>("abcd").As<Character>());

            using World world = CreateWorld();
            operation.Perform(world);

            uint entity = world[0];
            Assert.That(world.ContainsArray<Character>(entity), Is.True);
            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(4));

            operation.Clear();
            operation.SelectEntity(entity);
            operation.ResizeArray<Character>(8);

            operation.Perform(world);

            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(8));
        }

        [Test]
        public void AddOneComponentToSelectedEntity()
        {
            using Operation operation = new();
            operation.CreateEntity();
            operation.AddComponent(new TestComponent(1));

            Assert.That(operation.Count, Is.EqualTo(2));
        }

        [Test]
        public void AddThenRemoveComponents()
        {
            using Operation operation = new();
            for (uint i = 0; i < 5; i++)
            {
                operation.CreateEntity(false);
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

            operation.Clear();
            for (uint i = 1; i <= 5; i++)
            {
                operation.SelectEntity(i);
            }

            operation.RemoveComponent<TestComponent>();

            operation.Perform(world);

            for (uint i = 1; i <= 5; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(i), Is.False);
                Assert.That(world.ContainsComponent<SimpleComponent>(i), Is.True);
            }

            operation.Clear();
            for (uint i = 1; i <= 5; i++)
            {
                operation.SelectEntity(i);
            }

            operation.RemoveComponent<SimpleComponent>();

            operation.Perform(world);

            for (uint i = 1; i <= 5; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(i), Is.False);
                Assert.That(world.ContainsComponent<SimpleComponent>(i), Is.False);
            }
        }
    }
}