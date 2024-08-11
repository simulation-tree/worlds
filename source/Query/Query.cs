using System;
using System.Diagnostics;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    public readonly struct Query : IDisposable
    {
        private readonly QueryState state;
        private readonly World world;
        private readonly UnmanagedList<eint> entities;
        private readonly UnmanagedArray<RuntimeType> types;

        public readonly bool IsDisposed => state.IsDisposed;

        /// <summary>
        /// The component types used to filter with.
        /// </summary>
        public readonly ReadOnlySpan<RuntimeType> Types => types.AsSpan();

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
            entities = UnmanagedList<eint>.Create();
            state = QueryState.Create();
        }

        public Query(World world, ReadOnlySpan<RuntimeType> types)
        {
            this.world = world;
            this.types = new(types);
            entities = UnmanagedList<eint>.Create();
            state = QueryState.Create();
        }

        public Query(World world, RuntimeType type)
        {
            this.world = world;
            this.types = UnmanagedArray<RuntimeType>.Create(1);
            this.types.Set(0, type);

            entities = UnmanagedList<eint>.Create();
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
            UnmanagedDictionary<uint, ComponentChunk> componentChunks = world.ComponentChunks;
            for (int i = 0; i < componentChunks.Count; i++)
            {
                ComponentChunk chunk = componentChunks.Values[i];
                if (chunk.ContainsTypes(typesSpan, exact))
                {
                    UnmanagedList<eint> entities = chunk.Entities;
                    if (includeDisabled)
                    {
                        this.entities.AddRange(entities);
                    }
                    else
                    {
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            eint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                this.entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly bool Contains(eint entity)
        {
            ThrowIfNotUpdated();
            return entities.Contains(entity);
        }

        public readonly Enumerator GetEnumerator()
        {
            ThrowIfNotUpdated();
            return new(this);
        }

        public readonly struct Result
        {
            private readonly eint entity;
            private readonly World world;

            internal Result(eint entity, World world)
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

            public static implicit operator eint(Result entity)
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
