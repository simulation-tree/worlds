using Collections.Generic;
using Worlds.Functions;

namespace Worlds.Pointers
{
    internal struct WorldPointer
    {
        public readonly Schema schema;
        public readonly List<Slot> slots;
        public readonly Stack<uint> freeEntities;
        public readonly ChunkMap chunks;
        public readonly List<(EntityCreatedOrDestroyed, ulong)> entityCreatedOrDestroyed;
        public readonly List<(EntityParentChanged, ulong)> entityParentChanged;
        public readonly List<(EntityDataChanged, ulong)> entityDataChanged;
        public readonly List<uint> references;
        public int entityCreatedOrDestroyedCount;
        public int entityParentChangedCount;
        public int entityDataChangedCount;

        public WorldPointer(Schema schema)
        {
            this.schema = schema;
            slots = new(4);
            freeEntities = new(4);
            chunks = new(schema);
            entityCreatedOrDestroyed = new(4);
            entityParentChanged = new(4);
            entityDataChanged = new(4);
            references = new();

            //add reserve values at index 0
            slots.AddDefault();
        }
    }
}