﻿using System;
using System.Diagnostics;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    public readonly struct Query : IDisposable
    {
        private readonly QueryState state;
        private readonly World world;
        private readonly UnmanagedList<uint> entities;
        private readonly UnmanagedArray<RuntimeType> types;

        public readonly bool IsDisposed => state.IsDisposed;

        /// <summary>
        /// The component types used to filter with.
        /// </summary>
        public readonly ReadOnlySpan<RuntimeType> Types => types.AsSpan();

        public readonly uint TypeCount => types.Length;

        /// <summary>
        /// All entities found after updating the state.
        /// </summary>
        public readonly uint Count
        {
            get
            {
                ThrowIfNotUpdated();
                return entities.Count;
            }
        }

        public readonly Result this[uint index]
        {
            get
            {
                ThrowIfNotUpdated();
                return new(entities[index], world);
            }
        }

#if NET5_0_OR_GREATER
        [Obsolete("Default constructor not available", true)]
        public Query()
        {
            throw new NotImplementedException();
        }
#endif

        /// <summary>
        /// Creates a new query that is yet to be updated.
        /// </summary>
        public Query(World world)
        {
            this.world = world;
            this.types = UnmanagedArray<RuntimeType>.Create();
            entities = UnmanagedList<uint>.Create();
            state = QueryState.Create();
        }

        public Query(World world, ReadOnlySpan<RuntimeType> types)
        {
            this.world = world;
            this.types = new(types);
            entities = UnmanagedList<uint>.Create();
            state = QueryState.Create();
        }

        public Query(World world, RuntimeType type)
        {
            this.world = world;
            this.types = UnmanagedArray<RuntimeType>.Create(1);
            this.types.Set(0, type);

            entities = UnmanagedList<uint>.Create();
            state = QueryState.Create();
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfNotUpdated()
        {
            state.ThrowIfNotUpdated();
        }

        public readonly void Dispose()
        {
            entities.Dispose();
            types.Dispose();
            state.Dispose();
        }

        public readonly RuntimeType GetType(uint index)
        {
            return types[index];
        }

        /// <summary>
        /// Updates the query with the latest data.
        /// </summary>
        public void Update(Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            state.HasUpdated();
            entities.Clear();
            bool exact = (options & Option.ExactComponentTypes) == Option.ExactComponentTypes;
            bool includeDisabled = (options & Option.IncludeDisabledEntities) == Option.IncludeDisabledEntities;
            Span<RuntimeType> typesSpan = types.AsSpan();
            UnmanagedDictionary<uint, ComponentChunk> chunks = world.ComponentChunks;
            foreach (uint hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.ContainsTypes(typesSpan, exact))
                {
                    UnmanagedList<uint> entities = chunk.Entities;
                    if (includeDisabled)
                    {
                        this.entities.AddRange(entities);
                    }
                    else
                    {
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                this.entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly bool Contains(uint entity)
        {
            ThrowIfNotUpdated();
            return entities.Contains(entity);
        }

        public readonly Enumerator GetEnumerator()
        {
            ThrowIfNotUpdated();
            return new(this);
        }

        public static Query Create<T>(World world) where T : unmanaged
        {
            return new(world, RuntimeType.Get<T>());
        }

        public static Query Create<T1, T2>(World world) where T1 : unmanaged where T2 : unmanaged
        {
            Span<RuntimeType> types = stackalloc RuntimeType[2] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>() };
            return new(world, types);
        }

        public static Query Create<T1, T2, T3>(World world) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            Span<RuntimeType> types = stackalloc RuntimeType[3] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>() };
            return new(world, types);
        }

        public static Query Create<T1, T2, T3, T4>(World world) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            Span<RuntimeType> types = stackalloc RuntimeType[4] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>() };
            return new(world, types);
        }

        public readonly struct Result
        {
            private readonly uint entity;
            private readonly World world;

            internal Result(uint entity, World world)
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

            public static implicit operator uint(Result entity)
            {
                return entity.entity;
            }
        }

        public ref struct Enumerator
        {
            private readonly Query query;
            private uint index;

            public readonly Result Current => query[index - 1];

            internal Enumerator(Query query)
            {
                this.query = query;
                index = 0;
            }

            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }

        [Flags]
        public enum Option : byte
        {
            /// <summary>
            /// Enabled entities that at least contain the required component types.
            /// </summary>
            OnlyEnabledEntities = 0,
            /// <summary>
            /// Requires that the entity must have exactly the component types specified,
            /// no more and no less.
            /// </summary>
            ExactComponentTypes = 1,
            /// <summary>
            /// Allows disabled entities to be included in the query.
            /// </summary>
            IncludeDisabledEntities = 2
        }
    }
}
