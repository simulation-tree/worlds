using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public struct Query : IDisposable
    {
        public Option options;

        private readonly World world;
        private readonly UnmanagedList<EntityID> entities;
        private readonly UnmanagedArray<RuntimeType> types;

        public readonly bool IsDisposed => types.IsDisposed;
        public readonly uint Count => entities.Count;

        public Query(World world, ReadOnlySpan<RuntimeType> types, Option options = default)
        {
            this.world = world;
            this.types = new(types);
            this.options = options;
            entities = new();
        }

        public readonly void Dispose()
        {
            entities.Dispose();
            types.Dispose();
        }

        /// <summary>
        /// Updates the buffers inside the query to be up to date.
        /// </summary>
        public readonly void Update()
        {
            entities.Clear();
            bool exact = (options & Option.ExactComponentTypes) == Option.ExactComponentTypes;
            bool includeDisabled = (options & Option.IncludeDisabledEntities) == Option.IncludeDisabledEntities;
            UnmanagedDictionary<uint, ComponentChunk> componentChunks = world.ComponentChunks;
            for (int i = 0; i < componentChunks.Count; i++)
            {
                ComponentChunk chunk = componentChunks.Values[i];
                if (chunk.ContainsTypes(types.AsSpan(), exact))
                {
                    if (includeDisabled)
                    {
                        entities.AddRange(chunk.Entities);
                    }
                    else
                    {
                        UnmanagedList<EntityID> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            EntityID entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                this.entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly Result Get(uint index)
        {
            return new(entities[index], world);
        }

        public readonly struct Result
        {
            public readonly EntityID entity;
            public readonly World world;

            internal Result(EntityID entity, World world)
            {
                this.entity = entity;
                this.world = world;
            }

            public readonly bool ContainsComponent<T>() where T : unmanaged
            {
                return world.ContainsComponent<T>(entity);
            }

            public readonly ref T GetComponentRef<T>() where T : unmanaged
            {
                return ref world.GetComponentRef<T>(entity);
            }
        }

        [Flags]
        public enum Option : byte
        {
            Default = 0,
            ExactComponentTypes = 1,
            IncludeDisabledEntities = 2
        }
    }

    public readonly struct Query<T1> : IDisposable where T1 : unmanaged
    {
        private readonly Query query;

        public readonly bool IsDisposed => query.IsDisposed;
        public readonly uint Count => query.Count;

        public Query(World world, Query.Option options = default)
        {
            this.query = new(world, [RuntimeType.Get<T1>()], options);
        }

        public readonly void Dispose()
        {
            query.Dispose();
        }

        /// <summary>
        /// Updates the buffers inside the query to be up to date.
        /// </summary>
        public unsafe readonly void Update()
        {
            query.Update();
        }

        public readonly Query.Result Get(uint index)
        {
            return query.Get(index);
        }
    }

    public readonly struct Query<T1, T2> : IDisposable where T1 : unmanaged where T2 : unmanaged
    {
        private readonly Query query;

        public readonly bool IsDisposed => query.IsDisposed;
        public readonly uint Count => query.Count;

        public Query(World world, Query.Option options = default)
        {
            this.query = new(world, [RuntimeType.Get<T1>(), RuntimeType.Get<T2>()], options);
        }

        public readonly void Dispose()
        {
            query.Dispose();
        }

        /// <summary>
        /// Updates the buffers inside the query to be up to date.
        /// </summary>
        public unsafe readonly void Update()
        {
            query.Update();
        }

        public readonly Query.Result Get(uint index)
        {
            return query.Get(index);
        }
    }
}
