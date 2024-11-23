using Simulation;
using System;
using Unmanaged;

namespace Programs
{
    public unsafe readonly struct UpdateProgramFunction : IEquatable<UpdateProgramFunction>
    {
#if NET
        private readonly delegate* unmanaged<Simulator, Allocation, World, TimeSpan, uint> function;

        public UpdateProgramFunction(delegate* unmanaged<Simulator, Allocation, World, TimeSpan, uint> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Simulator, Allocation, World, TimeSpan, uint> function;

        public UpdateProgramFunction(delegate*<Simulator, Allocation, World, TimeSpan, uint> function)
        {
            this.function = function;
        }
#endif

        public readonly uint Invoke(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
        {
            return function(simulator, allocation, world, delta);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is UpdateProgramFunction function && Equals(function);
        }

        public readonly bool Equals(UpdateProgramFunction other)
        {
            nint a = (nint)function;
            nint b = (nint)other.function;
            return a == b;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        public static bool operator ==(UpdateProgramFunction left, UpdateProgramFunction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UpdateProgramFunction left, UpdateProgramFunction right)
        {
            return !(left == right);
        }
    }
}