using System;

namespace Worlds.Functions
{
    /// <summary>
    /// Function pointer for when the parent of an entity changes.
    /// </summary>
    public unsafe readonly struct EntityParentChanged : IEquatable<EntityParentChanged>
    {
#if NET
        private readonly delegate* unmanaged<Input, void> function;

        /// <inheritdoc/>
        public EntityParentChanged(delegate* unmanaged<Input, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Input, void> function;
        
        /// <inheritdoc/>
        public EntityParentChanged(delegate*<Input, void> function)
        {
            this.function = function;
        }
#endif

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is EntityParentChanged changed && Equals(changed);
        }

        /// <inheritdoc/>
        public readonly bool Equals(EntityParentChanged other)
        {
            return (nint)function == (nint)other.function;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        /// <inheritdoc/>
        public readonly void Invoke(World world, uint entity, uint oldParent, uint newParent, ulong userData)
        {
            Input input = new(world, entity, oldParent, newParent, userData);
            function(input);
        }

        /// <inheritdoc/>
        public static bool operator ==(EntityParentChanged left, EntityParentChanged right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(EntityParentChanged left, EntityParentChanged right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public readonly struct Input
        {
            /// <summary>
            /// The world that the event belongs to.
            /// </summary>
            public readonly World world;

            /// <summary>
            /// The entity where the assigned parent has changed.
            /// </summary>
            public readonly uint entity;

            /// <summary>
            /// Previous value.
            /// </summary>
            public readonly uint oldParent;

            /// <summary>
            /// New value.
            /// </summary>
            public readonly uint newParent;

            /// <summary>
            /// Custom user data specified when subscribing to the event.
            /// </summary>
            public readonly ulong userData;

            /// <inheritdoc/>
            public Input(World world, uint entity, uint oldParent, uint newParent, ulong userData)
            {
                this.world = world;
                this.entity = entity;
                this.oldParent = oldParent;
                this.newParent = newParent;
                this.userData = userData;
            }
        }
    }
}