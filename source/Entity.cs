using System;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
{
    public struct Entity : IEntity, IEquatable<Entity>
    {
        private uint value;
        private World world;

        public readonly bool IsDestroyed
        {
            get
            {
                if (world == default) return true;
                return !world.ContainsEntity(value);
            }
        }

        public readonly bool IsEnabled
        {
            get => world.IsEnabled(value);
            set => world.SetEnabled(this.value, value);
        }

        public readonly Entity Parent
        {
            get
            {
                ThrowIfDestroyed();
                uint parent = world.GetParent(value);
                return parent == default ? default : new(world, parent);
            }
            set
            {
                ThrowIfDestroyed();
                world.SetParent(this.value, value);
            }
        }

        public readonly ReadOnlySpan<uint> Children
        {
            get
            {
                ThrowIfDestroyed();
                return world.GetChildren(value);
            }
        }

        readonly World IEntity.World => world;
        readonly uint IEntity.Value => value;

        public Entity(World world, uint existingEntity)
        {
            this.value = existingEntity;
            this.world = world;
        }

        public Entity(World world)
        {
            this.world = world;
            value = world.CreateEntity();
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfDestroyed()
        {
            if (IsDestroyed)
            {
                throw new InvalidOperationException($"Entity `{value}` is destroyed and no longer available.");
            }
        }

        readonly Query IEntity.GetQuery(World world)
        {
            return new Query(world);
        }

        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[32];
            int length = ToString(buffer);
            return new string(buffer[..length]);
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

        public readonly Span<T> GetArray<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.GetArray<T>(value);
        }

        public readonly ref T GetArrayElement<T>(uint index) where T : unmanaged
        {
            ThrowIfDestroyed();
            return ref world.GetArrayElement<T>(value, index);
        }

        public readonly uint GetArrayLength<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.GetArrayLength<T>(value);
        }

        public readonly bool ContainsArray<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.ContainsArray<T>(value);
        }

        public readonly void DestroyArray<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            world.DestroyArray<T>(value);
        }

        public readonly Span<T> CreateArray<T>(uint length) where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.CreateArray<T>(value, length);
        }

        public readonly void CreateArray<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            ThrowIfDestroyed();
            world.CreateArray(value, values);
        }

        public readonly Span<T> ResizeArray<T>(uint newLength) where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.ResizeArray<T>(value, newLength);
        }

        public readonly bool TryGetArray<T>(out Span<T> values) where T : unmanaged
        {
            ThrowIfDestroyed();
            if (ContainsArray<T>())
            {
                values = GetArray<T>();
                return true;
            }
            else
            {
                values = default;
                return false;
            }
        }

        public readonly bool TryGetParent(out uint parent)
        {
            ThrowIfDestroyed();
            parent = world.GetParent(value);
            return parent != default;
        }

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.ContainsComponent<T>(value);
        }

        public readonly bool ContainsComponent(RuntimeType componentType)
        {
            ThrowIfDestroyed();
            return world.ContainsComponent(value, componentType);
        }

        public readonly ReadOnlySpan<RuntimeType> GetComponentTypes()
        {
            ThrowIfDestroyed();
            return world.GetComponentTypes(value);
        }

        public readonly T GetComponent<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.GetComponent<T>(value);
        }

        public readonly ref T GetComponentRef<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return ref world.GetComponentRef<T>(value);
        }

        public readonly ref T AddComponentRef<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return ref world.AddComponentRef<T>(value);
        }

        public readonly ref T TryGetComponentRef<T>(out bool contains) where T : unmanaged
        {
            ThrowIfDestroyed();
            return ref world.TryGetComponentRef<T>(value, out contains);
        }

        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.TryGetComponent(value, out component);
        }

        /// <summary>
        /// Retrieves an existing component of type <typeparamref name="T"/>, or the default value.
        /// </summary>
        public readonly T GetComponent<T>(T defaultValue) where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.GetComponent(value, defaultValue);
        }

        public readonly void SetComponent<T>(T component) where T : unmanaged
        {
            ThrowIfDestroyed();
            world.SetComponent(value, component);
        }

        /// <summary>
        /// Adds a new <typeparamref name="T"/> component to the entity.
        /// </summary>
        public readonly void AddComponent<T>(T component) where T : unmanaged
        {
            ThrowIfDestroyed();
            world.AddComponent(value, component);
        }

        /// <summary>
        /// Adds a new component of the given type with uninitialized data.
        /// </summary>
        public readonly void AddComponent(RuntimeType componentType)
        {
            ThrowIfDestroyed();
            world.AddComponent(value, componentType);
        }

        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            world.RemoveComponent<T>(value);
        }

        public readonly void RemoveComponent<T>(out T removedComponent) where T : unmanaged
        {
            ThrowIfDestroyed();
            removedComponent = GetComponentRef<T>();
            world.RemoveComponent<T>(value);
        }

        public readonly rint AddReference(uint otherEntity)
        {
            ThrowIfDestroyed();
            return world.AddReference(value, otherEntity);
        }

        public readonly rint AddReference<T>(T otherEntity) where T : unmanaged, IEntity
        {
            ThrowIfDestroyed();
            return AddReference(otherEntity.Value);
        }

        public readonly uint GetReference(rint reference)
        {
            ThrowIfDestroyed();
            return world.GetReference(value, reference);
        }

        public readonly T GetReference<T>(rint reference) where T : unmanaged, IEntity
        {
            ThrowIfDestroyed();
            return new Entity(world, GetReference(reference)).As<T>();
        }

        /// <summary>
        /// Reassigns an existing reference to a different entity.
        /// </summary>
        public readonly void SetReference(rint reference, uint otherEntity)
        {
            ThrowIfDestroyed();
            world.SetReference(value, reference, otherEntity);
        }

        /// <summary>
        /// Reassigns an existing reference to a different entity.
        /// </summary>
        public readonly void SetReference<T>(rint reference, T otherEntity) where T : unmanaged, IEntity
        {
            ThrowIfDestroyed();
            SetReference(reference, otherEntity.Value);
        }

        public readonly bool ContainsReference(rint reference)
        {
            ThrowIfDestroyed();
            return world.ContainsReference(value, reference);
        }

        public readonly bool TryGetReference(rint reference, out uint otherEntity)
        {
            ThrowIfDestroyed();
            return world.TryGetReference(value, reference, out otherEntity);
        }

        public readonly bool TryGetReference<T>(rint reference, out T otherEntity) where T : unmanaged, IEntity
        {
            ThrowIfDestroyed();
            if (TryGetReference(reference, out uint otherValue))
            {
                otherEntity = new Entity(world, otherValue).As<T>();
                return true;
            }
            else
            {
                otherEntity = default;
                return false;
            }
        }

        /// <summary>
        /// Interprets the entity as <typeparamref name="T"/>.
        /// </summary>
        public readonly unsafe T As<T>() where T : unmanaged, IEntity
        {
            EntityFunctions.ThrowIfTypeLayoutMismatches(typeof(T));
            Entity self = this;
            return *(T*)&self;
        }

        /// <summary>
        /// Adds missing components that qualify the entity to be of type <typeparamref name="T"/>.
        /// </summary>
        public readonly T Become<T>() where T : unmanaged, IEntity
        {
            using Query query = default(T).GetQuery(world);
            foreach (RuntimeType type in query.Types)
            {
                if (!ContainsComponent(type))
                {
                    AddComponent(type);
                }
            }

            return As<T>();
        }

        public unsafe static bool TryGetFirst<T>(World world, out T entity) where T : unmanaged, IEntity
        {
            EntityFunctions.ThrowIfTypeLayoutMismatches(typeof(T));
            using Query query = default(T).GetQuery(world);
            query.Update();

            if (query.Count > 0)
            {
                uint firstEntity = query[0];
                entity = new Entity(world, firstEntity).As<T>();
                return true;
            }
            else
            {
                entity = default;
                return false;
            }
        }

        public static T GetFirst<T>(World world) where T : unmanaged, IEntity
        {
            EntityFunctions.ThrowIfTypeLayoutMismatches(typeof(T));
            using Query query = default(T).GetQuery(world);
            query.Update();

            if (query.Count > 0)
            {
                uint firstEntity = query[0];
                return new Entity(world, firstEntity).As<T>();
            }

            throw new NullReferenceException($"Component of type {typeof(T)} not found in world.");
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Entity entity && Equals(entity);
        }

        public readonly bool Equals(Entity other)
        {
            return value == other.value && world.Equals(other.world);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(value, world);
        }

        public static implicit operator uint(Entity entity)
        {
            return entity.value;
        }

        public static implicit operator World(Entity entity)
        {
            return entity.world;
        }

        public static bool operator ==(Entity left, Entity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }

        public static bool operator ==(Entity left, uint right)
        {
            return left.value == right;
        }

        public static bool operator !=(Entity left, uint right)
        {
            return left.value != right;
        }

        public static bool operator ==(uint left, Entity right)
        {
            return left == right.value;
        }

        public static bool operator !=(uint left, Entity right)
        {
            return left != right.value;
        }
    }
}
