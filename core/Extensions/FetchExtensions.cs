using System;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using Worlds.Pointers;

namespace Worlds
{
    /// <summary>
    /// Fetch extensions for <see cref="World"/>s.
    /// </summary>
    public unsafe static class FetchExtensions
    {
        /// <summary>
        /// Tries to retrieve a reference to the first found component of type <typeparamref name="T"/>.
        /// </summary>
        public static ref T TryGetFirstComponent<T>(this World world, out bool contains) where T : unmanaged
        {
            int componentType = world.schema.GetComponentType<T>();
            int componentOffset = (int)world.schema.schema->componentOffsets[(uint)componentType];
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if (chunk->count > 0 && chunk->componentTypes.Contains(componentType))
                {
                    contains = true;
                    return ref chunk->components[ChunkPointer.FirstEntity].Read<T>(componentOffset);
                }
            }

            contains = false;
            return ref *(T*)default(nint);
        }

        /// <summary>
        /// Tries to retrieve the first <typeparamref name="T"/> component found.
        /// </summary>
        public static bool TryGetFirstComponent<T>(this World world, out T component) where T : unmanaged
        {
            int componentType = world.schema.GetComponentType<T>();
            int componentOffset = (int)world.schema.schema->componentOffsets[(uint)componentType];
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if (chunk->count > 0 && chunk->componentTypes.Contains(componentType))
                {
                    component = chunk->components[ChunkPointer.FirstEntity].Read<T>(componentOffset);
                    return true;
                }
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the first entity found with a component of type <typeparamref name="T"/>.
        /// </summary>
        public static bool TryGetFirstComponent<T>(this World world, out uint entity) where T : unmanaged
        {
            int componentType = world.schema.GetComponentType<T>();
            int componentOffset = (int)world.schema.schema->componentOffsets[(uint)componentType];
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if (chunk->count > 0 && chunk->componentTypes.Contains(componentType))
                {
                    entity = chunk->entities[ChunkPointer.FirstEntity];
                    return true;
                }
            }

            entity = default;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the first found entity and component of type <typeparamref name="T"/>.
        /// </summary>
        public unsafe static ref T TryGetFirstComponent<T>(this World world, out uint entity, out bool contains) where T : unmanaged
        {
            int componentType = world.schema.GetComponentType<T>();
            int componentOffset = (int)world.schema.schema->componentOffsets[(uint)componentType];
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if (chunk->count > 0 && chunk->componentTypes.Contains(componentType))
                {
                    entity = chunk->entities[ChunkPointer.FirstEntity];
                    contains = true;
                    return ref chunk->components[ChunkPointer.FirstEntity].Read<T>(componentOffset);
                }
            }

            entity = default;
            contains = false;
            return ref *(T*)default(nint);
        }

        /// <summary>
        /// Retrieves the first found entity and component of type <typeparamref name="T"/>.
        /// <para>
        /// May throw a <see cref="NullReferenceException"/> if no entity with the component is found.
        /// </para>
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>"
        public static ref T GetFirstComponent<T>(this World world) where T : unmanaged
        {
            int componentType = world.schema.GetComponentType<T>();
            int componentOffset = (int)world.schema.schema->componentOffsets[(uint)componentType];
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if (chunk->count > 0 && chunk->componentTypes.Contains(componentType))
                {
                    return ref chunk->components[ChunkPointer.FirstEntity].Read<T>(componentOffset);
                }
            }

