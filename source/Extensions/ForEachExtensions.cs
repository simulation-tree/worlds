using Collections;
using Unmanaged;

namespace Worlds
{
    public static class ForEachExtensions
    {
        public static void ForEach<T>(this World world, T forEach) where T : unmanaged, IForEach
        {
            BitSet componentTypes = forEach.ComponentTypes;
            BitSet excludeComponentTypes = forEach.ExcludeComponentTypes;
            Dictionary<BitSet, ComponentChunk> allChunks = world.ComponentChunks;
            USpan<ComponentChunk> chunks = stackalloc ComponentChunk[(int)allChunks.Count];
            uint chunkCount = 0;
            foreach (BitSet key in allChunks.Keys)
            {
                if (key.ContainsAll(componentTypes) && !key.ContainsAny(excludeComponentTypes))
                {
                    chunks[chunkCount++] = allChunks[key];
                }
            }

            for (uint i = 0; i < chunkCount; i++)
            {
                ComponentChunk chunk = chunks[i];
                uint count = chunk.Count;
                for (uint e = 0; e < count; e++)
                {
                    forEach.ForEach(chunk, e);
                }
            }
        }

        public static BitSet GetComponentTypes<T>(this T forEach) where T : unmanaged, IForEach
        {
            return forEach.ComponentTypes;
        }

        public static BitSet GetExcludedComponentTypes<T>(this T forEach) where T : unmanaged, IForEach
        {
            return forEach.ExcludeComponentTypes;
        }

        public static void ForEach<T>(ref this T forEach, ComponentChunk chunk, uint index) where T : unmanaged, IForEach
        {
            forEach.ForEach(chunk, index);
        }
    }
}
