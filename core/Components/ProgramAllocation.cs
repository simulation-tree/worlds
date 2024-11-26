using Unmanaged;

namespace Programs.Components
{
    /// <summary>
    /// Stores the allocation of a program.
    /// </summary>
    public readonly struct ProgramAllocation
    {
        /// <summary>
        /// The allocation of the program.
        /// </summary>
        public readonly Allocation value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramAllocation"/> struct.
        /// </summary>
        public ProgramAllocation(Allocation allocation)
        {
            this.value = allocation;
        }
    }
}