using System;
using System.Collections.Generic;
using System.Linq;
using Unmanaged;

namespace Simulation.Tests
{
    public class WorldInstructionTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAny();
        }

        [Test]
        public void CreateOneEntity()
        {
            using World world = new();
            using Operation operation = new();
            operation.CreateEntity();
            operation.AddComponent(new TestComponent(1337));

            world.Perform(operation);
            uint entity = world.Entities.First();
            Assert.That(world.ContainsComponent<TestComponent>(entity), Is.True);
            Assert.That(world.GetComponent<TestComponent>(entity).value, Is.EqualTo(1337));
        }

        [Test]
        public void DestroyExistingEntities()
        {
            using World world = new();
            for (uint i = 0; i < 30; i++)
            {
                world.CreateEntity();
            }

            using Operation operation = new();
            foreach (uint entity in world.Entities)
            {
                operation.SelectEntity(entity);
            }

            operation.RemoveInstructionAt(0);
            operation.DestroySelected();
            world.Perform(operation);

            Assert.That(world.Count, Is.EqualTo(29));
            Assert.That((uint)world.Entities.First(), Is.EqualTo(1));
        }

        [Test]
        public void CreateManyWithData()
        {
            using World world = new();
            using Operation operation = new();
            operation.CreateEntities(40);
            operation.AddComponent(new TestComponent(2));
            world.Perform(operation);
            List<uint> createdEntities = new();
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
            operation.CreateArray<char>(3);
            operation.SetArrayElement(0, 'a');
            operation.SetArrayElement(1, 'b');
            operation.SetArrayElement(2, 'c');

            using World world = new();
            world.Perform(operation);

            uint firstEntity = world[0];
            uint secondEntity = world[1];
            uint thirdEntity = world[2];

            Assert.That(world.GetComponent<TestComponent>(firstEntity).value, Is.EqualTo(4));
            Assert.That(world.GetComponent<TestComponent>(secondEntity).value, Is.EqualTo(5));
            Assert.That(world.GetComponent<TestComponent>(thirdEntity).value, Is.EqualTo(6));
            Assert.That(world.GetArray<char>(thirdEntity).ToString(), Is.EqualTo("abc"));
            Assert.That(world.GetParent(firstEntity), Is.EqualTo(default(uint)));
            Assert.That(world.GetParent(secondEntity), Is.EqualTo(default(uint)));
            Assert.That(world.GetParent(thirdEntity), Is.EqualTo(firstEntity));
        }

        public struct TestComponent
        {
            public int value;

            public TestComponent(int value)
            {
                this.value = value;
            }
        }

        [Test]
        public void SerializeInstructions()
        {
            Instruction a = Instruction.CreateEntity();
            Instruction b = Instruction.AddComponent(new TestComponent(1), out Allocation allocation);
            Instruction c = Instruction.CreateEntity();
            Instruction d = Instruction.SetParent(1);

            using BinaryWriter writer = new();
            writer.WriteObject(a);
            writer.WriteObject(b);
            writer.WriteObject(c);
            writer.WriteObject(d);

            using BinaryReader reader = new(writer);
            Instruction a2 = reader.ReadObject<Instruction>();
            Instruction b2 = reader.ReadObject<Instruction>();
            Instruction c2 = reader.ReadObject<Instruction>();
            Instruction d2 = reader.ReadObject<Instruction>();

            Assert.That(a2, Is.EqualTo(a));
            Assert.That(b2, Is.EqualTo(b));
            Assert.That(c2, Is.EqualTo(c));
            Assert.That(d2, Is.EqualTo(d));

            allocation.Dispose();
        }

        [Test]
        public void WriteSpanIntoArray()
        {
            string testString = "this is not an abacus";
            using Operation operation = new();
            operation.CreateEntity();
            operation.CreateArray<char>((uint)testString.Length);
            operation.SetArrayElements(0, testString.AsUSpan());

            using World world = new();
            world.Perform(operation);

            uint entity = world.Entities.First();
            USpan<char> list = world.GetArray<char>(entity);
            Assert.That(list.ToString(), Is.EqualTo(testString));
        }

        [Test]
        public void ModifyArrayMultipleTimes()
        {
            Operation operation = new();
            operation.CreateEntity();
            operation.CreateArray<char>(4);
            operation.SetArrayElement(0, 'a');

            using World world = new();
            world.Perform(operation);

            uint entity = world.Entities.First();
            Assert.That(world.ContainsArray<char>(entity), Is.True);

            USpan<char> list = world.GetArray<char>(entity);
            Assert.That(list[0], Is.EqualTo('a'));
            Assert.That(world.GetArrayLength<char>(entity), Is.EqualTo(4));

            operation.ClearInstructions();
            operation.SelectEntity(entity);
            operation.SetArrayElement(1, 'b');
            operation.SetArrayElement(2, 'c');

            world.Perform(operation);
            list = world.GetArray<char>(entity);
            Assert.That(list[0], Is.EqualTo('a'));
            Assert.That(list[1], Is.EqualTo('b'));
            Assert.That(list[2], Is.EqualTo('c'));

            operation.Dispose();
        }

        [Test]
        public void ResizeExistingArray()
        {
            using Operation operation = new();
            operation.CreateEntity();
            operation.CreateArray<char>(MemoryExtensions.AsSpan("abcd"));

            using World world = new();
            world.Perform(operation);

            uint entity = world.Entities.First();
            Assert.That(world.ContainsArray<char>(entity), Is.True);
            Assert.That(world.GetArrayLength<char>(entity), Is.EqualTo(4));

            operation.ClearInstructions();
            operation.SelectEntity(entity);
            operation.ResizeArray<char>(8);

            world.Perform(operation);

            Assert.That(world.GetArrayLength<char>(entity), Is.EqualTo(8));
        }
    }
}