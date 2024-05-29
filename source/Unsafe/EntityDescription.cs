#if !DEBUG
#define IGNORE_STACKTRACES
#endif

namespace Game.Unsafe
{
    public struct EntityDescription(EntityID entity, uint version, int componentsKey)
    {
        public EntityID entity = entity;
        public uint version = version;
        public int componentsKey = componentsKey;
        public EntityCollections collections;
    }
}