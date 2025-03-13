using System;
using System.Diagnostics;

namespace Worlds
{
    public static class FetchExtensions
    {
        /// <summary>
        /// Tries to retrieve a reference to the first found component of type <typeparamref name="T"/>.
        /// </summary>
        public unsafe static ref T TryGetFirstComponent<T>(this World world, out bool contains) where T : unmanaged
        {
            int componentType = world.Schema.GetComponentTypeIndex<T>();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.ContainsComponent(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        contains = true;
                        return ref chunk.GetComponent<T>(1, componentType);
                    }
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
            int componentType = world.Schema.GetComponentTypeIndex<T>();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.ContainsComponent(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        component = chunk.GetComponent<T>(1, componentType);
                        return true;
                    }
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
            int componentType = world.Schema.GetComponentTypeIndex<T>();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.ContainsComponent(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        entity = chunk.Entities[0];
                        return true;
                    }
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
            int componentType = world.Schema.GetComponentTypeIndex<T>();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.ContainsComponent(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        entity = chunk.Entities[0];
                        contains = true;
                        return ref chunk.GetComponent<T>(1, componentType);
                    }
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
            int componentType = world.Schema.GetComponentTypeIndex<T>();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.ContainsComponent(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        return ref chunk.GetComponent<T>(1, componentType);
                    }
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
            int componentType = world.Schema.GetComponentTypeIndex<T>();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.ContainsComponent(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        entity = chunk.Entities[0];
                        return ref chunk.GetComponent<T>(1, componentType);
                    }
                }
            }

            throw new NullReferenceException($"No entity with component of type `{typeof(T)}` found");
        }

        /// <summary>
        /// Iterates through all entities that contain the given <paramref name="componentTypes"/>.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining(this World world, BitMask componentTypes, bool onlyEnabled = true)
        {
            for (int i = 0; i < world.Chunks.Length; i++)
            {
                Chunk chunk = world.Chunks[i];
                Definition definition = chunk.Definition;
                if (definition.ComponentTypes.ContainsAll(componentTypes))
                {
                    if (!onlyEnabled || (onlyEnabled && !definition.TagTypes.Contains(TagType.Disabled)))
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
        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining(this World world, ComponentType componentType, bool onlyEnabled = true)
        {
            BitMask componentTypes = new((byte)componentType.index);
            return GetAllContaining(world, componentTypes, onlyEnabled);
        }

        /// <summary>
        /// Iterates through all entities that contain the given <typeparamref name="T"/> component.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining<T>(this World world, bool onlyEnabled = true) where T : unmanaged
        {
            BitMask componentTypes = new((byte)world.Schema.GetComponentTypeIndex<T>());
            return GetAllContaining(world, componentTypes, onlyEnabled);
        }

        /// <summary>
        /// Iterates through all entity types that comply with <typeparamref name="T"/> type.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<T> GetAll<T>(this World world, bool onlyEnabled = true) where T : unmanaged, IEntity
        {
            Schema schema = world.Schema;
            Definition definition = Definition.Get<T>(schema);
            for (int i = 0; i < world.Chunks.Length; i++)
            {
                Chunk chunk = world.Chunks[i];
                Definition chunkDefinition = chunk.Definition;
                if (chunkDefinition.ComponentTypes.ContainsAll(definition.ComponentTypes) && chunkDefinition.ArrayTypes.ContainsAll(definition.ArrayTypes))
                {
                    if (!onlyEnabled || (onlyEnabled && !chunkDefinition.TagTypes.Contains(TagType.Disabled)))
                    {
                        if (chunkDefinition.TagTypes.ContainsAll(definition.TagTypes))
                        {
                            int count = chunk.Count;
                            for (int e = 0; e < count; e++)
                            {
                                Entity entity = new(world, chunk.Entities[e]);
                                yield return entity.As<T>();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the first entity that complies with <typeparamref name="T"/> type.
        /// </summary>
        public static bool TryGetFirst<T>(this World world, out T entity, bool onlyEnabled = true) where T : unmanaged, IEntity
        {
            EntityExtensions.ThrowIfNotEntity<T>();

            Schema schema = world.Schema;
            Definition definition = Definition.Get<T>(schema);
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (!onlyEnabled || (onlyEnabled && !chunk.Definition.TagTypes.Contains(TagType.Disabled)))
                {
                    if (chunk.Definition.ComponentTypes.ContainsAll(definition.ComponentTypes) && chunk.Definition.ArrayTypes.ContainsAll(definition.ArrayTypes))
                    {
                        if (chunk.Definition.TagTypes.ContainsAll(definition.TagTypes))
                        {
                            if (chunk.Count > 0)
                            {
                                entity = new Entity(world, chunk.Entities[0]).As<T>();
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
        public static T GetFirst<T>(this World world, bool onlyEnabled = true) where T : unmanaged, IEntity
        {
            ThrowIfEntityDoesntExist<T>(world, onlyEnabled);

            TryGetFirst(world, out T entity, onlyEnabled);
            return entity;
        }

        /// <summary>
        /// Counts how many entities there are with <typeparamref name="T"/> components.
        /// </summary>
        public static int CountEntitiesWith<T>(this World world, bool onlyEnabled = true) where T : unmanaged
        {
            Schema schema = world.Schema;
            int componentType = schema.GetComponentTypeIndex<T>();
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            int count = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.ContainsComponent(componentType))
                {
                    if (!onlyEnabled || (onlyEnabled && !chunk.Definition.TagTypes.Contains(TagType.Disabled)))
                    {
                        count += chunk.Count;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many entities contain the given <paramref name="componentType"/>.
        /// </summary>
        public static int CountEntitiesWith(this World world, ComponentType componentType, bool onlyEnabled = true)
        {
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            int count = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.ContainsComponent(componentType))
                {
                    if (!onlyEnabled || (onlyEnabled && !chunk.Definition.TagTypes.Contains(TagType.Disabled)))
                    {
                        count += chunk.Count;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many entities comply with <typeparamref name="T"/> type.
        /// </summary>
        public static int CountEntities<T>(this World world, bool onlyEnabled = true) where T : unmanaged, IEntity
        {
            EntityExtensions.ThrowIfNotEntity<T>();

            Definition definition = Definition.Get<T>(world.Schema);
            ReadOnlySpan<Chunk> chunks = world.Chunks;
            int count = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                Definition chunkDefinition = chunk.Definition;
                if (!onlyEnabled || (onlyEnabled && !chunkDefinition.TagTypes.Contains(TagType.Disabled)))
                {
                    if (chunkDefinition.ComponentTypes.ContainsAll(definition.ComponentTypes) && chunkDefinition.ArrayTypes.ContainsAll(definition.ArrayTypes))
                    {
                        if (chunkDefinition.TagTypes.ContainsAll(definition.TagTypes))
                        {
                            count += chunk.Count;
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