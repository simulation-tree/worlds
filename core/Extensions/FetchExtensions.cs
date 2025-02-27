using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    public static class FetchExtensions
    {
        /// <summary>
        /// Tries to retrieve a reference to the first found component of type <typeparamref name="T"/>.
        /// </summary>
        public unsafe static ref T TryGetFirstComponent<T>(this World world, out bool contains) where T : unmanaged
        {
            ComponentType componentType = world.Schema.GetComponentType<T>();
            USpan<Chunk> chunks = world.Chunks;
            for (uint i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.Contains(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        contains = true;
                        return ref chunk.GetComponent<T>(0, componentType);
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
            ComponentType componentType = world.Schema.GetComponentType<T>();
            USpan<Chunk> chunks = world.Chunks;
            for (uint i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.Contains(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        component = chunk.GetComponent<T>(0, componentType);
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
            ComponentType componentType = world.Schema.GetComponentType<T>();
            USpan<Chunk> chunks = world.Chunks;
            for (uint i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.Contains(componentType))
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
            ComponentType componentType = world.Schema.GetComponentType<T>();
            USpan<Chunk> chunks = world.Chunks;
            for (uint i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.Contains(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        entity = chunk.Entities[0];
                        contains = true;
                        return ref chunk.GetComponent<T>(0, componentType);
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
            ComponentType componentType = world.Schema.GetComponentType<T>();
            USpan<Chunk> chunks = world.Chunks;
            for (uint i = 0; i < world.Chunks.Length; i++)
            {
                Chunk chunk = world.Chunks[i];
                if (chunk.Definition.Contains(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        return ref chunk.GetComponent<T>(0, componentType);
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
            ComponentType componentType = world.Schema.GetComponentType<T>();
            USpan<Chunk> chunks = world.Chunks;
            for (uint i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.Contains(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        entity = chunk.Entities[0];
                        return ref chunk.GetComponent<T>(0, componentType);
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
            for (uint i = 0; i < world.Chunks.Length; i++)
            {
                Chunk chunk = world.Chunks[i];
                Definition definition = chunk.Definition;
                if (definition.ComponentTypes.ContainsAll(componentTypes))
                {
                    if (!onlyEnabled || (onlyEnabled && !definition.TagTypes.Contains(byte.MaxValue)))
                    {
                        uint count = chunk.Count;
                        for (uint e = 0; e < count; e++)
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
            BitMask componentTypes = new((byte)world.Schema.GetComponentType<T>().index);
            return GetAllContaining(world, componentTypes, onlyEnabled);
        }

        /// <summary>
        /// Iterates through all entity types that comply with <typeparamref name="T"/> type.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<T> GetAll<T>(this World world, bool onlyEnabled = true) where T : unmanaged, IEntity
        {
            Schema schema = world.Schema;
            Definition definition = Definition.Get<T>(schema);
            for (uint i = 0; i < world.Chunks.Length; i++)
            {
                Chunk chunk = world.Chunks[i];
                Definition chunkDefinition = chunk.Definition;
                if (chunkDefinition.ComponentTypes.ContainsAll(definition.ComponentTypes) && chunkDefinition.ArrayTypes.ContainsAll(definition.ArrayTypes))
                {
                    if (!onlyEnabled || (onlyEnabled && !chunkDefinition.TagTypes.Contains(byte.MaxValue)))
                    {
                        if (chunkDefinition.TagTypes.ContainsAll(definition.TagTypes))
                        {
                            for (uint e = 0; e < chunk.Count; e++)
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
            USpan<Chunk> chunks = world.Chunks;
            for (uint i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (!onlyEnabled || (onlyEnabled && !chunk.Definition.TagTypes.Contains(byte.MaxValue)))
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
        public static uint CountEntitiesWith<T>(this World world, bool onlyEnabled = true) where T : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType = schema.GetComponentType<T>();
            return CountEntitiesWith(world, componentType, onlyEnabled);
        }

        /// <summary>
        /// Counts how many entities contain the given <paramref name="componentType"/>.
        /// </summary>
        public static uint CountEntitiesWith(this World world, ComponentType componentType, bool onlyEnabled = true)
        {
            USpan<Chunk> chunks = world.Chunks;
            uint count = 0;
            for (uint i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.Definition.Contains(componentType))
                {
                    if (!onlyEnabled || (onlyEnabled && !chunk.Definition.TagTypes.Contains(byte.MaxValue)))
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
        public static uint CountEntities<T>(this World world, bool onlyEnabled = true) where T : unmanaged, IEntity
        {
            EntityExtensions.ThrowIfNotEntity<T>();

            Definition definition = Definition.Get<T>(world.Schema);
            USpan<Chunk> chunks = world.Chunks;
            uint count = 0;
            for (uint i = 0; i < chunks.Length; i++)
            {
                Chunk chunk = chunks[i];
                Definition chunkDefinition = chunk.Definition;
                if (!onlyEnabled || (onlyEnabled && !chunkDefinition.TagTypes.Contains(byte.MaxValue)))
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