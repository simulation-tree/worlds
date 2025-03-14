using Collections.Generic;
using Worlds.Functions;

namespace Worlds.Pointers
{
    public readonly struct World
    {
        public readonly Worlds.Schema schema;
        public readonly List<Slot> slots;
        public readonly Stack<uint> freeEntities;
        public readonly Worlds.ChunkMap chunks;
        public readonly List<(EntityCreatedOrDestroyed, ulong)> entityCreatedOrDestroyed;
        public readonly List<(EntityParentChanged, ulong)> entityParentChanged;
        public readonly List<(EntityDataChanged, ulong)> entityDataChanged;

        internal World(Worlds.Schema schema)
        {
            this.schema = schema;

            slots = new(4);
            slots.AddDefault(); //reserved

            freeEntities = new(4);
            chunks = new(schema);
            chunks.GetOrCreate(default);

            entityCreatedOrDestroyed = new(4);
            entityParentChanged = new(4);
            entityDataChanged = new(4);
        }
    }
}