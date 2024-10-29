using Programs.Components;
using Programs.Functions;
using Simulation;
using Unmanaged;

namespace Programs.System
{
    public struct ProgramContainer
    {
        public readonly StartFunction start;
        public readonly FinishFunction finish;
        public readonly UpdateFunction update;
        public readonly RuntimeType type;
        public readonly World programWorld;
        public readonly Entity program;
        public readonly Allocation allocation;
        public bool finished;

        public ProgramContainer(IsProgram component, World programWorld, Entity program, Allocation allocation)
        {
            this.start = component.start;
            this.finish = component.finish;
            this.update = component.update;
            this.type = component.type;
            this.programWorld = programWorld;
            this.program = program;
            this.allocation = allocation;
        }
    }
}