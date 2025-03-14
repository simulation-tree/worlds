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
        public readonly int Count
        {
            get
            {
                int count = 0;
                foreach (Chunk chunk in world.Chunks)
                {
                    if (chunk.Count > 0)
                    {
                        Definition key = chunk.Definition;

                        //check if chunk contains inclusion
                        if ((key.componentTypes & required.componentTypes) != required.componentTypes)
                        {
                            continue;
                        }

                        if ((key.arrayTypes & required.arrayTypes) != required.arrayTypes)
                        {
                            continue;
                        }

                        if ((key.tagTypes & required.tagTypes) != required.tagTypes)
                        {
                            continue;
                        }

                        //check if chunk doesnt contain exclusion
                        if (key.componentTypes.ContainsAny(exclude.componentTypes))
                        {
                            continue;
                        }

                        if (key.arrayTypes.ContainsAny(exclude.arrayTypes))
                        {
                            continue;
                        }

                        if (key.tagTypes.ContainsAny(exclude.tagTypes))
                        {
                            continue;
                        }

                        count += chunk.Count;
                    }
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
            MemoryAddress.ThrowIfDefault((void*)world.Address);

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

        public Query RequireComponents(BitMask componentTypes)
        {
            required.AddComponentTypes(componentTypes);
            return this;
        }

        public Query ExcludeComponent<T>() where T : unmanaged
        {
            exclude.AddComponentType<T>(world.Schema);
            return this;
        }

        public Query RequireArrayElement<T>() where T : unmanaged
        {
            required.AddArrayType<T>(world.Schema);
            return this;
        }

        public Query RequireArrayElements(BitMask arrayTypes)
        {
            required.AddArrayTypes(arrayTypes);
            return this;
        }

        public Query ExcludeArray<T>() where T : unmanaged
        {
            exclude.AddArrayType<T>(world.Schema);
            return this;
        }

        public Query RequireTag<T>() where T : unmanaged
        {
            required.AddTagType<T>(world.Schema);
            return this;
        }

        public Query RequireTags(BitMask tagTypes)
        {
            required.AddTagTypes(tagTypes);
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
            foreach (Chunk chunk in world.Chunks)
            {
                if (chunk.Count > 0)
                {
                    Definition key = chunk.Definition;

                    //check if chunk contains inclusion
                    if ((key.componentTypes & required.componentTypes) != required.componentTypes)
                    {
                        continue;
                    }

                    if ((key.arrayTypes & required.arrayTypes) != required.arrayTypes)
                    {
                        continue;
                    }

                    if ((key.tagTypes & required.tagTypes) != required.tagTypes)
                    {
                        continue;
                    }

                    //check if chunk doesnt contain exclusion
                    if (key.componentTypes.ContainsAny(exclude.componentTypes))
                    {
                        continue;
                    }

                    if (key.arrayTypes.ContainsAny(exclude.arrayTypes))
                    {
                        continue;
                    }

                    if (key.tagTypes.ContainsAny(exclude.tagTypes))
                    {
                        continue;
                    }

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
            private int chunkIndex;
            private int entityIndex;

            public readonly uint Current => chunk.Entities[entityIndex - 1];

            public Enumerator(Query query)
            {
                entityIndex = 0;
                chunk = default;
                chunkIndex = 0;
                this.query = query;
                World world = query.world;
                Definition required = query.required;
                Definition exclude = query.exclude;
                for (int i = 0; i < world.Chunks.Length; i++)
                {
                    Chunk chunk = world.Chunks[i];
                    Definition key = chunk.Definition;

                    //check if chunk contains inclusion
                    if ((key.componentTypes & required.componentTypes) != required.componentTypes)
                    {
                        continue;
                    }

                    if ((key.arrayTypes & required.arrayTypes) != required.arrayTypes)
                    {
                        continue;
                    }

                    if ((key.tagTypes & required.tagTypes) != required.tagTypes)
                    {
                        continue;
                    }

                    //check if chunk doesnt contain exclusion
                    if (key.componentTypes.ContainsAny(exclude.componentTypes))
                    {
                        continue;
                    }

                    if (key.arrayTypes.ContainsAny(exclude.arrayTypes))
                    {
                        continue;
                    }

                    if (key.tagTypes.ContainsAny(exclude.tagTypes))
                    {
                        continue;
                    }

                    this.chunk = chunk;
                    chunkIndex = i;
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
                    chunk = default;
                    World world = query.world;
                    Definition required = query.required;
                    Definition exclude = query.exclude;
                    for (int i = chunkIndex + 1; i < world.Chunks.Length; i++)
                    {
                        Chunk chunk = world.Chunks[i]; 
                        if (chunk.Count > 0)
                        {
                            Definition key = chunk.Definition;

                            //check if chunk contains inclusion
                            if ((key.componentTypes & required.componentTypes) != required.componentTypes)
                            {
                                continue;
                            }

                            if ((key.arrayTypes & required.arrayTypes) != required.arrayTypes)
                            {
                                continue;
                            }

                            if ((key.tagTypes & required.tagTypes) != required.tagTypes)
                            {
                                continue;
                            }

                            //check if chunk doesnt contain exclusion
                            if (key.componentTypes.ContainsAny(exclude.componentTypes))
                            {
                                continue;
                            }

                            if (key.arrayTypes.ContainsAny(exclude.arrayTypes))
                            {
                                continue;
                            }

                            if (key.tagTypes.ContainsAny(exclude.tagTypes))
                            {
                                continue;
                            }

                            this.chunk = chunk;
                            chunkIndex = i;
                            break;
                        }
                    }

                    entityIndex = 1;
                    return chunk != default;
                }
            }
        }
    }
}