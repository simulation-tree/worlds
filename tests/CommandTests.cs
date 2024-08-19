using System;
using System.Collections.Generic;
using System.Linq;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    public class CommandTests
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
            using UnmanagedList<Instruction> commands = new();
            commands.Add(Instruction.CreateEntity());
            commands.Add(Instruction.AddComponent(new TestComponent(1337)));

            world.Perform(commands);
            eint entity = world.Entities.First();
            Assert.That(world.ContainsComponent<TestComponent>(entity), Is.True);
            Assert.That(world.GetComponent<TestComponent>(entity).value, Is.EqualTo(1337));
        }

        [Test]
        public void DestroyExistingEntities()
        {
            using World world = new();
            for (int i = 0; i < 30; i++)
            {
                world.CreateEntity();
            }

            using UnmanagedList<Instruction> commands = new();
            foreach (eint entity in world.Entities)
            {
                commands.Add(Instruction.AddToSelection(entity));
            }

            commands.RemoveAt(0);
            commands.Add(Instruction.DestroySelection());

            world.Perform(commands);

            Assert.That(world.Count, Is.EqualTo(1));
            Assert.That((uint)world.Entities.First(), Is.EqualTo(1));
        }

        [Test]
        public void CreateManyWithData()
        {
            using World world = new();
            using UnmanagedList<Instruction> commands = new();
            commands.Add(Instruction.CreateEntity(40));
            commands.Add(Instruction.AddComponent(new TestComponent(2)));
            world.Perform(commands);
            List<eint> createdEntities = new();
            foreach (eint entity in world.Entities)
            {
                createdEntities.Add(entity);
                Assert.That(world.GetComponent<TestComponent>(entity).value, Is.EqualTo(2));
            }

            Assert.That(createdEntities.Count, Is.EqualTo(40));
        }

        [Test]
        public void CreateThreeObjects()
        {
            using UnmanagedList<Instruction> commands = new();
            commands.Add(Instruction.CreateEntity());
            commands.Add(Instruction.AddComponent(new TestComponent(4)));

            commands.Add(Instruction.CreateEntity());
            commands.Add(Instruction.AddComponent(new TestComponent(5)));

            commands.Add(Instruction.CreateEntity());
            commands.Add(Instruction.SetParent(2));
            commands.Add(Instruction.AddComponent(new TestComponent(6)));
            commands.Add(Instruction.CreateList<char>());
            commands.Add(Instruction.AddElement<char>('a'));
            commands.Add(Instruction.AddElement<char>('b'));
            commands.Add(Instruction.AddElement<char>('c'));

            using World world = new();
            world.Perform(commands);

            eint firstEntity = world[0];
            eint secondEntity = world[1];
            eint thirdEntity = world[2];

            Assert.That(world.GetComponent<TestComponent>(firstEntity).value, Is.EqualTo(4));
            Assert.That(world.GetComponent<TestComponent>(secondEntity).value, Is.EqualTo(5));
            Assert.That(world.GetComponent<TestComponent>(thirdEntity).value, Is.EqualTo(6));
            Assert.That(world.GetList<char>(thirdEntity).AsSpan().ToString(), Is.EqualTo("abc"));
            Assert.That(world.GetParent(firstEntity), Is.EqualTo(default(eint)));
            Assert.That(world.GetParent(secondEntity), Is.EqualTo(default(eint)));
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
        public void AddSpanIntoList()
        {
            using UnmanagedList<Instruction> commands = new();
            commands.Add(Instruction.CreateEntity());
            commands.Add(Instruction.CreateList<char>());
            commands.Add(Instruction.AddElements("this is not an abacus".AsSpan()));

            using World world = new();
            world.Perform(commands);

            eint entity = world.Entities.First();
            UnmanagedList<char> list = world.GetList<char>(entity);
            Assert.That(list.AsSpan().ToString(), Is.EqualTo("this is not an abacus"));
        }
    }
}