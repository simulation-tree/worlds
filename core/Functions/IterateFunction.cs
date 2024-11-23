using System;

namespace Simulation.Functions
{
    public unsafe readonly struct IterateFunction
    {
#if NET
        private readonly delegate* unmanaged<SystemContainer, World, TimeSpan, void> value;

        public IterateFunction(delegate* unmanaged<SystemContainer, World, TimeSpan, void> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<SystemContainer, World, TimeSpan, void> value;

        public IterateFunction(delegate*<SystemContainer, World, TimeSpan, void> value)
        {
            this.value = value;
        }
#endif
        public readonly void Invoke(SystemContainer container, World world, TimeSpan delta)
        {
            value(container, world, delta);
        }
    }
}