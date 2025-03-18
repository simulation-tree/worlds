using System;

namespace Worlds.Functions
{
    /// <summary>
    /// Function pointer for when an entity has been created or destroyed.
    /// </summary>
    public unsafe readonly struct EntityCreatedOrDestroyed : IEquatable<EntityCreatedOrDestroyed>
    {
#if NET
        private readonly delegate* unmanaged<World, uint, ChangeType, ulong, void> function;

        /// <inheritdoc/>
        public EntityCreatedOrDestroyed(delegate* unmanaged<World, uint, ChangeType, ulong, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<World, uint, ChangeType, ulong, void> function;
        
        /// <inheritdoc/>
        public EntityCreatedOrDestroyed(delegate*<World, uint, ChangeType, ulong, void> function)
        {
            this.function = function;
        }
#endif

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is EntityCreatedOrDestroyed created && Equals(created);
        }

        /// <inheritdoc/>
        public readonly bool Equals(EntityCreatedOrDestroyed other)
        {
            return (nint)function == (nint)other.function;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        /// <inheritdoc/>
        public readonly void Invoke(World world, uint entity, ChangeType changeType, ulong userData)
        {
            function(world, entity, changeType, userData);
        }

        /// <inheritdoc/>
        public static bool operator ==(EntityCreatedOrDestroyed left, EntityCreatedOrDestroyed right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(EntityCreatedOrDestroyed left, EntityCreatedOrDestroyed right)
        {
            return !(left == right);
        }
    }
}