using Collections;
using System;
using System.Runtime.InteropServices;
using Unmanaged;
using Worlds.Unsafe;

namespace Worlds
{
    public readonly struct ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
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
            componentTypes = ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>();
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
            public readonly ComponentType c12;
            public readonly ComponentType c13;
            public readonly ComponentType c14;
            public readonly ComponentType c15;
            public readonly ComponentType c16;
            private uint entityCount;
            private uint entityIndex;
            private uint chunkIndex;
            private UnsafeComponentChunk* chunk;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> Current => UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(chunk, entityIndex - 1, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16);

            internal Enumerator(BitSet componentTypes, BitSet excludedComponentTypes, Dictionary<BitSet, ComponentChunk> allChunks)
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
                    c1 = ComponentType.Get<C1>();
                    c2 = ComponentType.Get<C2>();
                    c3 = ComponentType.Get<C3>();
                    c4 = ComponentType.Get<C4>();
                    c5 = ComponentType.Get<C5>();
                    c6 = ComponentType.Get<C6>();
                    c7 = ComponentType.Get<C7>();
                    c8 = ComponentType.Get<C8>();
                    c9 = ComponentType.Get<C9>();
                    c10 = ComponentType.Get<C10>();
                    c11 = ComponentType.Get<C11>();
                    c12 = ComponentType.Get<C12>();
                    c13 = ComponentType.Get<C13>();
                    c14 = ComponentType.Get<C14>();
                    c15 = ComponentType.Get<C15>();
                    c16 = ComponentType.Get<C16>();
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