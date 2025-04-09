using Collections.Generic;
using Worlds.Functions;

namespace Worlds.Pointers
{
    internal struct WorldPointer
    {
        public int version;
        public Schema schema;
        public List<Slot> slots;
        public Stack<uint> freeEntities;
        public ChunkMap chunks;
        public List<(EntityCreatedOrDestroyed, ulong)> entityCreatedOrDestroyed;
        public List<(EntityParentChanged, ulong)> entityParentChanged;
        public List<(EntityDataChanged, ulong)> entityDataChanged;
        public List<uint> references;
        public int entityCreatedOrDestroyedCount;
        public int entityParentChangedCount;
        public int entityDataChangedCount;
    }
}