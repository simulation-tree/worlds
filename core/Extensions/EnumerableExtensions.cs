using System;
using System.Diagnostics;

namespace Worlds
{
    public static class EnumerableExtensions
    {
        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining(this World world, BitMask componentTypes, bool onlyEnabled = true)
        {
            for (uint i = 0; i < world.Chunks.Length; i++)
            {
                Chunk chunk = world.Chunks[i];
                if (chunk.Definition.ComponentTypes.ContainsAll(componentTypes))
                {
                    if (!onlyEnabled || (onlyEnabled && !chunk.Definition.TagTypes.Contains(TagType.Disabled)))
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

        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining(this World world, ComponentType componentType, bool onlyEnabled = true)
        {
            BitMask componentTypes = new(componentType);
            return GetAllContaining(world, componentTypes, onlyEnabled);
        }

        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining<C1>(this World world, bool onlyEnabled = true) where C1 : unmanaged
        {
            BitMask componentTypes = new(world.Schema.GetComponent<C1>());
            return GetAllContaining(world, componentTypes, onlyEnabled);
        }

        /// <summary>
        /// Iterates through all entities that are <typeparamref name="T"/>.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<T> GetAll<T>(this World world, bool onlyEnabled = true) where T : unmanaged, IEntity
        {
            Schema schema = world.Schema;
            Definition definition = Archetype.Get<T>(schema).Definition;
            for (uint i = 0; i < world.Chunks.Length; i++)
            {
                Chunk chunk = world.Chunks[i];
                if (chunk.Definition.ComponentTypes.ContainsAll(definition.ComponentTypes) && chunk.Definition.ArrayElementTypes.ContainsAll(definition.ArrayElementTypes))
                {
                    if (!onlyEnabled || (onlyEnabled && !chunk.Definition.TagTypes.Contains(TagType.Disabled)))
                    {
                        if (chunk.Definition.TagTypes.ContainsAll(definition.TagTypes))
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
        /// Retrieves the first entity that complies with type <typeparamref name="T"/>.
        /// </summary>
        public static bool TryGetFirst<T>(this World world, out T entity, bool onlyEnabled = true) where T : unmanaged, IEntity
        {
            EntityExtensions.ThrowIfNotEntity<T>();

            Schema schema = world.Schema;
            Definition definition = Archetype.Get<T>(schema).Definition;
            Query query = new(world, definition);
            if (onlyEnabled)
            {
                query.ExcludeDisabled(true);
            }

            bool found = query.TryGetFirst(out uint foundEntity);
            if (found)
            {
                entity = new Entity(world, foundEntity).As<T>();
            }
            else
            {
                entity = default;
            }

            return found;
        }

        /// <summary>
        /// Retrieves the first entity that complies with the type.
        /// </summary>
        public static T GetFirst<T>(this World world, bool onlyEnabled = true) where T : unmanaged, IEntity
        {
            ThrowIfEntityDoesntExist<T>(world, onlyEnabled);

            TryGetFirst(world, out T entity, onlyEnabled);
            return entity;
        }

        /// <summary>
        /// Counts how many entities there are with component of type <typeparamref name="T"/>.
        /// </summary>
        public static uint CountEntitiesWith<T>(this World world, bool onlyEnabled = true) where T : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType = schema.GetComponent<T>();
            return CountEntitiesWith(world, componentType, onlyEnabled);
        }

        /// <summary>
        /// Counts how many entities there are with component of the given <paramref name="componentType"/>.
        /// </summary>
        public static uint CountEntitiesWith(this World world, ComponentType componentType, bool onlyEnabled = true)
        {
            Definition definition = new();
            definition.AddComponentType(componentType);
            Query query = new(world, definition);
            if (onlyEnabled)
            {
                query.ExcludeDisabled(true);
            }

            return query.Count;
        }

        /// <summary>
        /// Counts how many entities comply with type <typeparamref name="T"/>.
        /// </summary>
        public static uint CountEntities<T>(this World world, bool onlyEnabled = true) where T : unmanaged, IEntity
        {
            EntityExtensions.ThrowIfNotEntity<T>();

            Schema schema = world.Schema;
            Definition definition = Archetype.Get<T>(schema).Definition;
            Query query = new(world, definition);
            if (onlyEnabled)
            {
                query.ExcludeDisabled(true);
            }

            return query.Count;
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
