using System;

namespace Worlds.Functions
{
    public unsafe readonly struct EntityDataChanged : IEquatable<EntityDataChanged>
    {
#if NET
        private readonly delegate* unmanaged<World, uint, DataType, ChangeType, ulong, void> function;

        public EntityDataChanged(delegate* unmanaged<World, uint, DataType, ChangeType, ulong, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<World, uint, DataType, ChangeType, ulong, void> function;

        public EntityDataChanged(delegate*<World, uint, DataType, ChangeType, ulong, void> function)
        {
            this.function = function;
        }
#endif

        public readonly override bool Equals(object? obj)
        {
            return obj is EntityDataChanged added && Equals(added);
        }

        public readonly bool Equals(EntityDataChanged other)
        {
            return (nint)function == (nint)other.function;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        public readonly void Invoke(World world, uint entity, DataType type, ChangeType change, ulong userData)
        {
            function(world, entity, type, change, userData);
        }

        public static bool operator ==(EntityDataChanged left, EntityDataChanged right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityDataChanged left, EntityDataChanged right)
        {
            return !(left == right);
        }
    }
}