using Collections;
using System;

namespace Simulation
{
    public struct DefinitionQuery : IDisposable, IQuery
    {
        private readonly List<uint> results;
        private readonly BitSet componentTypes;
        private readonly BitSet arrayTypes;
        private World world;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;
        public readonly uint this[uint index] => results[index];

        readonly nint IQuery.Results => results.StartAddress;
        readonly uint IQuery.ResultSize => sizeof(uint);

#if NET
        [Obsolete("Default constructor not available", true)]
        public DefinitionQuery()
        {
            throw new NotImplementedException();
        }
#endif

        public DefinitionQuery(Definition definition)
        {
            results = new(1);
            componentTypes = definition.ComponentTypesMask;
            arrayTypes = definition.ArrayTypesMask;
        }

        public readonly void Dispose()
        {
            results.Dispose();
        }

        public void Update(World world, bool onlyEnabled = false)
        {
            this.world = world;
            results.Clear(world.MaxEntityValue);
            Dictionary<int, ComponentChunk> chunks = world.ComponentChunks;
            if (!onlyEnabled)
            {
                if (arrayTypes != default)
                {
                    foreach (int hash in chunks.Keys)
                    {
                        ComponentChunk chunk = chunks[hash];
                        if (chunk.ContainsAllTypes(componentTypes))
                        {
                            List<uint> entities = chunk.Entities;
                            for (uint e = 0; e < entities.Count; e++)
                            {
                                uint entity = entities[e];
                                BitSet entityArrayTypes = world.GetArrayTypesMask(entity);
                                if (entityArrayTypes.ContainsAll(arrayTypes))
                                {
                                    results.Add(entity);
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (int hash in chunks.Keys)
                    {
                        ComponentChunk chunk = chunks[hash];
                        if (chunk.ContainsAllTypes(componentTypes))
                        {
                            List<uint> entities = chunk.Entities;
                            results.AddRange(entities);
                        }
                    }
                }
            }
            else
            {
                if (arrayTypes != default)
                {
                    foreach (int hash in chunks.Keys)
                    {
                        ComponentChunk chunk = chunks[hash];
                        if (chunk.ContainsAllTypes(componentTypes))
                        {
                            List<uint> entities = chunk.Entities;
                            for (uint e = 0; e < entities.Count; e++)
                            {
                                uint entity = entities[e];
                                if (world.IsEnabled(entity))
                                {
                                    BitSet entityArrayTypes = world.GetArrayTypesMask(entity);
                                    if (entityArrayTypes.ContainsAll(arrayTypes))
                                    {
                                        results.Add(entity);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (int hash in chunks.Keys)
                    {
                        ComponentChunk chunk = chunks[hash];
                        if (chunk.ContainsAllTypes(componentTypes))
                        {
                            List<uint> entities = chunk.Entities;
                            for (uint e = 0; e < entities.Count; e++)
                            {
                                uint entity = entities[e];
                                if (world.IsEnabled(entity))
                                {
                                    results.Add(entity);
                                }
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

        public ref struct Enumerator
        {
            private readonly DefinitionQuery query;
            private uint index;

            public readonly uint Current => query[index - 1];

            internal Enumerator(DefinitionQuery query)
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
