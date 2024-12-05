using Collections;
using System;

namespace Worlds
{
    public readonly struct ComponentQuery<C1, C2, C3, C4, C5, C6> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
    {
        private readonly BitSet componentTypes;
        private readonly World world;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public ComponentQuery()
        {
            throw new NotSupportedException();
        }
#endif

        public ComponentQuery(World world)
        {
            componentTypes = ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6>();
            this.world = world;
        }

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            Dictionary<BitSet, ComponentChunk> chunks = world.ComponentChunks;
            return new(componentTypes, chunks);
        }

        public ref struct Enumerator
        {
            private readonly BitSet componentTypes;
            private readonly Dictionary<BitSet, ComponentChunk> chunks;
            private ComponentChunk currentChunk;
            private uint entityCount;
            private uint entityIndex;
            private uint currentChunkIndex;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly ComponentChunk.Entity<C1, C2, C3, C4, C5, C6> Current => currentChunk.GetEntity<C1, C2, C3, C4, C5, C6>(entityIndex - 1);

            internal Enumerator(BitSet componentTypes, Dictionary<BitSet, ComponentChunk> chunks)
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if (key.ContainsAll(componentTypes))
                    {
                        ComponentChunk chunk = chunks[key];
                        if (chunk.Count > 0)
                        {
                            currentChunk = chunks[key];
                            entityCount = currentChunk.Count;
                            break;
                        }
                    }

                    currentChunkIndex++;
                }

                this.componentTypes = componentTypes;
                this.chunks = chunks;
            }

            /// <summary>
            /// Moves to the next result.
            /// </summary>
            public bool MoveNext()
            {
                if (entityIndex < entityCount)
                {
                    entityIndex++;
                    return true;
                }
                else
                {
                    entityIndex = 1;
                    uint chunkIndex = 0;
                    foreach (BitSet key in chunks.Keys)
                    {
                        if (chunkIndex > currentChunkIndex)
                        {
                            if (key.ContainsAll(componentTypes))
                            {
                                currentChunkIndex = chunkIndex;
                                currentChunk = chunks[key];
                                entityCount = currentChunk.Count;
                                if (entityCount > 0)
                                {
                                    return true;
                                }
                            }
                        }

                        chunkIndex++;
                    }

                    return false;
                }
            }
        }
    }
}