using System;
using Unmanaged;

namespace Worlds
{
    public readonly struct Entity<C1> : IEntity, IEquatable<Entity<C1>> where C1 : unmanaged
    {
        private readonly Entity entity;

        readonly World IEntity.World => entity.world;
        readonly uint IEntity.Value => entity.value;

        readonly void IEntity.Describe(ref Archetype archetype)
        {
            archetype.AddComponentType<C1>();
        }

        public Entity(World world, uint existingEntity)
        {
            entity = new(world, existingEntity);
        }

        public Entity(World world)
        {
            entity = new(world, world.CreateEntity(default(C1)));
        }

        public Entity(World world, C1 c1)
        {
            entity = new(world, world.CreateEntity(c1));
        }

        public readonly void Dispose()
        {
            entity.Dispose();
        }

        public readonly override string ToString()
        {
            return entity.ToString();
        }

        public readonly uint ToString(USpan<char> buffer)
        {
            return entity.ToString(buffer);
        }

        public readonly override int GetHashCode()
        {
            return entity.GetHashCode();
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Entity<C1> other && Equals(other);
        }

        public readonly bool Equals(Entity<C1> other)
        {
            return entity.Equals(other.entity);
        }

        public static bool operator ==(Entity<C1> left, Entity<C1> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity<C1> left, Entity<C1> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Entity(Entity<C1> entity)
        {
            return entity.entity;
        }
    }
}