﻿using System;
using System.Diagnostics;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    public struct Entity : IEntity, IDisposable
    {
        private eint value;
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

        public readonly eint Parent
        {
            get
            {
                ThrowIfDestroyed();
                return world.GetParent(value);
            }
            set
            {
                ThrowIfDestroyed();
                world.SetParent(this.value, value);
            }
        }

        public readonly ReadOnlySpan<eint> Children
        {
            get
            {
                ThrowIfDestroyed();
                return world.GetChildren(value);
            }
        }

        readonly World IEntity.World => world;
        readonly eint IEntity.Value => value;

        public Entity(World world, eint existingEntity)
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

        /// <summary>
        /// Destroys the entity.
        /// </summary>
        public readonly void Dispose()
        {
            //todo: should this exist? should Dispose be removed so that its only Destroy? ditch it?
            //^^^ if c# had a warning for undisposed values, yes, it would all fall into place like a jigsaw puzzle
            ThrowIfDestroyed();
            world.DestroyEntity(value);
        }

        public readonly UnmanagedList<T> GetList<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.GetList<T>(value);
        }

        public readonly ref T GetListElement<T>(uint index) where T : unmanaged
        {
            ThrowIfDestroyed();
            return ref world.GetListElement<T>(value, index);
        }

        public readonly uint GetListLength<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.GetListLength<T>(value);
        }

        public readonly bool ContainsList<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.ContainsList<T>(value);
        }

        public readonly void DestroyList<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            world.DestroyList<T>(value);
        }

        public readonly UnmanagedList<T> CreateList<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.CreateList<T>(value);
        }

        public readonly UnmanagedList<T> CreateList<T>(uint initialCapacity) where T : unmanaged
        {
            ThrowIfDestroyed();
            return world.CreateList<T>(value, initialCapacity);
        }

        public readonly bool TryGetList<T>(out UnmanagedList<T> list) where T : unmanaged
        {
            ThrowIfDestroyed();
            if (ContainsList<T>())
            {
                list = GetList<T>();
                return true;
            }
            else
            {
                list = default;
                return false;
            }
        }

        public readonly bool TryGetParent(out eint parent)
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

        public readonly ref T GetComponent<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return ref world.GetComponentRef<T>(value);
        }

        public readonly ref T AddComponentRef<T>() where T : unmanaged
        {
            ThrowIfDestroyed();
            return ref world.AddComponentRef<T>(value);
        }

        public readonly ref T TryGetComponentRef<T>(out bool has) where T : unmanaged
        {
            ThrowIfDestroyed();
            return ref world.TryGetComponentRef<T>(value, out has);
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
            removedComponent = GetComponent<T>();
            world.RemoveComponent<T>(value);
        }

        public readonly rint AddReference(eint otherEntity)
        {
            ThrowIfDestroyed();
            return world.AddReference(value, otherEntity);
        }

        public readonly rint AddReference<T>(T otherEntity) where T : unmanaged, IEntity
        {
            ThrowIfDestroyed();
            return AddReference(otherEntity.Value);
        }

        public readonly eint GetReference(rint reference)
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
        public readonly void SetReference(rint reference, eint otherEntity)
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

        public readonly bool TryGetReference(rint reference, out eint otherEntity)
        {
            ThrowIfDestroyed();
            return world.TryGetReference(value, reference, out otherEntity);
        }

        public readonly bool TryGetReference<T>(rint reference, out T otherEntity) where T : unmanaged, IEntity
        {
            ThrowIfDestroyed();
            if (TryGetReference(reference, out eint otherValue))
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

        public unsafe static bool TryFindFirst<T>(World world, out T entity) where T : unmanaged, IEntity
        {
            EntityFunctions.ThrowIfTypeLayoutMismatches(typeof(T));
            using Query query = new T().GetQuery(world);
            query.Update();

            if (query.Count > 0)
            {
                eint firstEntity = query[0];
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
            using Query query = new T().GetQuery(world);
            query.Update();

            if (query.Count > 0)
            {
                eint firstEntity = query[0];
                return new Entity(world, firstEntity).As<T>();
            }

            throw new NullReferenceException($"Component of type {typeof(T)} not found in world.");
        }

        public static implicit operator eint(Entity entity)
        {
            return entity.value;
        }

        public static implicit operator World(Entity entity)
        {
            return entity.world;
        }
    }
}
