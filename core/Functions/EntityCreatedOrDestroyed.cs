using System;
using Unmanaged;

namespace Worlds.Functions
{
    /// <summary>
    /// Function pointer for when an entity has been created or destroyed.
    /// </summary>
    public unsafe readonly struct EntityCreatedOrDestroyed : IEquatable<EntityCreatedOrDestroyed>
    {
#if NET
        private readonly delegate* unmanaged<Input, void> function;

        /// <inheritdoc/>
        public EntityCreatedOrDestroyed(delegate* unmanaged<Input, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Input, void> function;
        
        /// <inheritdoc/>
        public EntityCreatedOrDestroyed(delegate*<Input, void> function)
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
        public readonly void Invoke(World world, uint entity, bool isPositive, ulong userData)
        {
            function(new(world, entity, isPositive, userData));
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

        /// <inheritdoc/>
        public readonly struct Input
        {
            /// <summary>
            /// The world in which the entity was created or destroyed.
            /// </summary>
            public readonly World world;

            /// <summary>
            /// The entity.
            /// </summary>
            public readonly uint entity;

            /// <summary>
            /// Indicates whether the entity was created or destroyed.
            /// </summary>
            public readonly Bool isCreated;

            /// <summary>
            /// Custom user data.
            /// </summary>
            public readonly ulong userData;

            /// <summary>
            /// Creates a new input parameter.
            /// </summary>
            public Input(World world, uint entity, Bool isCreated, ulong userData)
            {
                this.world = world;
                this.entity = entity;
                this.isCreated = isCreated;
                this.userData = userData;
            }
        }
    }
}