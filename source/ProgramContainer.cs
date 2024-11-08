using Programs.Components;
using Simulation;
using Unmanaged;

namespace Programs.System
{
    public struct ProgramContainer
    {
        public readonly StartProgramFunction start;
        public readonly FinishProgramFunction finish;
        public readonly UpdateProgramFunction update;
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