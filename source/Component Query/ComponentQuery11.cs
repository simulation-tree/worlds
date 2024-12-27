using Collections;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Worlds.Unsafe;

namespace Worlds
{
    public readonly struct ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
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
            componentTypes = world.Schema.GetComponents<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>();
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
            public readonly ComponentType c4;
            public readonly ComponentType c5;
            public readonly ComponentType c6;
            public readonly ComponentType c7;
            public readonly ComponentType c8;
            public readonly ComponentType c9;
            public readonly ComponentType c10;
            public readonly ComponentType c11;
            private uint entityCount;
            private uint entityIndex;
            private uint chunkIndex;
            private UnsafeComponentChunk* chunk;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> Current => UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(chunk, entityIndex - 1, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11);

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
                    c4 = schema.GetComponent<C4>();
                    c5 = schema.GetComponent<C5>();
                    c6 = schema.GetComponent<C6>();
                    c7 = schema.GetComponent<C7>();
                    c8 = schema.GetComponent<C8>();
                    c9 = schema.GetComponent<C9>();
                    c10 = schema.GetComponent<C10>();
                    c11 = schema.GetComponent<C11>();
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