using System;
using System.Runtime.CompilerServices;
using Unmanaged;
using Worlds.Pointers;

namespace Worlds
{
    /// <summary>
    /// A native query of entities.
    /// </summary>
    [SkipLocalsInit]
    public unsafe ref struct Query
    {
        private readonly World world;
        private Definition required;
        private Definition exclude;

        /// <summary>
        /// Counts how many entities match this query.
        /// </summary>
        public readonly int Count
        {
            get
            {
                int count = 0;
                ReadOnlySpan<Chunk> chunks = world.Chunks;
                for (int i = 0; i < chunks.Length; i++)
                {
                    Chunk chunk = chunks[i];
                    if (chunk.chunk->count > 0)
                    {
                        //check if chunk contains inclusion
                        if ((chunk.ComponentTypes & required.componentTypes) != required.componentTypes)
                        {
                            continue;
                        }

                        if ((chunk.ArrayTypes & required.arrayTypes) != required.arrayTypes)
                        {
                            continue;
                        }

                        if ((chunk.TagTypes & required.tagTypes) != required.tagTypes)
                        {
                            continue;
                        }

                        //check if chunk doesnt contain exclusion
                        if (chunk.ComponentTypes.ContainsAny(exclude.componentTypes))
                        {
                            continue;
                        }

                        if (chunk.ArrayTypes.ContainsAny(exclude.arrayTypes))
                        {
                            continue;
                        }

                        if (chunk.TagTypes.ContainsAny(exclude.tagTypes))
                        {
                            continue;
                        }

                        count += chunk.chunk->count;
                    }
                }

                return count;
            }
        }

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public Query() { }
#endif

        /// <summary>
        /// Creates a new query.
        /// </summary>
        public Query(World world, Definition required = default, Definition exclude = default)
        {
            MemoryAddress.ThrowIfDefault((void*)world.Address);

            this.world = world;
            this.required = required;
            this.exclude = exclude;
        }

        /// <summary>
        /// Specifies whether disabled entities should be excluded or not.
        /// </summary>
        public Query ExcludeDisabled(bool should)
        {
            if (should)
            {
                exclude.AddTagType(Schema.DisabledTagType);
            }
            else
            {
                exclude.RemoveTagType(Schema.DisabledTagType);
            }

            return this;
        }

        /// <summary>
        /// Makes this query require the given component types.
        /// </summary>
        public Query RequireComponent<T>() where T : unmanaged
        {
            required.AddComponentType<T>(world.schema);
            return this;
        }

        /// <summary>
        /// Makes this query require the given component types.
        /// </summary>
        public Query RequireComponents(BitMask componentTypes)
        {
            required.AddComponentTypes(componentTypes);
            return this;
        }

        /// <summary>
        /// Makes this query exclude the given component types.
        /// </summary>
        public Query ExcludeComponent<T>() where T : unmanaged
        {
            exclude.AddComponentType<T>(world.schema);
            return this;
        }

        /// <summary>
        /// Makes this query require the given array types.
        /// </summary>
        public Query RequireArrayElement<T>() where T : unmanaged
        {
            required.AddArrayType<T>(world.schema);
            return this;
        }

        /// <summary>
        /// Makes this query require the given array types.
        /// </summary>
        public Query RequireArrayElements(BitMask arrayTypes)
        {
            required.AddArrayTypes(arrayTypes);
            return this;
        }

        /// <summary>
        /// Makes this query exclude the given array types.
        /// </summary>
        public Query ExcludeArray<T>() where T : unmanaged
        {
            exclude.AddArrayType<T>(world.schema);
            return this;
        }

        /// <summary>
        /// Makes this query require the given tag types.
        /// </summary>
        public Query RequireTag<T>() where T : unmanaged
        {
            required.AddTagType<T>(world.schema);
            return this;
        }

        /// <summary>
        /// Makes this query require the given tag types.
        /// </summary>
        public Query RequireTags(BitMask tagTypes)
        {
            required.AddTagTypes(tagTypes);
            return this;
        }

        /// <summary>
        /// Makes this query exclude the given tag types.
        /// </summary>
        public Query ExcludeTag<T>() where T : unmanaged
        {
            exclude.AddTagType<T>(world.schema);
            return this;
        }

        /// <summary>
        /// The enumerator.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        /// <summary>
        /// Tries to retrieve the first found entity that matches the query.
        /// </summary>
        public readonly bool TryGetFirst(out uint entity)
        {
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.chunk->count > 0)
                {
                    //check if chunk contains inclusion
                    if ((chunk.ComponentTypes & required.componentTypes) != required.componentTypes)
                    {
                        continue;
                    }

                    if ((chunk.ArrayTypes & required.arrayTypes) != required.arrayTypes)
                    {
                        continue;
                    }

                    if ((chunk.TagTypes & required.tagTypes) != required.tagTypes)
                    {
                        continue;
                    }

                    //check if chunk doesnt contain exclusion
                    if (chunk.ComponentTypes.ContainsAny(exclude.componentTypes))
                    {
                        continue;
                    }

                    if (chunk.ArrayTypes.ContainsAny(exclude.arrayTypes))
                    {
                        continue;
                    }

                    if (chunk.TagTypes.ContainsAny(exclude.tagTypes))
                    {
                        continue;
                    }

                    entity = chunk.chunk->entities[ChunkPointer.FirstEntity];
                    return true;
                }
            }

