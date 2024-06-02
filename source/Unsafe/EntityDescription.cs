#if !DEBUG
#define IGNORE_STACKTRACES
#endif

namespace Game.Unsafe
{
    public struct EntityDescription(EntityID entity, uint version, uint componentsKey)
    {
        public EntityID entity = entity;
        public uint version = version;
        public uint componentsKey = componentsKey;
        public EntityCollections collections;
    }
}