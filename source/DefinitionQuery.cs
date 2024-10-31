using Collections;
using System;
using Unmanaged;

namespace Simulation
{
    public struct DefinitionQuery : IDisposable, IQuery
    {
        private readonly List<uint> results;
        private readonly Array<RuntimeType> componentTypes;
        private readonly Array<RuntimeType> arrayTypes;
        private readonly bool hasArrays;
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
            USpan<RuntimeType> componentTypes = stackalloc RuntimeType[definition.ComponentTypeCount];
            definition.CopyComponentTypes(componentTypes);
            this.componentTypes = new(componentTypes);

            hasArrays = definition.ArrayTypeCount > 0;
            if (hasArrays)
            {
                USpan<RuntimeType> arrayTypes = stackalloc RuntimeType[definition.ArrayTypeCount];
                definition.CopyArrayTypes(arrayTypes);
                this.arrayTypes = new(arrayTypes);
            }
        }

        public readonly void Dispose()
        {
            if (hasArrays)
            {
                arrayTypes.Dispose();
            }

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
                if (hasArrays)
                {
                    USpan<RuntimeType> arrayTypes = this.arrayTypes.AsSpan();
                    foreach (int hash in chunks.Keys)
                    {
                        ComponentChunk chunk = chunks[hash];
                        if (chunk.ContainsTypes(componentTypes))
                        {
                            List<uint> entities = chunk.Entities;
                            for (uint e = 0; e < entities.Count; e++)
                            {
                                uint entity = entities[e];
                                USpan<RuntimeType> entityArrays = world.GetArrayTypes(entity);
                                if (ContainsArray(arrayTypes, entityArrays))
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
                        if (chunk.ContainsTypes(componentTypes))
                        {
                            List<uint> entities = chunk.Entities;
                            results.AddRange(entities);
                        }
                    }
                }
            }
            else
            {
                if (hasArrays)
                {
                    USpan<RuntimeType> arrayTypes = this.arrayTypes.AsSpan();
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
                                    USpan<RuntimeType> entityArrays = world.GetArrayTypes(entity);
                                    if (ContainsArray(arrayTypes, entityArrays))
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
                        if (chunk.ContainsTypes(componentTypes))
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

        private static bool ContainsArray(USpan<RuntimeType> arrayTypes, USpan<RuntimeType> entityArrays)
        {
            for (uint i = 0; i < arrayTypes.Length; i++)
            {
                bool found = false;
                for (uint j = 0; j < entityArrays.Length; j++)
                {
                    if (arrayTypes[i] == entityArrays[j])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
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
