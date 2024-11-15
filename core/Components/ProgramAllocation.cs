using Unmanaged;

namespace Programs.Components
{
    public readonly struct ProgramAllocation
    {
        public readonly Allocation value;

        public ProgramAllocation(Allocation allocation)
        {
            this.value = allocation;
        }
    }
}