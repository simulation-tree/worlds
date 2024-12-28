using Collections;
using System;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Worlds
{
    public readonly struct ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
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
            componentTypes = world.Schema.GetComponents<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>();
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
            private static readonly uint stride = (uint)sizeof(ComponentChunk);

            private readonly Allocation chunks;
            private readonly uint chunkCount;
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
            public readonly ComponentType c12;
            public readonly ComponentType c13;
            public readonly ComponentType c14;
            private uint entityCount;
            private uint entityIndex;
            private uint chunkIndex;
            private ComponentChunk.Implementation* chunk;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> Current => ComponentChunk.Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(chunk, entityIndex - 1, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14);

            internal Enumerator(BitSet componentTypes, BitSet excludedComponentTypes, Dictionary<BitSet, ComponentChunk> allChunks, Schema schema)
            {
                chunkCount = 0;
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
                    c12 = schema.GetComponent<C12>();
                    c13 = schema.GetComponent<C13>();
                    c14 = schema.GetComponent<C14>();
                    chunks = new(NativeMemory.Alloc(chunkCount * stride));
                    chunks.CopyFrom(chunksBuffer.Pointer, stride * chunkCount);
                    chunk = (ComponentChunk.Implementation*)chunksBuffer[0];
                    entityCount = ComponentChunk.Implementation.GetCount(chunk);
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
                        chunk = (ComponentChunk.Implementation*)chunks.Read<nint>(chunkIndex * stride);
                        entityCount = ComponentChunk.Implementation.GetCount(chunk);
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