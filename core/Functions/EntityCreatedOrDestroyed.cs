using System;

namespace Worlds.Functions
{
    public unsafe readonly struct EntityCreatedOrDestroyed : IEquatable<EntityCreatedOrDestroyed>
    {
#if NET
        private readonly delegate* unmanaged<World, uint, ChangeType, ulong, void> function;

        public EntityCreatedOrDestroyed(delegate* unmanaged<World, uint, ChangeType, ulong, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<World, uint, ChangeType, ulong, void> function;

        public EntityCreatedOrDestroyed(delegate*<World, uint, ChangeType, ulong, void> function)
        {
            this.function = function;
        }
#endif

        public readonly override bool Equals(object? obj)
        {
            return obj is EntityCreatedOrDestroyed created && Equals(created);
        }

        public readonly bool Equals(EntityCreatedOrDestroyed other)
        {
            return (nint)function == (nint)other.function;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        public readonly void Invoke(World world, uint entity, ChangeType changeType, ulong userData)
        {
            function(world, entity, changeType, userData);
        }

        public static bool operator ==(EntityCreatedOrDestroyed left, EntityCreatedOrDestroyed right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityCreatedOrDestroyed left, EntityCreatedOrDestroyed right)
        {
            return !(left == right);
        }
    }
}