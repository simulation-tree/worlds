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
            using UnmanagedList<Command> commands = new();
            commands.Add(Command.CreateEntity());
            commands.Add(Command.AddComponent(new TestComponent(1337)));

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

            using UnmanagedList<Command> commands = new();
            foreach (eint entity in world.Entities)
            {
                commands.Add(Command.AddToSelection(entity));
            }

            commands.RemoveAt(0);
            commands.Add(Command.DestroySelection());

            world.Perform(commands);

            Assert.That(world.Count, Is.EqualTo(1));
            Assert.That((uint)world.Entities.First(), Is.EqualTo(1));
        }

        [Test]
        public void CreateManyWithData()
        {
            using World world = new();
            using UnmanagedList<Command> commands = new();
            commands.Add(Command.CreateEntity(40));
            commands.Add(Command.AddComponent(new TestComponent(2)));
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
            using UnmanagedList<Command> commands = new();
            commands.Add(Command.CreateEntity());
            commands.Add(Command.AddComponent(new TestComponent(4)));

            commands.Add(Command.CreateEntity());
            commands.Add(Command.AddComponent(new TestComponent(5)));

            commands.Add(Command.CreateEntity());
            commands.Add(Command.SetParent(2));
            commands.Add(Command.AddComponent(new TestComponent(6)));
            commands.Add(Command.CreateList<char>());
            commands.Add(Command.AddElement<char>('a'));
            commands.Add(Command.AddElement<char>('b'));
            commands.Add(Command.AddElement<char>('c'));

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
    }
}