using System;

namespace Worlds.Functions
{
    /// <summary>
    /// Function pointer for when a component, array or tag was added or removed from an entity.
    /// </summary>
    public unsafe readonly struct EntityDataChanged : IEquatable<EntityDataChanged>
    {
#if NET
        private readonly delegate* unmanaged<World, uint, DataType, ChangeType, ulong, void> function;

        /// <inheritdoc/>
        public EntityDataChanged(delegate* unmanaged<World, uint, DataType, ChangeType, ulong, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<World, uint, DataType, ChangeType, ulong, void> function;
        
        /// <inheritdoc/>
        public EntityDataChanged(delegate*<World, uint, DataType, ChangeType, ulong, void> function)
        {
            this.function = function;
        }
#endif

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is EntityDataChanged added && Equals(added);
        }

        /// <inheritdoc/>
        public readonly bool Equals(EntityDataChanged other)
        {
            return (nint)function == (nint)other.function;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        /// <inheritdoc/>
        public readonly void Invoke(World world, uint entity, DataType type, ChangeType change, ulong userData)
        {
            function(world, entity, type, change, userData);
        }

        /// <inheritdoc/>
        public static bool operator ==(EntityDataChanged left, EntityDataChanged right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(EntityDataChanged left, EntityDataChanged right)
        {
            return !(left == right);
        }
    }
}