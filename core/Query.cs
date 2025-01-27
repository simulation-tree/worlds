using System;
using Unmanaged;

namespace Worlds
{
    public ref struct Query
    {
        private readonly World world;
        private Definition required;
        private Definition exclude;

        /// <summary>
        /// Counts how many entities match this query.
        /// </summary>
        public readonly uint Count
        {
            get
            {
                uint count = 0;
                foreach (Definition key in world.Chunks.Keys)
                {
                    //check if chunk contains inclusion
                    if ((key.ComponentTypes & required.ComponentTypes) != required.ComponentTypes)
                    {
                        continue;
                    }

                    if ((key.ArrayElementTypes & required.ArrayElementTypes) != required.ArrayElementTypes)
                    {
                        continue;
                    }

                    if ((key.TagTypes & required.TagTypes) != required.TagTypes)
                    {
                        continue;
                    }

                    //check if chunk doesnt contain exclusion
                    if (key.ComponentTypes.ContainsAny(exclude.ComponentTypes))
                    {
                        continue;
                    }

                    if (key.ArrayElementTypes.ContainsAny(exclude.ArrayElementTypes))
                    {
                        continue;
                    }

                    if (key.TagTypes.ContainsAny(exclude.TagTypes))
                    {
                        continue;
                    }

                    Chunk chunk = world.Chunks[key];
                    count += chunk.Count;
                }

                return count;
            }
        }

#if NET
        [Obsolete("Default constructor not supported", true)]
        public Query()
        {
            throw new NotSupportedException();
        }
#endif

        public unsafe Query(World world, Definition required = default, Definition exclude = default)
        {
            Allocations.ThrowIfNull((void*)world.Address);

            this.world = world;
            this.required = required;
            this.exclude = exclude;
        }

        public Query ExcludeDisabled(bool should)
        {
            if (should)
            {
                exclude.AddTagType(TagType.Disabled);
            }
            else
            {
                exclude.RemoveTagType(TagType.Disabled);
            }

            return this;
        }

        public Query RequireComponent<T>() where T : unmanaged
        {
            required.AddComponentType<T>(world.Schema);
            return this;
        }

        public Query ExcludeComponent<T>() where T : unmanaged
        {
            exclude.AddComponentType<T>(world.Schema);
            return this;
        }

        public Query RequireArrayElement<T>() where T : unmanaged
        {
            required.AddArrayElementType<T>(world.Schema);
            return this;
        }

        public Query ExcludeArrayElement<T>() where T : unmanaged
        {
            exclude.AddArrayElementType<T>(world.Schema);
            return this;
        }

        public Query RequireTag<T>() where T : unmanaged
        {
            required.AddTagType<T>(world.Schema);
            return this;
        }

        public Query ExcludeTag<T>() where T : unmanaged
        {
            exclude.AddTagType<T>(world.Schema);
            return this;
        }

        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        public readonly bool TryGetFirst(out uint entity)
        {
            foreach (Definition key in world.Chunks.Keys)
            {
                //check if chunk contains inclusion
                if ((key.ComponentTypes & required.ComponentTypes) != required.ComponentTypes)
                {
                    continue;
                }

                if ((key.ArrayElementTypes & required.ArrayElementTypes) != required.ArrayElementTypes)
                {
                    continue;
                }

                if ((key.TagTypes & required.TagTypes) != required.TagTypes)
                {
                    continue;
                }

                //check if chunk doesnt contain exclusion
                if (key.ComponentTypes.ContainsAny(exclude.ComponentTypes))
                {
                    continue;
                }

                if (key.ArrayElementTypes.ContainsAny(exclude.ArrayElementTypes))
                {
                    continue;
                }

                if (key.TagTypes.ContainsAny(exclude.TagTypes))
                {
                    continue;
                }

                Chunk chunk = world.Chunks[key];
                if (chunk.Count > 0)
                {
                    entity = chunk.Entities[0];
                    return true;
                }
            }

            entity = default;
            return false;
        }

        public ref struct Enumerator
        {
            private readonly Query query;

            private Chunk chunk;
            private uint chunkIndex;
            private uint entityIndex;

            public readonly uint Current => chunk.Entities[entityIndex - 1];

            public Enumerator(Query query)
            {
                this.query = query;
                World world = query.world;
                Definition required = query.required;
                Definition exclude = query.exclude;
                foreach (Definition key in world.Chunks.Keys)
                {
                    chunkIndex++;

                    //check if chunk contains inclusion
                    if ((key.ComponentTypes & required.ComponentTypes) != required.ComponentTypes)
                    {
                        continue;
                    }

                    if ((key.ArrayElementTypes & required.ArrayElementTypes) != required.ArrayElementTypes)
                    {
                        continue;
                    }

                    if ((key.TagTypes & required.TagTypes) != required.TagTypes)
                    {
                        continue;
                    }

                    //check if chunk doesnt contain exclusion
                    if (key.ComponentTypes.ContainsAny(exclude.ComponentTypes))
                    {
                        continue;
                    }

                    if (key.ArrayElementTypes.ContainsAny(exclude.ArrayElementTypes))
                    {
                        continue;
                    }

                    if (key.TagTypes.ContainsAny(exclude.TagTypes))
                    {
                        continue;
                    }

                    chunk = world.Chunks[key];
                    break;
                }
            }

            public bool MoveNext()
            {
                if (entityIndex < chunk.Count)
                {
                    entityIndex++;
                    return true;
                }
                else
                {
                    World world = query.world;
                    Definition required = query.required;
                    Definition exclude = query.exclude;
                    uint chunkIndex = 0;
                    chunk = default;
                    foreach (Definition key in world.Chunks.Keys)
                    {
                        chunkIndex++;
                        if (this.chunkIndex >= chunkIndex)
                        {
                            continue;
                        }

                        //check if chunk contains inclusion
                        if ((key.ComponentTypes & required.ComponentTypes) != required.ComponentTypes)
                        {
                            continue;
                        }

                        if ((key.ArrayElementTypes & required.ArrayElementTypes) != required.ArrayElementTypes)
                        {
                            continue;
                        }

                        if ((key.TagTypes & required.TagTypes) != required.TagTypes)
                        {
                            continue;
                        }

                        //check if chunk doesnt contain exclusion
                        if (key.ComponentTypes.ContainsAny(exclude.ComponentTypes))
                        {
                            continue;
                        }

                        if (key.ArrayElementTypes.ContainsAny(exclude.ArrayElementTypes))
                        {
                            continue;
                        }

                        if (key.TagTypes.ContainsAny(exclude.TagTypes))
                        {
                            continue;
                        }

                        this.chunkIndex = chunkIndex;
                        chunk = world.Chunks[key];
                        break;
                    }

                    entityIndex = 1;
                    return chunk != default;
                }
            }
        }
    }
}