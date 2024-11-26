using Programs;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Unmanaged.Tests;

namespace Simulation.Tests
{
    public class ProgramTests : UnmanagedTests
    {
        [Test]
        public void SimpleProgram()
        {
            using World world = new();
            using Simulator simulator = new(world);
            Program program = Program.Create<Calculator>(world);

            Assert.That(program.State, Is.EqualTo(ProgramState.Uninitialized));

            uint returnCode;
            do
            {
                simulator.Update();

                ref Calculator calculator = ref program.Read<Calculator>();
                Console.WriteLine(calculator.value);
                Assert.That(program.State, Is.Not.EqualTo(ProgramState.Uninitialized));
            }
            while (!program.IsFinished(out returnCode));

            Assert.That(program.State, Is.EqualTo(ProgramState.Finished));
            Assert.That(returnCode, Is.EqualTo(1337u));
        }

        [Test]
        public void ExitEarly()
        {
            using World world = new();
            using Simulator simulator = new(world);
            Program program = Program.Create<Calculator>(world);

            Assert.That(program.State, Is.EqualTo(ProgramState.Uninitialized));

            simulator.Update(); //to invoke the initializer and update
            ref Calculator calculator = ref program.Read<Calculator>();

            Assert.That(calculator.state.ToString(), Is.EqualTo("Running2"));
            program.Dispose();
            simulator.Update(); //to invoke the finisher

            Assert.That(calculator.value, Is.EqualTo(2));
            Assert.That(calculator.state.ToString(), Is.EqualTo("Finished0"));
        }

        public struct Calculator : IProgram
        {
            public byte value;
            public FixedString state;

            unsafe readonly StartProgramFunction IProgram.Start => new(&Start);
            unsafe readonly UpdateProgramFunction IProgram.Update => new(&Update);
            unsafe readonly FinishProgramFunction IProgram.Finish => new(&Finish);

            [UnmanagedCallersOnly]
            private static void Start(Simulator simulator, Allocation allocation, World world)
            {
                allocation.Write(new Calculator());
            }

            [UnmanagedCallersOnly]
            private static uint Update(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
            {
                ref Calculator calculator = ref allocation.Read<Calculator>();
                calculator.value += 2;
                calculator.state = "Running";
                calculator.state.Append(calculator.value);

                uint newEntity = world.CreateEntity();
                world.AddComponent(newEntity, true);
                if (world.Count >= 4)
                {
                    return 1337;
                }
                else
                {
                    return default;
                }
            }

            [UnmanagedCallersOnly]
            private static void Finish(Simulator simulator, Allocation allocation, World world, uint returnCode)
            {
                ref Calculator calculator = ref allocation.Read<Calculator>();
                calculator.state = "Finished";
                calculator.state.Append(returnCode);
            }
        }
    }
}
