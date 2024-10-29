using Simulation;
using System;
using Unmanaged;

namespace Programs.Functions
{
    public unsafe readonly struct UpdateFunction : IEquatable<UpdateFunction>
    {
#if NET
        private readonly delegate* unmanaged<Simulator, Allocation, World, TimeSpan, uint> function;

        public UpdateFunction(delegate* unmanaged<Simulator, Allocation, World, TimeSpan, uint> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Simulator, Allocation, World, TimeSpan, uint> function;

        public UpdateFunction(delegate*<Simulator, Allocation, World, TimeSpan, uint> function)
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
            return obj is UpdateFunction function && Equals(function);
        }

        public readonly bool Equals(UpdateFunction other)
        {
            nint a = (nint)function;
            nint b = (nint)other.function;
            return a == b;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        public static bool operator ==(UpdateFunction left, UpdateFunction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UpdateFunction left, UpdateFunction right)
        {
            return !(left == right);
        }
    }
}