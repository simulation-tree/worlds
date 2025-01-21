using System;
using Unmanaged;

namespace Worlds
{
    public readonly struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> : IEntity, IEquatable<Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
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
            archetype.AddComponentType<C5>();
            archetype.AddComponentType<C6>();
            archetype.AddComponentType<C7>();
            archetype.AddComponentType<C8>();
            archetype.AddComponentType<C9>();
            archetype.AddComponentType<C10>();
            archetype.AddComponentType<C11>();
            archetype.AddComponentType<C12>();
            archetype.AddComponentType<C13>();
        }

        public Entity(World world, uint existingEntity)
        {
            entity = new(world, existingEntity);
        }

        public Entity(World world)
        {
            entity = new(world, world.CreateEntity(default(C1), default(C2), default(C3), default(C4), default(C5), default(C6), default(C7), default(C8), default(C9), default(C10), default(C11), default(C12), default(C13)));
        }

        public Entity(World world, C1 c1, C2 c2, C3 c3, C4 c4, C5 c5, C6 c6, C7 c7, C8 c8, C9 c9, C10 c10, C11 c11, C12 c12, C13 c13)
        {
            entity = new(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13));
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
            return obj is Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> other && Equals(other);
        }

        public readonly bool Equals(Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> other)
        {
            return entity.Equals(other.entity);
        }

        public static bool operator ==(Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> left, Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> left, Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Entity(Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> entity)
        {
            return entity.entity;
        }
    }
}