using Collections;
using System;
using Unmanaged;

namespace Worlds
{
    //todo: make a generator of queries
    /// <summary>
    /// Query object for <typeparamref name="T1"/> components.
    /// </summary>
    public struct ComponentQuery<T1> : IDisposable, IQuery where T1 : unmanaged
    {
        private readonly List<Result> results;
        private readonly BitSet componentTypes;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;

        /// <summary>
        /// Indexer for query results.
        /// </summary>
        public readonly Result this[uint index] => results[index];

        readonly nint IQuery.Results => results.StartAddress;
        readonly uint IQuery.ResultSize => TypeInfo<Result>.size;

#if NET
        /// <summary>
        /// Creates an uninitialized query.
        /// </summary>
        public ComponentQuery()
        {
            this = Create();
        }
#endif

        private ComponentQuery(List<Result> results, BitSet componentTypes)
        {
            this.results = results;
            this.componentTypes = componentTypes;
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            results.Dispose();
        }

        /// <summary>
        /// Updates the query with results using the given <paramref name="world"/>.
        /// </summary>
        public readonly void Update(World world, bool onlyEnabled = false)
        {
            results.Clear(world.MaxEntityValue);
            Dictionary<BitSet, ComponentChunk> chunks = world.ComponentChunks;
            if (!onlyEnabled)
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if (key.ContainsAll(componentTypes))
                    {
                        ComponentChunk chunk = chunks[key];
                        List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            nint component1 = chunk.GetComponentAddress<T1>(e);
                            results.Add(new(entity, component1));
                        }
                    }
                }
            }
            else
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if (key.ContainsAll(componentTypes))
                    {
                        ComponentChunk chunk = chunks[key];
                        List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
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

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        /// <summary>
        /// Creates an uninitialized query.
        /// </summary>
        public static ComponentQuery<T1> Create()
        {
            List<Result> results = new(1);
            BitSet set = new();
            set.Set(ComponentType.Get<T1>());
            return new(results, set);
        }

        /// <summary>
        /// Query result container.
        /// </summary>
        public readonly struct Result
        {
            /// <summary>
            /// Entity ID.
            /// </summary>
            public readonly uint entity;

            private readonly nint component1;

            /// <summary>
            /// Reference to the <typeparamref name="T1"/> component.
            /// </summary>
            public unsafe ref T1 Component1 => ref *(T1*)component1;

            internal Result(uint entity, nint component1)
            {
                this.entity = entity;
                this.component1 = component1;
            }
        }

        /// <summary>
        /// Query result enumerator.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly ComponentQuery<T1> query;
            private uint index;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly Result Current => query[index - 1];

            internal Enumerator(ComponentQuery<T1> query)
            {
                this.query = query;
                index = 0;
            }

            /// <summary>
            /// Moves to the next result.
            /// </summary>
            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }

    /// <summary>
    /// Query object for <typeparamref name="T1"/> and <typeparamref name="T2"/> components.
    /// </summary>
    public struct ComponentQuery<T1, T2> : IDisposable, IQuery where T1 : unmanaged where T2 : unmanaged
    {
        private readonly List<Result> results;
        private readonly BitSet componentTypes;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;

        /// <summary>
        /// Indexer for query results.
        /// </summary>
        public readonly Result this[uint index] => results[index];

        readonly nint IQuery.Results => results.StartAddress;
        readonly uint IQuery.ResultSize => TypeInfo<Result>.size;

#if NET
        /// <summary>
        /// Creates an uninitialized query.
        /// </summary>
        public ComponentQuery()
        {
            this = Create();
        }
#endif

        private ComponentQuery(List<Result> results, BitSet componentTypes)
        {
            this.results = results;
            this.componentTypes = componentTypes;
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            results.Dispose();
        }

        /// <summary>
        /// Updates the query with results using the given <paramref name="world"/>.
        /// </summary>
        public readonly void Update(World world, bool onlyEnabled = false)
        {
            results.Clear(world.MaxEntityValue);
            Dictionary<BitSet, ComponentChunk> chunks = world.ComponentChunks;
            if (!onlyEnabled)
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if (key.ContainsAll(componentTypes))
                    {
                        ComponentChunk chunk = chunks[key];
                        List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            nint c1 = chunk.GetComponentAddress<T1>(e);
                            nint c2 = chunk.GetComponentAddress<T2>(e);
                            results.Add(new(entity, c1, c2));
                        }
                    }
                }
            }
            else
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if (key.ContainsAll(componentTypes))
                    {
                        ComponentChunk chunk = chunks[key];
                        List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                nint c1 = chunk.GetComponentAddress<T1>(e);
                                nint c2 = chunk.GetComponentAddress<T2>(e);
                                results.Add(new(entity, c1, c2));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        /// <summary>
        /// Creates an uninitialized query.
        /// </summary>
        public static ComponentQuery<T1, T2> Create()
        {
            List<Result> results = new(1);
            BitSet set = new();
            set.Set(ComponentType.Get<T1>());
            set.Set(ComponentType.Get<T2>());
            return new(results, set);
        }

        /// <summary>
        /// Query result container.
        /// </summary>
        public readonly struct Result
        {
            /// <summary>
            /// Entity ID.
            /// </summary>
            public readonly uint entity;

            private readonly nint c1;
            private readonly nint c2;

            /// <summary>
            /// Reference to the <typeparamref name="T1"/> component.
            /// </summary>
            public unsafe ref T1 Component1 => ref *(T1*)c1;

            /// <summary>
            /// Reference to the <typeparamref name="T2"/> component.
            /// </summary>
            public unsafe ref T2 Component2 => ref *(T2*)c2;

            internal Result(uint entity, nint c1, nint c2)
            {
                this.entity = entity;
                this.c1 = c1;
                this.c2 = c2;
            }
        }

        /// <summary>
        /// Query result enumerator.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly ComponentQuery<T1, T2> query;
            private uint index;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly Result Current => query[index - 1];

            internal Enumerator(ComponentQuery<T1, T2> query)
            {
                this.query = query;
                index = 0;
            }

            /// <summary>
            /// Moves to the next result.
            /// </summary>
            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }

    /// <summary>
    /// Query object for <typeparamref name="T1"/>, <typeparamref name="T2"/> and <typeparamref name="T3"/> components.
    /// </summary>
    public struct ComponentQuery<T1, T2, T3> : IDisposable, IQuery where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        private readonly List<Result> results;
        private readonly BitSet componentTypes;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;

        /// <summary>
        /// Indexer for query results.
        /// </summary>
        public readonly Result this[uint index] => results[index];

        readonly nint IQuery.Results => results.StartAddress;
        readonly uint IQuery.ResultSize => TypeInfo<Result>.size;

