namespace Simulation.Functions
{
    public unsafe readonly struct InitializeFunction
    {
#if NET
        private readonly delegate* unmanaged<SystemContainer, World, void> value;

        public InitializeFunction(delegate* unmanaged<SystemContainer, World, void> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<SystemContainer, World, void> value;

        public InitializeFunction(delegate*<SystemContainer, World, void> value)
        {
            this.value = value;
        }
#endif
        public readonly void Invoke(SystemContainer container, World world)
        {
            value(container, world);
        }
    }
}