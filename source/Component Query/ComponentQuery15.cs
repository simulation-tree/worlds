using Collections;
using System;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Worlds
{
    public readonly struct ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
    {
        private readonly BitSet componentTypes;
        private readonly BitSet excludedComponentTypes;
        private readonly World world;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public ComponentQuery()
        {
            throw new NotSupportedException();
        }
#endif

        public ComponentQuery(World world, BitSet excludedComponentTypes = default)
        {
            componentTypes = ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>();
            this.excludedComponentTypes = excludedComponentTypes;
            this.world = world;
        }

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            Dictionary<BitSet, ComponentChunk> chunks = world.ComponentChunks;
            return new(componentTypes, excludedComponentTypes, chunks);
        }

        public unsafe ref struct Enumerator
        {
            private readonly BitSet componentTypes;
            private readonly BitSet excludedComponentTypes;
            private readonly void* chunks;
            private readonly uint chunkCount;
            private uint entityCount;
            private uint entityIndex;
            private uint chunkIndex;
            private ComponentChunk chunk;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> Current => chunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(entityIndex - 1);

            internal Enumerator(BitSet componentTypes, BitSet excludedComponentTypes, Dictionary<BitSet, ComponentChunk> allChunks)
            {
                chunkCount = 0;
                USpan<ComponentChunk> chunksBuffer = stackalloc ComponentChunk[(int)allChunks.Count];
                foreach (BitSet key in allChunks.Keys)
                {
                    if (key.ContainsAll(componentTypes) && !key.ContainsAny(excludedComponentTypes))
                    {
                        ComponentChunk chunk = allChunks[key];
                        if (chunk.Count > 0)
                        {
                            chunksBuffer[chunkCount++] = chunk;
                        }
                    }
                }

                this.excludedComponentTypes = excludedComponentTypes;
                this.componentTypes = componentTypes;
                entityIndex = 0;
                chunkIndex = 0;
                entityCount = 0;
                if (chunkCount > 0)
                {
                    uint stride = TypeInfo<ComponentChunk>.size;
                    chunks = NativeMemory.Alloc(chunkCount * stride);
                    System.Runtime.CompilerServices.Unsafe.CopyBlock(chunks, chunksBuffer.Pointer, stride * chunkCount);
                    chunk = chunksBuffer[0];
                    entityCount = chunk.Count;
                }
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
                    chunkIndex++;
                    if (chunkIndex < chunkCount)
                    {
                        chunk = ((ComponentChunk*)chunks)[chunkIndex];
                        entityCount = chunk.Count;
                        entityIndex = 1;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public readonly void Dispose()
            {
                if (chunkCount > 0)
                {
                    NativeMemory.Free(chunks);
                }
            }
        }
    }
}