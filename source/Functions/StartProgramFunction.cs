using Simulation;
using System;
using Unmanaged;

namespace Programs
{
    public unsafe readonly struct StartProgramFunction : IEquatable<StartProgramFunction>
    {
#if NET
        private readonly delegate* unmanaged<Simulator, Allocation, World, void> function;

        public StartProgramFunction(delegate* unmanaged<Simulator, Allocation, World, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Simulator, Allocation, World, void> function;

        public StartFunction(delegate*<Simulator, Allocation, World, void> function)
        {
            this.function = function;
        }
#endif

        public readonly void Invoke(Simulator simulator, Allocation allocation, World world)
        {
            function(simulator, allocation, world);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is StartProgramFunction function && Equals(function);
        }

        public readonly bool Equals(StartProgramFunction other)
        {
            nint a = (nint)function;
            nint b = (nint)other.function;
            return a == b;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        public static bool operator ==(StartProgramFunction left, StartProgramFunction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StartProgramFunction left, StartProgramFunction right)
        {
            return !(left == right);
        }
    }
}