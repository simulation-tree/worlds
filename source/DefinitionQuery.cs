using Collections;
using System;

namespace Worlds
{
    /// <summary>
    /// Query object for finding entities with a specific <see cref="Definition"/>.
    /// </summary>
    public readonly struct DefinitionQuery : IDisposable, IQuery
    {
        private readonly List<uint> results;
        private readonly BitSet componentTypes;
        private readonly BitSet arrayTypes;

        /// <summary>
        /// All entities found after updating.
        /// </summary>
        public readonly uint Count => results.Count;

        /// <summary>
        /// Gets the entity at the specified <paramref name="index"/>.
        /// </summary>
        public readonly uint this[uint index] => results[index];

        readonly nint IQuery.Results => results.StartAddress;
        readonly uint IQuery.ResultSize => sizeof(uint);

#if NET
        /// <summary>
        /// Not supported.
        /// </summary>
        [Obsolete("Default constructor not available", true)]
        public DefinitionQuery()
        {
            throw new NotImplementedException();
        }
#endif

        /// <summary>
        /// Creates a new query object for finding entities with the given <paramref name="definition"/>.
        /// </summary>
        public DefinitionQuery(Definition definition)
        {
            results = new(1);
            componentTypes = definition.ComponentTypesMask;
            arrayTypes = definition.ArrayTypesMask;
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            results.Dispose();
        }

        /// <summary>
        /// Updates the query with the given <paramref name="world"/>.
        /// </summary>
        public readonly void Update(World world, bool onlyEnabled = false)
        {
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

        /// <summary>
        /// Gets an enumerator for the query.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        /// <summary>
        /// Enumerator for <see cref="DefinitionQuery"/>.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly DefinitionQuery query;
            private uint index;

            /// <summary>
            /// Gets the current entity.
            /// </summary>
            public readonly uint Current => query[index - 1];

            internal Enumerator(DefinitionQuery query)
            {
                this.query = query;
                index = 0;
            }

            /// <summary>
            /// Moves to the next entity.
            /// </summary>
            public bool MoveNext()
            {
                return ++index <= query.Count;
            }
        }
    }
}
