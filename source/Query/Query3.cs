using System;
using System.Diagnostics;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    public readonly struct Query<T1, T2, T3> : IDisposable where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        private readonly World world;
        private readonly UnmanagedList<Result> results;
        private readonly QueryState state;

        public readonly bool IsDisposed => state.IsDisposed;

        public readonly uint Count
        {
            get
            {
                ThrowIfNotInitialized();
                return results.Count;
            }
        }

        public readonly Result this[uint index]
        {
            get
            {
                ThrowIfNotInitialized();
                return results[index];
            }
        }

#if NET5_0_OR_GREATER
        [Obsolete("Default constructor not available", true)]
        public Query()
        {
            throw new NotImplementedException();
        }
#endif

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
            Span<RuntimeType> types = stackalloc RuntimeType[3];
            types[0] = RuntimeType.Get<T1>();
            types[1] = RuntimeType.Get<T2>();
            types[2] = RuntimeType.Get<T3>();
            bool exact = (options & Query.Option.ExactComponentTypes) == Query.Option.ExactComponentTypes;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            if (includeDisabled)
            {
                foreach (uint hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(types, exact))
                    {
                        UnmanagedList<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            nint component1 = chunk.GetComponentAddress<T1>(e);
                            nint component2 = chunk.GetComponentAddress<T2>(e);
                            nint component3 = chunk.GetComponentAddress<T3>(e);
                            results.Add(new(entity, component1, component2, component3));
                        }
                    }
                }
            }
            else
            {
                foreach (uint hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(types, exact))
                    {
                        UnmanagedList<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                nint component1 = chunk.GetComponentAddress<T1>(e);
                                nint component2 = chunk.GetComponentAddress<T2>(e);
                                nint component3 = chunk.GetComponentAddress<T3>(e);
                                results.Add(new(entity, component1, component2, component3));
                            }
                        }
                    }
                }
            }
        }

        public readonly bool Contains(uint entity)
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
            public readonly uint entity;

            private readonly nint component1;
            private readonly nint component2;
            private readonly nint component3;

            public unsafe ref T1 Component1 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T1>((void*)component1);
            public unsafe ref T2 Component2 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T2>((void*)component2);
            public unsafe ref T3 Component3 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T3>((void*)component3);

            internal Result(uint entity, nint component1, nint component2, nint component3)
            {
                this.entity = entity;
                this.component1 = component1;
                this.component2 = component2;
                this.component3 = component3;
            }
        }

        public ref struct Enumerator
        {
            private readonly Query<T1, T2, T3> query;
            private uint index;

            public readonly Result Current => query.results[index - 1];

            internal Enumerator(Query<T1, T2, T3> query)
            {
                this.query = query;
                index = 0;
            }

            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }
}
