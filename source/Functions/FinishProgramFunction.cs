using Simulation;
using System;
using Unmanaged;

namespace Programs
{
    /// <summary>
    /// A function that finishes a program.
    /// </summary>
    public unsafe readonly struct FinishProgramFunction : IEquatable<FinishProgramFunction>
    {
#if NET
        private readonly delegate* unmanaged<Simulator, Allocation, World, uint, void> function;

        /// <summary>
        /// Creates a new <see cref="FinishProgramFunction"/>.
        /// </summary>
        public FinishProgramFunction(delegate* unmanaged<Simulator, Allocation, World, uint, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Simulator, Allocation, World, uint, void> function;

        public FinishProgramFunction(delegate*<Simulator, Allocation, World, uint, void> function)
        {
            this.function = function;
        }
#endif

        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly void Invoke(Simulator simulator, Allocation allocation, World world, uint returnCode)
        {
            function(simulator, allocation, world, returnCode);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is FinishProgramFunction function && Equals(function);
        }

        /// <inheritdoc/>
        public readonly bool Equals(FinishProgramFunction other)
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
        public static bool operator ==(FinishProgramFunction left, FinishProgramFunction right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(FinishProgramFunction left, FinishProgramFunction right)
        {
            return !(left == right);
        }
    }
}