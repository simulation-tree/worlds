using Collections;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Worlds.Unsafe;

namespace Worlds
{
    public readonly struct ComponentQuery<C1, C2, C3> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
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
            componentTypes = world.Schema.GetComponents<C1, C2, C3>();
            this.excludedComponentTypes = excludedComponentTypes;
            this.world = world;
        }

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            Dictionary<BitSet, ComponentChunk> chunks = world.ComponentChunks;
            return new(componentTypes, excludedComponentTypes, chunks, world.Schema);
        }

        public unsafe ref struct Enumerator
        {
            private readonly USpan<nint> chunks;
            public readonly ComponentType c1;
            public readonly ComponentType c2;
            public readonly ComponentType c3;
            private uint entityCount;
            private uint entityIndex;
            private uint chunkIndex;
            private UnsafeComponentChunk* chunk;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly ComponentChunk.Entity<C1, C2, C3> Current => UnsafeComponentChunk.GetEntity<C1, C2, C3>(chunk, entityIndex - 1, c1, c2, c3);

            internal Enumerator(BitSet componentTypes, BitSet excludedComponentTypes, Dictionary<BitSet, ComponentChunk> allChunks, Schema schema)
            {
                uint chunkCount = 0;
                USpan<nint> chunksBuffer = stackalloc nint[(int)allChunks.Count];
                foreach (BitSet key in allChunks.Keys)
                {
                    if (key.ContainsAll(componentTypes) && !key.ContainsAny(excludedComponentTypes))
                    {
                        ComponentChunk chunk = allChunks[key];
                        if (chunk.Count > 0)
                        {
                            chunksBuffer[chunkCount++] = chunk.Address;
                        }
                    }
                }

                entityIndex = 0;
                chunkIndex = 0;
                entityCount = 0;
                if (chunkCount > 0)
                {
                    c1 = schema.GetComponent<C1>();
                    c2 = schema.GetComponent<C2>();
                    c3 = schema.GetComponent<C3>();
                    uint stride = TypeInfo<ComponentChunk>.size;
                    chunks = new(NativeMemory.Alloc(chunkCount * stride), chunkCount);
                    System.Runtime.CompilerServices.Unsafe.CopyBlock(chunks.Pointer, chunksBuffer.Pointer, stride * chunkCount);
                    chunk = (UnsafeComponentChunk*)chunksBuffer[0];
                    entityCount = UnsafeComponentChunk.GetCount(chunk);
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
                    if (chunkIndex < chunks.Length)
                    {
                        chunk = (UnsafeComponentChunk*)chunks[chunkIndex];
                        entityCount = UnsafeComponentChunk.GetCount(chunk);
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
                if (chunks.Length > 0)
                {
                    NativeMemory.Free(chunks.Pointer);
                }
            }
        }
    }
}