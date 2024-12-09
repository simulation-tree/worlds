﻿using System.Collections.Generic;
using Unmanaged;

namespace Worlds.Tests
{
    public class WorldInstructionTests : WorldTests
    {
        [Test]
        public void CreateOneEntity()
        {
            using World world = new();
            using Operation operation = new();
            Operation.SelectedEntity newEntity = operation.CreateEntity();
            newEntity.AddComponent(new TestComponent(1337));

            world.Perform(operation);
            uint entity = world[0];
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

            Assert.That(world.Count, Is.EqualTo(1));
            Assert.That(world[0], Is.EqualTo(1));
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

            operation.CreateEntity();
            operation.AddComponent(new TestComponent(5));

            operation.CreateEntity();
            operation.SetParentToPreviouslyCreatedEntity(2);
            operation.AddComponent(new TestComponent(6));
            operation.CreateArray<Character>(3);
            operation.SetArrayElement(0, (Character)'a');
            operation.SetArrayElement(1, (Character)'b');
            operation.SetArrayElement(2, (Character)'c');

            using World world = new();
            world.Perform(operation);

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
        public void AddInstructionsThenRemove()
        {
            using Operation operation = new();
            operation.SelectEntity(1);
            operation.AddComponent(new TestComponent(1));
            operation.ClearSelection();
            operation.SelectEntity(2);
            operation.AddComponent(new TestComponent(2));

            Assert.That(operation.Count, Is.EqualTo(5));

            operation.RemoveInstructionAt(2);
            Assert.That(operation.Count, Is.EqualTo(4));

            USpan<Instruction> instructions = operation.AsSpan();
            Assert.That(instructions[0].type, Is.EqualTo(Instruction.Type.SelectEntity));
            Assert.That(instructions[1].type, Is.EqualTo(Instruction.Type.AddComponent));
            Assert.That(instructions[2].type, Is.EqualTo(Instruction.Type.SelectEntity));
            Assert.That(instructions[3].type, Is.EqualTo(Instruction.Type.AddComponent));
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
            operation.CreateArray<Character>((uint)testString.Length);
            operation.SetArrayElements(0, testString.AsUSpan().As<Character>());

            using World world = new();
            world.Perform(operation);

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

            using World world = new();
            world.Perform(operation);

            uint entity = world[0];
            Assert.That(world.ContainsArray<Character>(entity), Is.True);

            USpan<Character> list = world.GetArray<Character>(entity);
            Assert.That((char)list[0], Is.EqualTo('a'));
            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(4));

            operation.ClearInstructions();
            Operation.SelectedEntity selected = operation.SelectEntity(entity);
            selected.SetArrayElement(1, (Character)'b');
            selected.SetArrayElement(2, (Character)'c');

            world.Perform(operation);
            list = world.GetArray<Character>(entity);
            Assert.That((char)list[0], Is.EqualTo('a'));
            Assert.That((char)list[1], Is.EqualTo('b'));
            Assert.That((char)list[2], Is.EqualTo('c'));

            operation.Dispose();
        }

        [Test]
        public void ResizeExistingArray()
        {
            using Operation operation = new();
            Operation.SelectedEntity newEntity = operation.CreateEntity();
            newEntity.CreateArray("abcd".AsUSpan().As<Character>());

            using World world = new();
            world.Perform(operation);

            uint entity = world[0];
            Assert.That(world.ContainsArray<Character>(entity), Is.True);
            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(4));

            operation.ClearInstructions();
            Operation.SelectedEntity selectedEntity = operation.SelectEntity(entity);
            selectedEntity.ResizeArray<Character>(8);

            world.Perform(operation);

            Assert.That(world.GetArrayLength<Character>(entity), Is.EqualTo(8));
        }

        [Test]
        public void InsertInstructions()
        {
            using Operation operation = new();
            operation.CreateEntity();
            operation.AddComponent(new TestComponent(1));
            operation.CreateEntity();
            operation.InsertInstructionAt(Instruction.AddComponent(new Another(25)), 1);

            Assert.That(operation.Count, Is.EqualTo(4));
            Assert.That(operation[0].type, Is.EqualTo(Instruction.Type.CreateEntity));
            Assert.That(operation[1].type, Is.EqualTo(Instruction.Type.AddComponent));
            Assert.That(operation[2].type, Is.EqualTo(Instruction.Type.AddComponent));
            Assert.That(operation[3].type, Is.EqualTo(Instruction.Type.CreateEntity));

            operation.InsertInstructionAt(Instruction.RemoveComponent<TestComponent>(), 4);

            Assert.That(operation.Count, Is.EqualTo(5));
            Assert.That(operation[4].type, Is.EqualTo(Instruction.Type.RemoveComponent));
        }

        [Test]
        public void AddOneComponentToSelectedEntity()
        {
            using Operation operation = new();
            Operation.SelectedEntity a = operation.CreateEntity();
            a.AddComponent(new TestComponent(1));

            Assert.That(operation.Count, Is.EqualTo(2));
            Assert.That(operation[0].type, Is.EqualTo(Instruction.Type.CreateEntity));
            Assert.That(operation[1].type, Is.EqualTo(Instruction.Type.AddComponent));
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

            using World world = new();
            world.Perform(operation);

            for (uint i = 1; i <= 5; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(i), Is.True);
                Assert.That(world.ContainsComponent<SimpleComponent>(i), Is.True);
            }

            operation.ClearInstructions();
            for (uint i = 1; i <= 5; i++)
            {
                operation.SelectEntity(i);
            }

            operation.RemoveComponent<TestComponent>();

            world.Perform(operation);

            for (uint i = 1; i <= 5; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(i), Is.False);
                Assert.That(world.ContainsComponent<SimpleComponent>(i), Is.True);
            }

            operation.ClearInstructions();
            for (uint i = 1; i <= 5; i++)
            {
                operation.SelectEntity(i);
            }

            operation.RemoveComponent<SimpleComponent>();

            world.Perform(operation);

            for (uint i = 1; i <= 5; i++)
            {
                Assert.That(world.ContainsComponent<TestComponent>(i), Is.False);
                Assert.That(world.ContainsComponent<SimpleComponent>(i), Is.False);
            }
        }
    }
}