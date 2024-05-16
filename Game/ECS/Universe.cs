using System.Collections.Concurrent;

namespace Game.ECS
{
    internal static class Universe
    {
        internal static uint createdWorlds;
        internal static readonly ConcurrentBag<uint> destroyedWorlds = [];
    }
}
