using System;

namespace Worlds.Functions
{
    public unsafe readonly struct EntityDataChanged : IEquatable<EntityDataChanged>
    {
        private readonly delegate* unmanaged<World, uint, byte, DataType, ChangeType, ulong, void> function;

        public EntityDataChanged(delegate* unmanaged<World, uint, byte, DataType, ChangeType, ulong, void> function)
        {
            this.function = function;
        }

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

        public readonly void Invoke(World world, uint entity, byte type, DataType dataType, ChangeType change, ulong userData)
        {
            function(world, entity, type, dataType, change, userData);
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