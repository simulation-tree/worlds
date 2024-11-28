using Programs.Components;
using Simulation;
using Unmanaged;

namespace Programs.System
{
    /// <summary>
    /// Container for a program running in a <see cref="World"/>,
    /// operated by a <see cref="Simulator"/>.
    /// </summary>
    public struct ProgramContainer
    {
        /// <summary>
        /// The function to start the program.
        /// </summary>
        public readonly StartProgramFunction start;

        /// <summary>
        /// The function to finish the program.
        /// </summary>
        public readonly FinishProgramFunction finish;

        /// <summary>
        /// The function to update the program.
        /// </summary>
        public readonly UpdateProgramFunction update;

        /// <summary>
        /// The <see cref="World"/> that belongs to this program.
        /// </summary>
        public readonly World programWorld;

        /// <summary>
        /// The entity that initialized this program.
        /// </summary>
        public readonly Entity program;

        /// <summary>
        /// Native memory containing the program's data.
        /// </summary>
        public readonly Allocation allocation;

        /// <summary>
        /// Whether the program has finished running.
        /// </summary>
        public bool finished;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramContainer"/> struct.
        /// </summary>
        public ProgramContainer(IsProgram component, World programWorld, Entity program, Allocation allocation)
        {
            this.start = component.start;
            this.finish = component.finish;
            this.update = component.update;
            this.programWorld = programWorld;
            this.program = program;
            this.allocation = allocation;
        }
    }
}