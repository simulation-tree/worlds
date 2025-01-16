using System;

namespace Worlds.Functions
{
    public unsafe readonly struct EntityParentChanged : IEquatable<EntityParentChanged>
    {
        private readonly delegate* unmanaged<World, uint, uint, uint, ulong, void> function;
        public EntityParentChanged(delegate* unmanaged<World, uint, uint, uint, ulong, void> function)
        {
            this.function = function;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is EntityParentChanged changed && Equals(changed);
        }

        public readonly bool Equals(EntityParentChanged other)
        {
            return (nint)function == (nint)other.function;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        public readonly void Invoke(World world, uint entity, uint oldParent, uint newParent, ulong userData)
        {
            function(world, entity, oldParent, newParent, userData);
        }

        public static bool operator ==(EntityParentChanged left, EntityParentChanged right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityParentChanged left, EntityParentChanged right)
        {
            return !(left == right);
        }
    }
}