#if NET
        /// <summary>
        /// Creates an uninitialized query.
        /// </summary>
        public ComponentQuery()
        {
            this = Create();
        }
#endif

        private ComponentQuery(List<Result> results, BitSet componentTypes)
        {
            this.results = results;
            this.componentTypes = componentTypes;
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            results.Dispose();
        }

        /// <summary>
        /// Updates the query with results using the given <paramref name="world"/>.
        /// </summary>
        public readonly void Update(World world, bool onlyEnabled = false)
        {
            results.Clear(world.MaxEntityValue);
            Dictionary<BitSet, ComponentChunk> chunks = world.ComponentChunks;
            if (!onlyEnabled)
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if (key.ContainsAll(componentTypes))
                    {
                        ComponentChunk chunk = chunks[key];
                        List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            nint c1 = chunk.GetComponentAddress<T1>(e);
                            nint c2 = chunk.GetComponentAddress<T2>(e);
                            nint c3 = chunk.GetComponentAddress<T3>(e);
                            results.Add(new(entity, c1, c2, c3));
                        }
                    }
                }
            }
            else
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if (key.ContainsAll(componentTypes))
                    {
                        ComponentChunk chunk = chunks[key];
                        List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                nint c1 = chunk.GetComponentAddress<T1>(e);
                                nint c2 = chunk.GetComponentAddress<T2>(e);
                                nint c3 = chunk.GetComponentAddress<T3>(e);
                                results.Add(new(entity, c1, c2, c3));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        /// <summary>
        /// Creates an uninitialized query.
        /// </summary>
        public static ComponentQuery<T1, T2, T3> Create()
        {
            List<Result> results = new(1);
            BitSet set = new();
            set.Set(ComponentType.Get<T1>());
            set.Set(ComponentType.Get<T2>());
            set.Set(ComponentType.Get<T3>());
            return new(results, set);
        }

        /// <summary>
        /// Query result container.
        /// </summary>
        public readonly struct Result
        {
            /// <summary>
            /// Entity ID.
            /// </summary>
            public readonly uint entity;

            private readonly nint c1;
            private readonly nint c2;
            private readonly nint c3;

            /// <summary>
            /// Reference to the <typeparamref name="T1"/> component.
            /// </summary>
            public unsafe ref T1 Component1 => ref *(T1*)c1;

            /// <summary>
            /// Reference to the <typeparamref name="T2"/> component.
            /// </summary>
            public unsafe ref T2 Component2 => ref *(T2*)c2;

            /// <summary>
            /// Reference to the <typeparamref name="T3"/> component.
            /// </summary>
            public unsafe ref T3 Component3 => ref *(T3*)c3;

            internal Result(uint entity, nint c1, nint c2, nint c3)
            {
                this.entity = entity;
                this.c1 = c1;
                this.c2 = c2;
                this.c3 = c3;
            }
        }

        /// <summary>
        /// Query result enumerator.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly ComponentQuery<T1, T2, T3> query;
            private uint index;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly Result Current => query[index - 1];

            internal Enumerator(ComponentQuery<T1, T2, T3> query)
            {
                this.query = query;
                index = 0;
            }

            /// <summary>
            /// Moves to the next result.
            /// </summary>
            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }

    /// <summary>
    /// Query object for <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/> and <typeparamref name="T4"/> components.
    /// </summary>
    public struct ComponentQuery<T1, T2, T3, T4> : IDisposable, IQuery where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
    {
        private readonly List<Result> results;
        private readonly BitSet componentTypes;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;

        /// <summary>
        /// Indexer for query results.
        /// </summary>
        public readonly Result this[uint index] => results[index];

        readonly nint IQuery.Results => results.StartAddress;
        readonly uint IQuery.ResultSize => TypeInfo<Result>.size;

#if NET
        /// <summary>
        /// Creates an uninitialized query.
        /// </summary>
        public ComponentQuery()
        {
            this = Create();
        }
#endif

        private ComponentQuery(List<Result> results, BitSet componentTypes)
        {
            this.results = results;
            this.componentTypes = componentTypes;
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            results.Dispose();
        }

        /// <summary>
        /// Updates the query with results using the given <paramref name="world"/>.
        /// </summary>
        public readonly void Update(World world, bool onlyEnabled = false)
        {
            results.Clear(world.MaxEntityValue);
            Dictionary<BitSet, ComponentChunk> chunks = world.ComponentChunks;
            if (!onlyEnabled)
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if (key.ContainsAll(componentTypes))
                    {
                        ComponentChunk chunk = chunks[key];
                        List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            nint c1 = chunk.GetComponentAddress<T1>(e);
                            nint c2 = chunk.GetComponentAddress<T2>(e);
                            nint c3 = chunk.GetComponentAddress<T3>(e);
                            nint c4 = chunk.GetComponentAddress<T4>(e);
                            results.Add(new(entity, c1, c2, c3, c4));
                        }
                    }
                }
            }
            else
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if (key.ContainsAll(componentTypes))
                    {
                        ComponentChunk chunk = chunks[key];
                        List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                nint c1 = chunk.GetComponentAddress<T1>(e);
                                nint c2 = chunk.GetComponentAddress<T2>(e);
                                nint c3 = chunk.GetComponentAddress<T3>(e);
                                nint c4 = chunk.GetComponentAddress<T4>(e);
                                results.Add(new(entity, c1, c2, c3, c4));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        /// <summary>
        /// Creates an uninitialized query.
        /// </summary>
        public static ComponentQuery<T1, T2, T3, T4> Create()
        {
            List<Result> results = new(1);
            BitSet set = new();
            set.Set(ComponentType.Get<T1>());
            set.Set(ComponentType.Get<T2>());
            set.Set(ComponentType.Get<T3>());
            set.Set(ComponentType.Get<T4>());
            return new(results, set);
        }

        /// <summary>
        /// Query result container.
        /// </summary>
        public readonly struct Result
        {
            /// <summary>
            /// Entity ID.
            /// </summary>
            public readonly uint entity;

            private readonly nint c1;
            private readonly nint c2;
            private readonly nint c3;
            private readonly nint c4;

            /// <summary>
            /// Reference to the <typeparamref name="T1"/> component.
            /// </summary>
            public unsafe ref T1 Component1 => ref *(T1*)c1;

            /// <summary>
            /// Reference to the <typeparamref name="T2"/> component.
            /// </summary>
            public unsafe ref T2 Component2 => ref *(T2*)c2;

            /// <summary>
            /// Reference to the <typeparamref name="T3"/> component.
            /// </summary>
            public unsafe ref T3 Component3 => ref *(T3*)c3;

            /// <summary>
            /// Reference to the <typeparamref name="T4"/> component.
            /// </summary>
            public unsafe ref T4 Component4 => ref *(T4*)c4;

            internal Result(uint entity, nint c1, nint c2, nint c3, nint c4)
            {
                this.entity = entity;
                this.c1 = c1;
                this.c2 = c2;
                this.c3 = c3;
                this.c4 = c4;
            }
        }

        /// <summary>
        /// Query result enumerator.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly ComponentQuery<T1, T2, T3, T4> query;
            private uint index;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly Result Current => query[index - 1];

            internal Enumerator(ComponentQuery<T1, T2, T3, T4> query)
            {
                this.query = query;
                index = 0;
            }

            /// <summary>
            /// Moves to the next result.
            /// </summary>
            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }

    /// <summary>
    /// Query object for <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/> and <typeparamref name="T5"/> components.
    /// </summary>
    public struct ComponentQuery<T1, T2, T3, T4, T5> : IDisposable, IQuery where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
    {
        private readonly List<Result> results;
        private readonly BitSet componentTypes;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;

        /// <summary>
        /// Indexer for query results.
        /// </summary>
        public readonly Result this[uint index] => results[index];

        readonly nint IQuery.Results => results.StartAddress;
        readonly uint IQuery.ResultSize => TypeInfo<Result>.size;

#if NET
        /// <summary>
        /// Creates an uninitialized query.
        /// </summary>
        public ComponentQuery()
        {
            this = Create();
        }
#endif

        private ComponentQuery(List<Result> results, BitSet componentTypes)
        {
            this.results = results;
            this.componentTypes = componentTypes;
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            results.Dispose();
        }

        /// <summary>
        /// Updates the query with results using the given <paramref name="world"/>.
        /// </summary>
        public readonly void Update(World world, bool onlyEnabled = false)
        {
            results.Clear(world.MaxEntityValue);
            Dictionary<BitSet, ComponentChunk> chunks = world.ComponentChunks;
            if (!onlyEnabled)
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if (key.ContainsAll(componentTypes))
                    {
                        ComponentChunk chunk = chunks[key];
                        List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            nint c1 = chunk.GetComponentAddress<T1>(e);
                            nint c2 = chunk.GetComponentAddress<T2>(e);
                            nint c3 = chunk.GetComponentAddress<T3>(e);
                            nint c4 = chunk.GetComponentAddress<T4>(e);
                            nint c5 = chunk.GetComponentAddress<T5>(e);
                            results.Add(new(entity, c1, c2, c3, c4, c5));
                        }
                    }
                }
            }
            else
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if (key.ContainsAll(componentTypes))
                    {
                        ComponentChunk chunk = chunks[key];
                        List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                nint c1 = chunk.GetComponentAddress<T1>(e);
                                nint c2 = chunk.GetComponentAddress<T2>(e);
                                nint c3 = chunk.GetComponentAddress<T3>(e);
                                nint c4 = chunk.GetComponentAddress<T4>(e);
                                nint c5 = chunk.GetComponentAddress<T5>(e);
                                results.Add(new(entity, c1, c2, c3, c4, c5));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        /// <summary>
        /// Creates an uninitialized query.
        /// </summary>
        public static ComponentQuery<T1, T2, T3, T4, T5> Create()
        {
            List<Result> results = new(1);
            BitSet set = new();
            set.Set(ComponentType.Get<T1>());
            set.Set(ComponentType.Get<T2>());
            set.Set(ComponentType.Get<T3>());
            set.Set(ComponentType.Get<T4>());
            set.Set(ComponentType.Get<T5>());
            return new(results, set);
        }

        /// <summary>
        /// Query result container.
        /// </summary>
        public readonly struct Result
        {
            /// <summary>
            /// Entity ID.
            /// </summary>
            public readonly uint entity;

            private readonly nint c1;
            private readonly nint c2;
            private readonly nint c3;
            private readonly nint c4;
            private readonly nint c5;

            /// <summary>
            /// Reference to the <typeparamref name="T1"/> component.
            /// </summary>
            public unsafe ref T1 Component1 => ref *(T1*)c1;

            /// <summary>
            /// Reference to the <typeparamref name="T2"/> component.
            /// </summary>
            public unsafe ref T2 Component2 => ref *(T2*)c2;

            /// <summary>
            /// Reference to the <typeparamref name="T3"/> component.
            /// </summary>
            public unsafe ref T3 Component3 => ref *(T3*)c3;

            /// <summary>
            /// Reference to the <typeparamref name="T4"/> component.
            /// </summary>
            public unsafe ref T4 Component4 => ref *(T4*)c4;

            /// <summary>
            /// Reference to the <typeparamref name="T5"/> component.
            /// </summary>
            public unsafe ref T5 Component5 => ref *(T5*)c5;

            internal Result(uint entity, nint c1, nint c2, nint c3, nint c4, nint c5)
            {
                this.entity = entity;
                this.c1 = c1;
                this.c2 = c2;
                this.c3 = c3;
                this.c4 = c4;
                this.c5 = c5;
            }
        }

        /// <summary>
        /// Query result enumerator.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly ComponentQuery<T1, T2, T3, T4, T5> query;
            private uint index;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly Result Current => query[index - 1];

            internal Enumerator(ComponentQuery<T1, T2, T3, T4, T5> query)
            {
                this.query = query;
                index = 0;
            }

            /// <summary>
            /// Moves to the next result.
            /// </summary>
            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }
}
