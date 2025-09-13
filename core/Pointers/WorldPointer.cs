using Collections.Generic;
using System;
using Worlds.Functions;

namespace Worlds.Pointers
{
    internal struct WorldPointer
    {
        public int maxDepth;
        public Schema schema;
        public List<Slot> slots;
        public List<SlotMetadata> slotMetadata;
        public List<Arrays> arrays;
        public Stack<uint> freeEntities;
        public ChunkMap chunkMap;
        public List<(EntityCreatedOrDestroyed, ulong)> entityCreatedOrDestroyed;
        public List<(EntityParentChanged, ulong)> entityParentChanged;
        public List<(EntityDataChanged, ulong)> entityDataChanged;
        public List<uint> references;
        public Flags flags;
        public int entityCreatedOrDestroyedCount;
        public int entityParentChangedCount;
        public int entityDataChangedCount;

        [Flags]
        public enum Flags
        {
            None = 0,
            HasEntityCreatedOrDestroyedListeners = 1,
            HasEntityParentChangedListeners = 2,
            HasEntityDataChangedListeners = 4
        }
    }
}