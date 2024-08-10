using System;
using System.Diagnostics;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    public readonly struct Query<T1> : IDisposable where T1 : unmanaged
    {
        private readonly World world;
        private readonly UnmanagedList<Result> results;
        private readonly QueryState state;

        public readonly uint Count
        {
            get
            {
                ThrowIfNotInitialized();
                return results.Count;
            }
        }

        public readonly bool IsDisposed => state.IsDisposed;

        public readonly Result this[uint index]
        {
            get
            {
                ThrowIfNotInitialized();
                return results[index];
            }
        }

        [Obsolete("Default constructor not available", true)]
        public Query()
        {
            throw new NotImplementedException();
        }

        public Query(World world)
        {
            this.world = world;
            results = UnmanagedList<Result>.Create();
            state = QueryState.Create();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotInitialized()
        {
            state.ThrowIfNotUpdated();
        }

        public readonly void Dispose()
        {
            results.Dispose();
            state.Dispose();
        }

        public void Update(Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            state.HasUpdated();
            results.Clear();
            UnmanagedDictionary<uint, ComponentChunk> chunks = world.ComponentChunks;
            Span<RuntimeType> types = stackalloc RuntimeType[1];
            types[0] = RuntimeType.Get<T1>();
            bool exact = (options & Query.Option.ExactComponentTypes) == Query.Option.ExactComponentTypes;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            if (includeDisabled)
            {
                for (int i = 0; i < chunks.Count; i++)
                {
                    ComponentChunk chunk = chunks.Values[i];
                    if (chunk.ContainsTypes(types, exact))
                    {
                        UnmanagedList<eint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            eint entity = entities[e];
                            nint component1 = chunk.GetComponentAddress<T1>(e);
                            results.Add(new(entity, component1));
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < chunks.Count; i++)
                {
                    ComponentChunk chunk = chunks.Values[i];
                    if (chunk.ContainsTypes(types, exact))
                    {
                        UnmanagedList<eint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            eint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                nint component1 = chunk.GetComponentAddress<T1>(e);
                                results.Add(new(entity, component1));
                            }
                        }
                    }
                }
            }
        }

        public readonly bool Contains(eint entity)
        {
            ThrowIfNotInitialized();
            for (uint i = 0; i < Count; i++)
            {
                if (results[i].entity == entity)
                {
                    return true;
                }
            }

            return false;
        }

        public readonly Enumerator GetEnumerator()
        {
            ThrowIfNotInitialized();
            return new(this);
        }

        public readonly struct Result
        {
            public readonly eint entity;

            private readonly nint component1;

            public unsafe ref T1 Component1 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T1>((void*)component1);

            internal Result(eint entity, nint component1)
            {
                this.entity = entity;
                this.component1 = component1;
            }
        }

        public ref struct Enumerator
        {
            private readonly Query<T1> query;
            private uint index;

            public readonly Result Current => query.results[index - 1];

            internal Enumerator(Query<T1> query)
            {
                this.query = query;
                this.index = 0;
            }

            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }
}