            entity = default;
            return false;
        }

        /// <summary>
        /// The enumerator of the query.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly Query query;

            private ChunkPointer* currentChunk;
            private int chunkIndex;
            private int entityIndex;

            /// <summary>
            /// The found entity.
            /// </summary>
            public readonly uint Current => currentChunk->entities[entityIndex];

            internal Enumerator(Query query)
            {
                entityIndex = 0;
                currentChunk = default;
                chunkIndex = 0;
                this.query = query;
                World world = query.world;
                Definition required = query.required;
                Definition exclude = query.exclude;
                ReadOnlySpan<Chunk> chunks = world.Chunks;
                for (int i = 0; i < chunks.Length; i++)
                {
                    ChunkPointer* chunk = chunks[i].chunk;

                    //check if chunk contains inclusion
                    if ((chunk->componentTypes & required.componentTypes) != required.componentTypes)
                    {
                        continue;
                    }

                    if ((chunk->arrayTypes & required.arrayTypes) != required.arrayTypes)
                    {
                        continue;
                    }

                    if ((chunk->tagTypes & required.tagTypes) != required.tagTypes)
                    {
                        continue;
                    }

                    //check if chunk doesnt contain exclusion
                    if (chunk->componentTypes.ContainsAny(exclude.componentTypes))
                    {
                        continue;
                    }

                    if (chunk->arrayTypes.ContainsAny(exclude.arrayTypes))
                    {
                        continue;
                    }

                    if (chunk->tagTypes.ContainsAny(exclude.tagTypes))
                    {
                        continue;
                    }

                    currentChunk = chunk;
                    chunkIndex = i;
                    break;
                }
            }

            /// <summary>
            /// Advances the enumerator to the next entity in the query.
            /// </summary>
            public bool MoveNext()
            {
                if (entityIndex < currentChunk->count)
                {
                    entityIndex++;
                    return true;
                }
                else
                {
                    currentChunk = default;
                    World world = query.world;
                    Definition required = query.required;
                    Definition exclude = query.exclude;
                    ReadOnlySpan<Chunk> chunks = world.Chunks;
                    for (int i = chunkIndex + 1; i < chunks.Length; i++)
                    {
                        ChunkPointer* chunk = chunks[i].chunk;
                        if (chunk->count > 0)
                        {
                            //Definition key = chunk->Definition;

                            //check if chunk contains inclusion
                            if ((chunk->componentTypes & required.componentTypes) != required.componentTypes)
                            {
                                continue;
                            }

                            if ((chunk->arrayTypes & required.arrayTypes) != required.arrayTypes)
                            {
                                continue;
                            }

                            if ((chunk->tagTypes & required.tagTypes) != required.tagTypes)
                            {
                                continue;
                            }

                            //check if chunk doesnt contain exclusion
                            if (chunk->componentTypes.ContainsAny(exclude.componentTypes))
                            {
                                continue;
                            }

                            if (chunk->arrayTypes.ContainsAny(exclude.arrayTypes))
                            {
                                continue;
                            }

                            if (chunk->tagTypes.ContainsAny(exclude.tagTypes))
                            {
                                continue;
                            }

                            currentChunk = chunk;
                            chunkIndex = i;
                            break;
                        }
                    }

                    entityIndex = 1;
                    return currentChunk != default;
                }
            }
        }
    }
}