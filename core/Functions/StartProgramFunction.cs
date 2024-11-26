using Simulation;
using System;
using Unmanaged;

namespace Programs
{
    /// <summary>
    /// A function that starts a program.
    /// </summary>
    public unsafe readonly struct StartProgramFunction : IEquatable<StartProgramFunction>
    {
#if NET
        private readonly delegate* unmanaged<Simulator, Allocation, World, void> function;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartProgramFunction"/> struct.
        /// </summary>
        public StartProgramFunction(delegate* unmanaged<Simulator, Allocation, World, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Simulator, Allocation, World, void> function;

        public StartProgramFunction(delegate*<Simulator, Allocation, World, void> function)
        {
            this.function = function;
        }
#endif
        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly void Invoke(Simulator simulator, Allocation allocation, World world)
        {
            function(simulator, allocation, world);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is StartProgramFunction function && Equals(function);
        }

        /// <inheritdoc/>
        public readonly bool Equals(StartProgramFunction other)
        {
            nint a = (nint)function;
            nint b = (nint)other.function;
            return a == b;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(StartProgramFunction left, StartProgramFunction right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(StartProgramFunction left, StartProgramFunction right)
        {
            return !(left == right);
        }
    }
}