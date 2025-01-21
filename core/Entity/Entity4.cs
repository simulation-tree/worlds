using System;
using Unmanaged;

namespace Worlds
{
    public readonly struct Entity<C1, C2, C3, C4> : IEntity, IEquatable<Entity<C1, C2, C3, C4>> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
    {
        private readonly Entity entity;

        readonly World IEntity.World => entity.world;
        readonly uint IEntity.Value => entity.value;

        readonly void IEntity.Describe(ref Archetype archetype)
        {
            archetype.AddComponentType<C1>();
            archetype.AddComponentType<C2>();
            archetype.AddComponentType<C3>();
            archetype.AddComponentType<C4>();
        }

        public Entity(World world, uint existingEntity)
        {
            entity = new(world, existingEntity);
        }

        public Entity(World world)
        {
            entity = new(world, world.CreateEntity(default(C1), default(C2), default(C3), default(C4)));
        }

        public Entity(World world, C1 c1, C2 c2, C3 c3, C4 c4)
        {
            entity = new(world, world.CreateEntity(c1, c2, c3, c4));
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
            return obj is Entity<C1, C2, C3, C4> other && Equals(other);
        }

        public readonly bool Equals(Entity<C1, C2, C3, C4> other)
        {
            return entity.Equals(other.entity);
        }

        public static bool operator ==(Entity<C1, C2, C3, C4> left, Entity<C1, C2, C3, C4> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity<C1, C2, C3, C4> left, Entity<C1, C2, C3, C4> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Entity(Entity<C1, C2, C3, C4> entity)
        {
            return entity.entity;
        }
    }
}