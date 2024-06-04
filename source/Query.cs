using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public struct Query : IDisposable
    {
        public Option options;
        private bool initialized;

        private readonly World world;
        private readonly UnmanagedList<EntityID> entities;
        private readonly UnmanagedArray<RuntimeType> types;

        public readonly bool IsDisposed => types.IsDisposed;
        public readonly uint Count
        {
            get
            {
                if (!initialized)
                {
                    throw new InvalidOperationException("Fill() must be called before accessing Count.");
                }

                return entities.Count;
            }
        }

        public readonly Entity this[uint index] => new(entities[index], world);

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
        /// Updates the query with the latest data.
        /// </summary>
        public void Fill()
        {
            initialized = true;
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
                    UnmanagedList<EntityID> entities = chunk.Entities;
                    if (includeDisabled)
                    {
                        this.entities.AddRange(entities);
                    }
                    else
                    {
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

        public readonly Enumerator GetEnumerator()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Fill() must be called before iterating.");
            }

            return new(this);
        }

        public readonly struct Entity
        {
            private readonly EntityID entity;
            private readonly World world;

            internal Entity(EntityID entity, World world)
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

            public static implicit operator EntityID(Entity entity)
            {
                return entity.entity;
            }
        }

        public ref struct Enumerator
        {
            private readonly Query query;
            private uint index;

            public readonly Entity Current => query[index - 1];

            internal Enumerator(Query query)
            {
                this.query = query;
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
            /// Only enabled entities that at least contain the required component types.
            /// </summary>
            Default = 0,
            /// <summary>
            /// Requires that the entity must have exactly the component types specified, no more.
            /// </summary>
            ExactComponentTypes = 1,
            /// <summary>
            /// Allows disabled entities to be included in the query.
            /// </summary>
            IncludeDisabledEntities = 2
        }
    }

    public struct Query<T1> : IDisposable where T1 : unmanaged
    {
        private readonly World world;
        private readonly UnmanagedList<Result> results;
        private readonly Query.Option options;

        public readonly uint Count => results.Count;
        public readonly Result this[uint index] => results[index];

        public Query(World world, Query.Option options = default)
        {
            this.world = world;
            this.options = options;
            results = new();
        }

        public void Dispose()
        {
            results.Dispose();
        }

        public void Fill()
        {
            results.Clear();
            UnmanagedDictionary<uint, ComponentChunk> chunks = world.ComponentChunks;
            Span<RuntimeType> types = [RuntimeType.Get<T1>()];
            bool exact = (options & Query.Option.ExactComponentTypes) == Query.Option.ExactComponentTypes;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < chunks.Count; i++)
            {
                ComponentChunk chunk = chunks.Values[i];
                if (chunk.ContainsTypes(types, exact))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        EntityID entity = entities[e];
                        if (includeDisabled || world.IsEnabled(entity))
                        {
                            nint component1 = chunk.GetComponentAddress<T1>(e);
                            results.Add(new(entity, component1));
                        }
                    }
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        public readonly struct Result
        {
            public readonly EntityID entity;

            private readonly nint component1;

            public unsafe ref T1 Component1 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T1>((void*)component1);

            internal Result(EntityID entity, nint component1)
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
            }

            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }

    public struct Query<T1, T2> : IDisposable where T1 : unmanaged where T2 : unmanaged
    {
        private readonly World world;
        private readonly UnmanagedList<Result> results;
        private readonly Query.Option options;

        public readonly uint Count => results.Count;
        public readonly Result this[uint index] => results[index];

        public Query(World world, Query.Option options = default)
        {
            this.world = world;
            this.options = options;
            results = new();
        }

        public void Dispose()
        {
            results.Dispose();
        }

        public void Fill()
        {
            results.Clear();
            UnmanagedDictionary<uint, ComponentChunk> chunks = world.ComponentChunks;
            Span<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>()];
            bool exact = (options & Query.Option.ExactComponentTypes) == Query.Option.ExactComponentTypes;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < chunks.Count; i++)
            {
                ComponentChunk chunk = chunks.Values[i];
                if (chunk.ContainsTypes(types, exact))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        EntityID entity = entities[e];
                        if (includeDisabled || world.IsEnabled(entity))
                        {
                            nint component1 = chunk.GetComponentAddress<T1>(e);
                            nint component2 = chunk.GetComponentAddress<T2>(e);
                            results.Add(new(entity, component1, component2));
                        }
                    }
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        public readonly struct Result
        {
            public readonly EntityID entity;

            private readonly nint component1;
            private readonly nint component2;

            public unsafe ref T1 Component1 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T1>((void*)component1);
            public unsafe ref T2 Component2 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T2>((void*)component2);

            internal Result(EntityID entity, nint component1, nint component2)
            {
                this.entity = entity;
                this.component1 = component1;
                this.component2 = component2;
            }
        }

        public ref struct Enumerator
        {
            private readonly Query<T1, T2> query;
            private uint index;

            public readonly Result Current => query.results[index - 1];

            internal Enumerator(Query<T1, T2> query)
            {
                this.query = query;
            }

            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }

    public struct Query<T1, T2, T3> : IDisposable where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        private readonly World world;
        private readonly UnmanagedList<Result> results;
        private readonly Query.Option options;

        public readonly uint Count => results.Count;
        public readonly Result this[uint index] => results[index];

        public Query(World world, Query.Option options = default)
        {
            this.world = world;
            this.options = options;
            results = new();
        }

        public void Dispose()
        {
            results.Dispose();
        }

        public void Fill()
        {
            results.Clear();
            UnmanagedDictionary<uint, ComponentChunk> chunks = world.ComponentChunks;
            Span<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>()];
            bool exact = (options & Query.Option.ExactComponentTypes) == Query.Option.ExactComponentTypes;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < chunks.Count; i++)
            {
                ComponentChunk chunk = chunks.Values[i];
                if (chunk.ContainsTypes(types, exact))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        EntityID entity = entities[e];
                        if (includeDisabled || world.IsEnabled(entity))
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

        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        public readonly struct Result
        {
            public readonly EntityID entity;

            private readonly nint component1;
            private readonly nint component2;
            private readonly nint component3;

            public unsafe ref T1 Component1 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T1>((void*)component1);
            public unsafe ref T2 Component2 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T2>((void*)component2);
            public unsafe ref T3 Component3 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T3>((void*)component3);

            internal Result(EntityID entity, nint component1, nint component2, nint component3)
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
            }

            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }

    public struct Query<T1, T2, T3, T4> : IDisposable where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
    {
        private readonly World world;
        private readonly UnmanagedList<Result> results;
        private readonly Query.Option options;

        public readonly uint Count => results.Count;
        public readonly Result this[uint index] => results[index];

        public Query(World world, Query.Option options = default)
        {
            this.world = world;
            this.options = options;
            results = new();
        }

        public void Dispose()
        {
            results.Dispose();
        }

        public void Fill()
        {
            results.Clear();
            UnmanagedDictionary<uint, ComponentChunk> chunks = world.ComponentChunks;
            Span<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>()];
            bool exact = (options & Query.Option.ExactComponentTypes) == Query.Option.ExactComponentTypes;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            for (int i = 0; i < chunks.Count; i++)
            {
                ComponentChunk chunk = chunks.Values[i];
                if (chunk.ContainsTypes(types, exact))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        EntityID entity = entities[e];
                        if (includeDisabled || world.IsEnabled(entity))
                        {
                            nint component1 = chunk.GetComponentAddress<T1>(e);
                            nint component2 = chunk.GetComponentAddress<T2>(e);
                            nint component3 = chunk.GetComponentAddress<T3>(e);
                            nint component4 = chunk.GetComponentAddress<T4>(e);
                            results.Add(new(entity, component1, component2, component3, component4));
                        }
                    }
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        public readonly struct Result
        {
            public readonly EntityID entity;

            private readonly nint component1;
            private readonly nint component2;
            private readonly nint component3;
            private readonly nint component4;

            public unsafe ref T1 Component1 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T1>((void*)component1);
            public unsafe ref T2 Component2 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T2>((void*)component2);
            public unsafe ref T3 Component3 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T3>((void*)component3);
            public unsafe ref T4 Component4 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T4>((void*)component4);

            internal Result(EntityID entity, nint component1, nint component2, nint component3, nint component4)
            {
                this.entity = entity;
                this.component1 = component1;
                this.component2 = component2;
                this.component3 = component3;
                this.component4 = component4;
            }
        }

        public ref struct Enumerator
        {
            private readonly Query<T1, T2, T3, T4> query;
            private uint index;

            public readonly Result Current => query.results[index - 1];

            internal Enumerator(Query<T1, T2, T3, T4> query)
            {
                this.query = query;
            }

            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }
}