            throw new NullReferenceException($"No entity with component of type `{typeof(T)}` was found");
        }

        /// <summary>
        /// Retrieves the first found entity and component of type <typeparamref name="T"/>.
        /// <para>
        /// May throw a <see cref="NullReferenceException"/> if no entity with the component is found.
        /// </para>
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public static ref T GetFirstComponent<T>(this World world, out uint entity) where T : unmanaged
        {
            int componentType = world.schema.GetComponentType<T>();
            int componentOffset = (int)world.schema.schema->componentOffsets[(uint)componentType];
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if (chunk->count > 0 && chunk->componentTypes.Contains(componentType))
                {
                    entity = chunk->entities[ChunkPointer.FirstEntity];
                    return ref chunk->components[ChunkPointer.FirstEntity].Read<T>(componentOffset);
                }
            }

            throw new NullReferenceException($"No entity with component of type `{typeof(T)}` found");
        }

        /// <summary>
        /// Iterates through all enabled entities that contain the given <paramref name="componentTypes"/>.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining(this World world, BitMask componentTypes)
        {
            for (int i = 0; i < world.Chunks.Length; i++)
            {
                Chunk chunk = world.Chunks[i];
                if (chunk.ComponentTypes.ContainsAll(componentTypes))
                {
                    if ((chunk.TagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                    {
                        int count = chunk.Count;
                        for (int e = 0; e < count; e++)
                        {
                            yield return chunk.Entities[e];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Iterates through all enabled entities that contain the given <paramref name="componentTypes"/>.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining(this World world, BitMask componentTypes, bool onlyEnabled)
        {
            int chunksLength = world.Chunks.Length;
            if (onlyEnabled)
            {
                for (int i = 0; i < chunksLength; i++)
                {
                    Chunk chunk = world.Chunks[i];
                    if (chunk.ComponentTypes.ContainsAll(componentTypes) && (chunk.TagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                    {
                        int count = chunk.Count;
                        for (int e = 0; e < count; e++)
                        {
                            yield return chunk.Entities[e];
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < chunksLength; i++)
                {
                    Chunk chunk = world.Chunks[i];
                    if (chunk.ComponentTypes.ContainsAll(componentTypes))
                    {
                        int count = chunk.Count;
                        for (int e = 0; e < count; e++)
                        {
                            yield return chunk.Entities[e];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Iterates through all entities that contain the given <paramref name="componentType"/>.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining(this World world, int componentType, bool onlyEnabled)
        {
            int chunksLength = world.Chunks.Length;
            if (onlyEnabled)
            {
                for (int i = 0; i < chunksLength; i++)
                {
                    Chunk chunk = world.Chunks[i];
                    if (chunk.ComponentTypes.Contains(componentType) && (chunk.TagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                    {
                        int count = chunk.Count;
                        for (int e = 0; e < count; e++)
                        {
                            yield return chunk.Entities[e];
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < chunksLength; i++)
                {
                    Chunk chunk = world.Chunks[i];
                    if (chunk.ComponentTypes.Contains(componentType))
                    {
                        int count = chunk.Count;
                        for (int e = 0; e < count; e++)
                        {
                            yield return chunk.Entities[e];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Iterates through all enabled entities that contain the given <typeparamref name="T"/> component.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining<T>(this World world) where T : unmanaged
        {
            int chunksLength = world.Chunks.Length;
            int componentType = world.schema.GetComponentType<T>();
            for (int i = 0; i < chunksLength; i++)
            {
                Chunk chunk = world.Chunks[i];
                if (chunk.ComponentTypes.Contains(componentType) && (chunk.TagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                {
                    int count = chunk.Count;
                    for (int e = 0; e < count; e++)
                    {
                        yield return chunk.Entities[e];
                    }
                }
            }
        }

        /// <summary>
        /// Iterates through all entities that contain the given <typeparamref name="T"/> component.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining<T>(this World world, bool onlyEnabled) where T : unmanaged
        {
            int chunksLength = world.Chunks.Length;
            int componentType = world.schema.GetComponentType<T>();
            if (onlyEnabled)
            {
                for (int i = 0; i < chunksLength; i++)
                {
                    Chunk chunk = world.Chunks[i];
                    if (chunk.ComponentTypes.Contains(componentType) && (chunk.TagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                    {
                        int count = chunk.Count;
                        for (int e = 0; e < count; e++)
                        {
                            yield return chunk.Entities[e];
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < chunksLength; i++)
                {
                    Chunk chunk = world.Chunks[i];
                    if (chunk.ComponentTypes.Contains(componentType))
                    {
                        int count = chunk.Count;
                        for (int e = 0; e < count; e++)
                        {
                            yield return chunk.Entities[e];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Iterates through all enabled entity types that comply with <typeparamref name="T"/> type.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<T> GetAll<T>(this World world) where T : unmanaged, IEntity
        {
            Definition definition = Definition.Get<T>(world.schema);
            int chunksLength = world.Chunks.Length;
            for (int i = 0; i < chunksLength; i++)
            {
                Chunk chunk = world.Chunks[i];
                if (chunk.ComponentTypes.ContainsAll(definition.componentTypes) && chunk.ArrayTypes.ContainsAll(definition.arrayTypes))
                {
                    if ((chunk.TagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                    {
                        if (chunk.TagTypes.ContainsAll(definition.tagTypes))
                        {
                            int count = chunk.Count;
                            for (int e = 0; e < count; e++)
                            {
                                yield return new Entity(world, chunk.Entities[e]).As<T>();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Iterates through all entity types that comply with <typeparamref name="T"/> type.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<T> GetAll<T>(this World world, bool onlyEnabled) where T : unmanaged, IEntity
        {
            Definition definition = Definition.Get<T>(world.schema);
            int chunksLength = world.Chunks.Length;
            if (onlyEnabled)
            {
                for (int i = 0; i < chunksLength; i++)
                {
                    Chunk chunk = world.Chunks[i];
                    if (chunk.ComponentTypes.ContainsAll(definition.componentTypes) && chunk.ArrayTypes.ContainsAll(definition.arrayTypes))
                    {
                        if ((chunk.TagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                        {
                            if (chunk.TagTypes.ContainsAll(definition.tagTypes))
                            {
                                int count = chunk.Count;
                                for (int e = 0; e < count; e++)
                                {
                                    yield return new Entity(world, chunk.Entities[e]).As<T>();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < chunksLength; i++)
                {
                    Chunk chunk = world.Chunks[i];
                    if (chunk.ComponentTypes.ContainsAll(definition.componentTypes) && chunk.ArrayTypes.ContainsAll(definition.arrayTypes))
                    {
                        if (chunk.TagTypes.ContainsAll(definition.tagTypes))
                        {
                            int count = chunk.Count;
                            for (int e = 0; e < count; e++)
                            {
                                yield return new Entity(world, chunk.Entities[e]).As<T>();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the first enabled entity that complies with <typeparamref name="T"/> type.
        /// </summary>
        public static bool TryGetFirst<T>(this World world, out T entity) where T : unmanaged, IEntity
        {
            EntityExtensions.ThrowIfLayoutNotCompatible<T>();

            Definition definition = Definition.Get<T>(world.schema);
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if ((chunk->tagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                {
                    if (chunk->componentTypes.ContainsAll(definition.componentTypes) && chunk->arrayTypes.ContainsAll(definition.arrayTypes))
                    {
                        if (chunk->tagTypes.ContainsAll(definition.tagTypes))
                        {
                            if (chunk->count > 0)
                            {
                                entity = new Entity(world, chunk->entities[ChunkPointer.FirstEntity]).As<T>();
                                return true;
                            }
                        }
                    }
                }
            }

            entity = default;
            return false;
        }

        /// <summary>
        /// Retrieves the first entity that complies with <typeparamref name="T"/> type.
        /// </summary>
        public static bool TryGetFirst<T>(this World world, out T entity, bool onlyEnabled) where T : unmanaged, IEntity
        {
            EntityExtensions.ThrowIfLayoutNotCompatible<T>();

            Definition definition = Definition.Get<T>(world.schema);
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            if (onlyEnabled)
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    ChunkPointer* chunk = chunks[i].chunk;
                    if ((chunk->tagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                    {
                        if (chunk->componentTypes.ContainsAll(definition.componentTypes) && chunk->arrayTypes.ContainsAll(definition.arrayTypes))
                        {
                            if (chunk->tagTypes.ContainsAll(definition.tagTypes))
                            {
                                if (chunk->count > 0)
                                {
                                    entity = new Entity(world, chunk->entities[ChunkPointer.FirstEntity]).As<T>();
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    ChunkPointer* chunk = chunks[i].chunk;
                    if (chunk->componentTypes.ContainsAll(definition.componentTypes) && chunk->arrayTypes.ContainsAll(definition.arrayTypes))
                    {
                        if (chunk->tagTypes.ContainsAll(definition.tagTypes))
                        {
                            if (chunk->count > 0)
                            {
                                entity = new Entity(world, chunk->entities[ChunkPointer.FirstEntity]).As<T>();
                                return true;
                            }
                        }
                    }
                }
            }

            entity = default;
            return false;
        }

        /// <summary>
        /// Retrieves the first enabled entity that complies with <typeparamref name="T"/> type.
        /// </summary>
        public static T GetFirst<T>(this World world) where T : unmanaged, IEntity
        {
            ThrowIfEntityDoesntExist<T>(world, true);

            TryGetFirst(world, out T entity);
            return entity;
        }

        /// <summary>
        /// Retrieves the first entity that complies with <typeparamref name="T"/> type.
        /// </summary>
        public static T GetFirst<T>(this World world, bool onlyEnabled) where T : unmanaged, IEntity
        {
            ThrowIfEntityDoesntExist<T>(world, onlyEnabled);

            TryGetFirst(world, out T entity, onlyEnabled);
            return entity;
        }

        /// <summary>
        /// Counts how many enabled entities there are with <typeparamref name="T"/> components.
        /// </summary>
        public static int CountEntitiesWith<T>(this World world) where T : unmanaged
        {
            int componentType = world.schema.GetComponentType<T>();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            int count = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if (chunk->componentTypes.Contains(componentType))
                {
                    if ((chunk->tagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                    {
                        count += chunk->count;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many entities there are with <typeparamref name="T"/> components.
        /// </summary>
        public static int CountEntitiesWith<T>(this World world, bool onlyEnabled) where T : unmanaged
        {
            int componentType = world.schema.GetComponentType<T>();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            int count = 0;
            if (onlyEnabled)
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    ChunkPointer* chunk = chunks[i].chunk;
                    if (chunk->componentTypes.Contains(componentType))
                    {
                        if ((chunk->tagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                        {
                            count += chunk->count;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    ChunkPointer* chunk = chunks[i].chunk;
                    if (chunk->componentTypes.Contains(componentType))
                    {
                        count += chunk->count;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many enabled entities contain the given <paramref name="componentType"/>.
        /// </summary>
        public static int CountEntitiesWith(this World world, int componentType)
        {
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            int count = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if (chunk->componentTypes.Contains(componentType))
                {
                    if ((chunk->tagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                    {
                        count += chunk->count;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many entities contain the given <paramref name="componentType"/>.
        /// </summary>
        public static int CountEntitiesWith(this World world, int componentType, bool onlyEnabled)
        {
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            int count = 0;
            if (onlyEnabled)
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    ChunkPointer* chunk = chunks[i].chunk;
                    if (chunk->componentTypes.Contains(componentType))
                    {
                        if ((chunk->tagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                        {
                            count += chunk->count;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    ChunkPointer* chunk = chunks[i].chunk;
                    if (chunk->componentTypes.Contains(componentType))
                    {
                        count += chunk->count;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many enabled entities comply with <typeparamref name="T"/> type.
        /// </summary>
        public static int CountEntities<T>(this World world) where T : unmanaged, IEntity
        {
            EntityExtensions.ThrowIfLayoutNotCompatible<T>();

            Definition definition = Definition.Get<T>(world.schema);
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            int count = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if ((chunk->tagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                {
                    if (chunk->componentTypes.ContainsAll(definition.componentTypes) && chunk->arrayTypes.ContainsAll(definition.arrayTypes))
                    {
                        if (chunk->tagTypes.ContainsAll(definition.tagTypes))
                        {
                            count += chunk->count;
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many entities comply with <typeparamref name="T"/> type.
        /// </summary>
        public static int CountEntities<T>(this World world, bool onlyEnabled) where T : unmanaged, IEntity
        {
            EntityExtensions.ThrowIfLayoutNotCompatible<T>();

            Definition definition = Definition.Get<T>(world.schema);
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            int count = 0;
            int chunksLength = chunks.Length;
            if (onlyEnabled)
            {
                for (int i = 0; i < chunksLength; i++)
                {
                    ChunkPointer* chunk = chunks[i].chunk;
                    if ((chunk->tagTypes.value.GetElement(3) & Schema.DisabledMask) == 0)
                    {
                        if (chunk->componentTypes.ContainsAll(definition.componentTypes) && chunk->arrayTypes.ContainsAll(definition.arrayTypes))
                        {
                            if (chunk->tagTypes.ContainsAll(definition.tagTypes))
                            {
                                count += chunk->count;
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < chunksLength; i++)
                {
                    ChunkPointer* chunk = chunks[i].chunk;
                    if (chunk->componentTypes.ContainsAll(definition.componentTypes) && chunk->arrayTypes.ContainsAll(definition.arrayTypes))
                    {
                        if (chunk->tagTypes.ContainsAll(definition.tagTypes))
                        {
                            count += chunk->count;
                        }
                    }
                }
            }

            return count;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfEntityDoesntExist<T>(this World world, bool onlyEnabled) where T : unmanaged, IEntity
        {
            if (!TryGetFirst(world, out T _, onlyEnabled))
            {
                throw new NullReferenceException($"No entity of type `{typeof(T)}` exists");
            }
        }
    }
}