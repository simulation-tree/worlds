using System.Collections.Generic;

namespace Worlds
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<uint> GetAllContaining(this World world, BitSet componentTypes, bool onlyEnabled = false)
        {
            Collections.Dictionary<BitSet, ComponentChunk> chunks = world.ComponentChunks;
            if (onlyEnabled)
            {
                foreach (BitSet key in chunks.Keys)
                {
                    if ((key & componentTypes) == componentTypes)
                    {
                        ComponentChunk chunk = chunks[key];
                        Collections.List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            uint entity = entities[e];
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
                foreach (BitSet key in chunks.Keys)
                {
                    if ((key & componentTypes) == componentTypes)
                    {
                        ComponentChunk chunk = chunks[key];
                        Collections.List<uint> entities = chunk.Entities;
                        for (uint e = 0; e < entities.Count; e++)
                        {
                            yield return entities[e];
                        }
                    }
                }
            }
        }

        public static IEnumerable<uint> GetAllContaining(this World world, ComponentType componentType, bool onlyEnabled = false)
        {
            BitSet componentTypes = new(componentType);
            return GetAllContaining(world, componentTypes, onlyEnabled);
        }

        public static IEnumerable<uint> GetAllContaining<C1>(this World world, bool onlyEnabled = false) where C1 : unmanaged
        {
            return GetAllContaining(world, ComponentType.Get<C1>(), onlyEnabled);
        }
    }
}
