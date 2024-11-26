using System;

namespace Simulation.Functions
{
    /// <summary>
    /// A function that iterates over a system.
    /// </summary>
    public unsafe readonly struct IterateFunction
    {
#if NET
        private readonly delegate* unmanaged<SystemContainer, World, TimeSpan, void> value;

        /// <summary>
        /// Creates a new iterate function.
        /// </summary>
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
        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly void Invoke(SystemContainer container, World world, TimeSpan delta)
        {
            value(container, world, delta);
        }
    }
}