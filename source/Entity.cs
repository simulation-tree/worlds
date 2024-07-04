using System;
using System.Diagnostics;
using Unmanaged.Collections;

namespace Simulation
{
    public readonly struct Entity : IDisposable, IEntity
    {
        public readonly World world;

        private readonly uint value;

        public readonly bool IsDestroyed => !world.ContainsEntity(this);

        public readonly ReadOnlySpan<EntityID> Children
        {
            get
            {
                ThrowIfDestroyed();
                return world.GetChildren(this);
            }
        }

        public readonly EntityID Parent
        {
            get
            {
                ThrowIfDestroyed();
                return world.GetParent(this);
            }
            set
            {
                ThrowIfDestroyed();
                world.SetParent(this, value);
            }
        }

        World IEntity.World => world;
        EntityID IEntity.Value => new(value);

        public Entity()
        {
            throw new InvalidOperationException("Cannot create an entity without a world.");
        }

        public Entity(World world, EntityID existingEntity)
        {
            this.value = existingEntity.value;
            this.world = world;
        }

        public Entity(World world)
        {
            this.world = world;
            value = world.CreateEntity().value;
        }

        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[32];
            int length = ToString(buffer);
            return new string(buffer.Slice(0, length));
        }

        public readonly int ToString(Span<char> buffer)
        {
            int length = 0;
            if (IsDestroyed)
            {
                buffer[length++] = 'D';
                buffer[length++] = 'e';
                buffer[length++] = 's';
                buffer[length++] = 't';
                buffer[length++] = 'r';
                buffer[length++] = 'o';
                buffer[length++] = 'y';
                buffer[length++] = 'e';
                buffer[length++] = 'd';
                buffer[length++] = ' ';
            }

            value.TryFormat(buffer, out int valueLength);
            return length + valueLength;
        }

        /// <summary>
        /// Destroys the entity.
        /// </summary>
        public readonly void Dispose()
        {
            ThrowIfDestroyed();
            world.DestroyEntity(this);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDestroyed()
        {
            if (IsDestroyed)
            {
                throw new InvalidOperationException($"Entity {value} is destroyed.");
            }
        }

        public readonly void AddComponent<T>(T component) where T : unmanaged
        {
            world.AddComponent(this, component);
        }

        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            world.RemoveComponent<T>(this);
        }

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            return world.ContainsComponent<T>(this);
        }

        public readonly ref T GetComponentRef<T>() where T : unmanaged
        {
            return ref world.GetComponentRef<T>(this);
        }

        public readonly T GetComponent<T>() where T : unmanaged
        {
            return world.GetComponent<T>(this);
        }

        public readonly void SetComponent<T>(T component) where T : unmanaged
        {
            world.SetComponent(this, component);
        }

        public readonly UnmanagedList<T> CreateCollection<T>(uint initialCapacity = 1) where T : unmanaged
        {
            return world.CreateCollection<T>(this, initialCapacity);
        }

        public readonly void DestroyCollection<T>() where T : unmanaged
        {
            world.DestroyCollection<T>(this);
        }

        public readonly UnmanagedList<T> GetCollection<T>() where T : unmanaged
        {
            return world.GetCollection<T>(this);
        }

        public readonly bool ContainsCollection<T>() where T : unmanaged
        {
            return world.ContainsCollection<T>(this);
        }

        public static implicit operator EntityID(Entity entity)
        {
            return new(entity.value);
        }
    }
}