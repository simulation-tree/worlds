using System.Collections.Concurrent;
using System.Collections.Generic;
using Unmanaged;

namespace Game.ECS
{
    internal static class Universe
    {
        internal static uint createdWorlds;
        internal static readonly ConcurrentBag<uint> destroyedWorlds = [];

        //todo: replace these with unmanaged collections inside a World
        internal static Dictionary<ComponentTypeMask, CollectionOfComponents>[] components = [];
        internal static CollectionOfCollections?[][] collections = [];
        internal static ConcurrentQueue<Container>[] eventQueues = [];
        internal static Dictionary<RuntimeType, List<Listener>>[] listenerHandlers = [];
        internal static Dictionary<RuntimeType, List<object?>>[] listenerCauses = [];
    }
}
