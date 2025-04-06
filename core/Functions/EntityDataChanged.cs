using System;
using Unmanaged;

namespace Worlds.Functions
{
    /// <summary>
    /// Function pointer for when a component, array or tag was added or removed from an entity.
    /// </summary>
    public unsafe readonly struct EntityDataChanged : IEquatable<EntityDataChanged>
    {
#if NET
        private readonly delegate* unmanaged<Input, void> function;

        /// <inheritdoc/>
        public EntityDataChanged(delegate* unmanaged<Input, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Input, void> function;

        /// <inheritdoc/>
        public EntityDataChanged(delegate*<Input, void> function)
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
        public readonly void Invoke(World world, uint entity, DataType type, bool isPositive, ulong userData)
        {
            function(new(world, entity, type, isPositive, userData));
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

        /// <summary>
        /// Parameter type.
        /// </summary>
        public readonly struct Input
        {
            /// <summary>
            /// The world where the change occured.
            /// </summary>
            public readonly World world;
            
            /// <summary>
            /// The entity with the event.
            /// </summary>
            public readonly uint entity;

            /// <summary>
            /// The data type that was changed.
            /// </summary>
            public readonly DataType type;

            /// <summary>
            /// Indicates whether the component, array or tag was added or removed.
            /// </summary>
            public readonly Bool isPositive;

            /// <summary>
            /// Custom user data.
            /// </summary>
            public readonly ulong userData;

            /// <summary>
            /// Creates the input type.
            /// </summary>
            public Input(World world, uint entity, DataType type, Bool isPositive, ulong userData)
            {
                this.world = world;
                this.entity = entity;
                this.type = type;
                this.isPositive = isPositive;
                this.userData = userData;
            }
        }
    }
}