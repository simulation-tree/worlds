using Simulation;
using System;
using Unmanaged;

namespace Programs.Functions
{
    public unsafe readonly struct FinishFunction : IEquatable<FinishFunction>
    {
#if NET
        private readonly delegate* unmanaged<Simulator, Allocation, World, uint, void> function;

        public FinishFunction(delegate* unmanaged<Simulator, Allocation, World, uint, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Simulator, Allocation, World, uint, void> function;

        public FinishFunction(delegate*<Simulator, Allocation, World, uint, void> function)
        {
            this.function = function;
        }
#endif

        public readonly void Invoke(Simulator simulator, Allocation allocation, World world, uint returnCode)
        {
            function(simulator, allocation, world, returnCode);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is FinishFunction function && Equals(function);
        }

        public readonly bool Equals(FinishFunction other)
        {
            nint a = (nint)function;
            nint b = (nint)other.function;
            return a == b;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        public static bool operator ==(FinishFunction left, FinishFunction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FinishFunction left, FinishFunction right)
        {
            return !(left == right);
        }
    }
}