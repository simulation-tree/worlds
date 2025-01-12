﻿using Collections;
using Unmanaged;

namespace Worlds
{
    public static class EnumerableExtensions
    {
        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining(this World world, BitSet componentTypes, bool onlyEnabled = false)
        {
            Dictionary<Definition, Chunk> chunks = world.Chunks;
            if (onlyEnabled)
            {
                foreach (Definition key in chunks.Keys)
                {
                    if ((key.ComponentTypes & componentTypes) == componentTypes)
                    {
                        Chunk chunk = chunks[key];
                        uint count = chunk.Count;
                        for (uint e = 0; e < count; e++)
                        {
                            uint entity = chunk[e];
                            if (world.IsEnabled(entity))
                            {
                                yield return entity;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (Definition key in chunks.Keys)
                {
                    if ((key.ComponentTypes & componentTypes) == componentTypes)
                    {
                        Chunk chunk = chunks[key];
                        uint count = chunk.Count;
                        for (uint e = 0; e < count; e++)
                        {
                            yield return chunk[e];
                        }
                    }
                }
            }
        }

        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining(this World world, ComponentType componentType, bool onlyEnabled = false)
        {
            BitSet componentTypes = new(componentType);
            return GetAllContaining(world, componentTypes, onlyEnabled);
        }

        public static System.Collections.Generic.IEnumerable<uint> GetAllContaining<C1>(this World world, bool onlyEnabled = false) where C1 : unmanaged
        {
            return GetAllContaining(world, world.Schema.GetComponent<C1>(), onlyEnabled);
        }

        /// <summary>
        /// Iterates through all entities that are <typeparamref name="T"/>.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<T> GetAll<T>(this World world) where T : unmanaged, IEntity
        {
            Dictionary<Definition, Chunk> chunks = world.Chunks;
            Schema schema = world.Schema;
            Definition definition = Archetype.Get<T>(schema).definition;
            foreach (Definition key in chunks.Keys)
            {
                if (key.ComponentTypes.ContainsAll(definition.ComponentTypes) && key.ArrayElementTypes.ContainsAll(definition.ArrayElementTypes) && key.TagTypes.ContainsAll(definition.TagTypes))
                {
                    Chunk chunk = chunks[key];
                    for (uint e = 0; e < chunk.Count; e++)
                    {
                        Entity entity = new(world, chunk[e]);
                        yield return entity.As<T>();
                    }
                }
            }
        }

        /// <summary>
        /// Finds all entities that contain all of the given component types and
        /// adds them to the given list.
        /// </summary>
        public static void Fill(this World world, USpan<ComponentType> componentTypes, List<uint> list, bool onlyEnabled = false)
        {
            Dictionary<Definition, Chunk> chunks = world.Chunks;
            BitSet componentTypesMask = new();
            foreach (ComponentType componentType in componentTypes)
            {
                componentTypesMask |= componentType;
            }

            foreach (Definition key in chunks.Keys)
            {
                if ((key.ComponentTypes & componentTypesMask) == componentTypesMask)
                {
                    Chunk chunk = chunks[key];
                    USpan<uint> entities = chunk.Entities;
                    if (!onlyEnabled)
                    {
                        list.AddRange(entities);
                    }
                    else
                    {
                        for (uint e = 0; e < entities.Length; e++)
                        {
                            uint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                list.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fills the given <paramref name="list"/> with all components of type <typeparamref name="T"/>.
        /// </summary>
        public static void Fill<T>(this World world, List<T> list, bool onlyEnabled = false) where T : unmanaged
        {
            ComponentType componentType = world.Schema.GetComponent<T>();
            Dictionary<Definition, Chunk> chunks = world.Chunks;
            foreach (Definition key in chunks.Keys)
            {
                if (key.ComponentTypes == componentType)
                {
                    Chunk chunk = chunks[key];
                    if (!onlyEnabled)
                    {
                        list.AddRange(chunk.GetComponents<T>(componentType));
                    }
                    else
                    {
                        USpan<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Length; e++)
                        {
                            uint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                list.Add(chunk.GetComponent<T>(e, componentType));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fills the given <paramref name="destination"/> list with all entities that contain
        /// a component of type <typeparamref name="T"/>.
        /// </summary>
        public static void Fill<T>(this World world, List<uint> destination, bool onlyEnabled = false) where T : unmanaged
        {
            ComponentType componentType = world.Schema.GetComponent<T>();
            Dictionary<Definition, Chunk> chunks = world.Chunks;
            foreach (Definition key in chunks.Keys)
            {
                if (key.ComponentTypes == componentType)
                {
                    Chunk chunk = chunks[key];
                    USpan<uint> entities = chunk.Entities;
                    if (!onlyEnabled)
                    {
                        destination.AddRange(entities);
                    }
                    else
                    {
                        for (uint e = 0; e < entities.Length; e++)
                        {
                            uint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                destination.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fills <paramref name="components"/> and <paramref name="destination"/>.
        /// </summary>
        public static void Fill<T>(this World world, List<T> components, List<uint> destination, bool onlyEnabled = false) where T : unmanaged
        {
            ComponentType componentType = world.Schema.GetComponent<T>();
            Dictionary<Definition, Chunk> chunks = world.Chunks;
            foreach (Definition key in chunks.Keys)
            {
                if (key.ComponentTypes == componentType)
                {
                    Chunk chunk = chunks[key];
                    USpan<uint> entities = chunk.Entities;
                    if (!onlyEnabled)
                    {
                        components.AddRange(chunk.GetComponents<T>(componentType));
                        destination.AddRange(entities);
                    }
                    else
                    {
                        for (uint e = 0; e < entities.Length; e++)
                        {
                            uint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                components.Add(chunk.GetComponent<T>(e, componentType));
                                destination.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fills the given <paramref name="destination"/> list with all entities that have
        /// a component of type <paramref name="componentType"/>.
        /// </summary>
        public static void Fill(this World world, ComponentType componentType, List<uint> destination, bool onlyEnabled = false)
        {
            Dictionary<Definition, Chunk> chunks = world.Chunks;
            foreach (Definition key in chunks.Keys)
            {
                if (key.ComponentTypes == componentType)
                {
                    Chunk chunk = chunks[key];
                    USpan<uint> entities = chunk.Entities;
                    if (!onlyEnabled)
                    {
                        destination.AddRange(entities);
                    }
                    else
                    {
                        for (uint e = 0; e < entities.Length; e++)
                        {
                            uint entity = entities[e];
                            if (world.IsEnabled(entity))
                            {
                                destination.Add(entity);
                            }
                        }
                    }
                }
            }
        }
    }
}
