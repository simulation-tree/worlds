using Collections;
using System;
using Unmanaged;

namespace Simulation
{
    public struct ComponentQuery<T1> : IDisposable, IQuery where T1 : unmanaged
    {
        private readonly List<Result> results;
        private readonly Array<RuntimeType> componentTypes;
        private World world;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;
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

        private ComponentQuery(List<Result> results, Array<RuntimeType> componentTypes)
        {
            this.world = default;
            this.results = results;
            this.componentTypes = componentTypes;
        }

        public readonly void Dispose()
        {
            componentTypes.Dispose();
            results.Dispose();
        }

        public void Update(World world, bool onlyEnabled = false)
        {
            this.world = world;
            results.Clear(world.MaxEntityValue);
            Dictionary<int, ComponentChunk> chunks = world.ComponentChunks;
            USpan<RuntimeType> componentTypes = this.componentTypes.AsSpan();
            if (!onlyEnabled)
            {
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
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
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
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
            USpan<RuntimeType> componentTypes = stackalloc RuntimeType[1] { RuntimeType.Get<T1>() };
            Array<RuntimeType> types = new(componentTypes);
            return new(results, types);
        }

        public readonly struct Result
        {
            public readonly uint entity;

            private readonly nint component1;

            public unsafe ref T1 Component1 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T1>((void*)component1);

            internal Result(uint entity, nint component1)
            {
                this.entity = entity;
                this.component1 = component1;
            }
        }

        public ref struct Enumerator
        {
            private readonly ComponentQuery<T1> query;
            private uint index;

            public readonly Result Current => query[index - 1];

            internal Enumerator(ComponentQuery<T1> query)
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

    public struct ComponentQuery<T1, T2> : IDisposable, IQuery where T1 : unmanaged where T2 : unmanaged
    {
        private readonly List<Result> results;
        private readonly Array<RuntimeType> componentTypes;
        private World world;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;
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

        private ComponentQuery(List<Result> results, Array<RuntimeType> componentTypes)
        {
            this.world = default;
            this.results = results;
            this.componentTypes = componentTypes;
        }

        /// <summary>
        /// Disposes the query.
        /// </summary>
        public readonly void Dispose()
        {
            componentTypes.Dispose();
            results.Dispose();
        }

        public void Update(World world, bool onlyEnabled = false)
        {
            this.world = world;
            results.Clear(world.MaxEntityValue);
            Dictionary<int, ComponentChunk> chunks = world.ComponentChunks;
            USpan<RuntimeType> componentTypes = this.componentTypes.AsSpan();
            if (!onlyEnabled)
            {
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
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
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
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
            USpan<RuntimeType> componentTypes = stackalloc RuntimeType[2] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>() };
            Array<RuntimeType> types = new(componentTypes);
            return new(results, types);
        }

        public readonly struct Result
        {
            public readonly uint entity;

            private readonly nint c1;
            private readonly nint c2;

            public unsafe ref T1 Component1 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T1>((void*)c1);
            public unsafe ref T2 Component2 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T2>((void*)c2);

            internal Result(uint entity, nint c1, nint c2)
            {
                this.entity = entity;
                this.c1 = c1;
                this.c2 = c2;
            }
        }

        public ref struct Enumerator
        {
            private readonly ComponentQuery<T1, T2> query;
            private uint index;

            public readonly Result Current => query[index - 1];

            internal Enumerator(ComponentQuery<T1, T2> query)
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

    public struct ComponentQuery<T1, T2, T3> : IDisposable, IQuery where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        private readonly List<Result> results;
        private readonly Array<RuntimeType> componentTypes;
        private World world;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;
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

        private ComponentQuery(List<Result> results, Array<RuntimeType> componentTypes)
        {
            this.world = default;
            this.results = results;
            this.componentTypes = componentTypes;
        }

        public readonly void Dispose()
        {
            componentTypes.Dispose();
            results.Dispose();
        }

        public void Update(World world, bool onlyEnabled = false)
        {
            this.world = world;
            results.Clear(world.MaxEntityValue);
            Dictionary<int, ComponentChunk> chunks = world.ComponentChunks;
            USpan<RuntimeType> componentTypes = this.componentTypes.AsSpan();
            if (!onlyEnabled)
            {
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
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
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
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
            USpan<RuntimeType> componentTypes = stackalloc RuntimeType[3] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>() };
            Array<RuntimeType> types = new(componentTypes);
            return new(results, types);
        }

        public readonly struct Result
        {
            public readonly uint entity;

            private readonly nint c1;
            private readonly nint c2;
            private readonly nint c3;

            public unsafe ref T1 Component1 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T1>((void*)c1);
            public unsafe ref T2 Component2 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T2>((void*)c2);
            public unsafe ref T3 Component3 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T3>((void*)c3);

            internal Result(uint entity, nint c1, nint c2, nint c3)
            {
                this.entity = entity;
                this.c1 = c1;
                this.c2 = c2;
                this.c3 = c3;
            }
        }

        public ref struct Enumerator
        {
            private readonly ComponentQuery<T1, T2, T3> query;
            private uint index;

            public readonly Result Current => query[index - 1];

            internal Enumerator(ComponentQuery<T1, T2, T3> query)
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

    public struct ComponentQuery<T1, T2, T3, T4> : IDisposable, IQuery where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
    {
        private readonly List<Result> results;
        private readonly Array<RuntimeType> componentTypes;
        private World world;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;
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

        private ComponentQuery(List<Result> results, Array<RuntimeType> componentTypes)
        {
            this.world = default;
            this.results = results;
            this.componentTypes = componentTypes;
        }

        public readonly void Dispose()
        {
            componentTypes.Dispose();
            results.Dispose();
        }

        public void Update(World world, bool onlyEnabled = false)
        {
            this.world = world;
            results.Clear(world.MaxEntityValue);
            Dictionary<int, ComponentChunk> chunks = world.ComponentChunks;
            USpan<RuntimeType> componentTypes = this.componentTypes.AsSpan();
            if (!onlyEnabled)
            {
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
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
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
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
            USpan<RuntimeType> componentTypes = stackalloc RuntimeType[4] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>() };
            Array<RuntimeType> types = new(componentTypes);
            return new(results, types);
        }

        public readonly struct Result
        {
            public readonly uint entity;

            private readonly nint c1;
            private readonly nint c2;
            private readonly nint c3;
            private readonly nint c4;

            public unsafe ref T1 Component1 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T1>((void*)c1);
            public unsafe ref T2 Component2 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T2>((void*)c2);
            public unsafe ref T3 Component3 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T3>((void*)c3);
            public unsafe ref T4 Component4 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T4>((void*)c4);

            internal Result(uint entity, nint c1, nint c2, nint c3, nint c4)
            {
                this.entity = entity;
                this.c1 = c1;
                this.c2 = c2;
                this.c3 = c3;
                this.c4 = c4;
            }
        }

        public ref struct Enumerator
        {
            private readonly ComponentQuery<T1, T2, T3, T4> query;
            private uint index;

            public readonly Result Current => query[index - 1];

            internal Enumerator(ComponentQuery<T1, T2, T3, T4> query)
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

    public struct ComponentQuery<T1, T2, T3, T4, T5> : IDisposable, IQuery where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
    {
        private readonly List<Result> results;
        private readonly Array<RuntimeType> componentTypes;
        private World world;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;
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

        private ComponentQuery(List<Result> results, Array<RuntimeType> componentTypes)
        {
            this.world = default;
            this.results = results;
            this.componentTypes = componentTypes;
        }

        public readonly void Dispose()
        {
            componentTypes.Dispose();
            results.Dispose();
        }

        public void Update(World world, bool onlyEnabled = false)
        {
            this.world = world;
            results.Clear(world.MaxEntityValue);
            Dictionary<int, ComponentChunk> chunks = world.ComponentChunks;
            USpan<RuntimeType> componentTypes = this.componentTypes.AsSpan();
            if (!onlyEnabled)
            {
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
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
                foreach (int hash in chunks.Keys)
                {
                    ComponentChunk chunk = chunks[hash];
                    if (chunk.ContainsTypes(componentTypes))
                    {
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
            USpan<RuntimeType> componentTypes = stackalloc RuntimeType[5] { RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>(), RuntimeType.Get<T5>() };
            Array<RuntimeType> types = new(componentTypes);
            return new(results, types);
        }

        public readonly struct Result
        {
            public readonly uint entity;

            private readonly nint c1;
            private readonly nint c2;
            private readonly nint c3;
            private readonly nint c4;
            private readonly nint c5;

            public unsafe ref T1 Component1 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T1>((void*)c1);
            public unsafe ref T2 Component2 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T2>((void*)c2);
            public unsafe ref T3 Component3 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T3>((void*)c3);
            public unsafe ref T4 Component4 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T4>((void*)c4);
            public unsafe ref T5 Component5 => ref System.Runtime.CompilerServices.Unsafe.AsRef<T5>((void*)c5);

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

        public ref struct Enumerator
        {
            private readonly ComponentQuery<T1, T2, T3, T4, T5> query;
            private uint index;

            public readonly Result Current => query[index - 1];

            internal Enumerator(ComponentQuery<T1, T2, T3, T4, T5> query)
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
