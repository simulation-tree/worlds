using System;

namespace Worlds.Functions
{
    public unsafe readonly struct EntityParentChanged : IEquatable<EntityParentChanged>
    {
#if NET
        private readonly delegate* unmanaged<Input, void> function;
        
        public EntityParentChanged(delegate* unmanaged<Input, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Input, void> function;

        public EntityParentChanged(delegate*<Input, void> function)
        {
            this.function = function;
        }
#endif

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
            Input input = new(world, entity, oldParent, newParent, userData);
            function(input);
        }

        public static bool operator ==(EntityParentChanged left, EntityParentChanged right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityParentChanged left, EntityParentChanged right)
        {
            return !(left == right);
        }

        public readonly struct Input
        {
            public readonly World world;
            public readonly uint entity;
            public readonly uint oldParent;
            public readonly uint newParent;
            public readonly ulong userData;

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