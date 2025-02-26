using Collections.Generic;
using Worlds.Functions;

namespace Worlds.Pointers
{
    public unsafe struct World
    {
        public readonly List<Slot> slots;
        public readonly Stack<uint> freeEntities;
        public readonly Dictionary<Definition, Worlds.Chunk> chunksMap;
        public readonly List<Worlds.Chunk> uniqueChunks;
        public readonly Schema schema;
        public readonly List<(EntityCreatedOrDestroyed, ulong)> entityCreatedOrDestroyed;
        public readonly List<(EntityParentChanged, ulong)> entityParentChanged;
        public readonly List<(EntityDataChanged, ulong)> entityDataChanged;

        internal World(Schema schema)
        {
            slots = new(4);
            slots.AddDefault(); //reserved

            freeEntities = new(4);
            chunksMap = new(4);
            uniqueChunks = new(4);
            this.schema = schema;
            entityCreatedOrDestroyed = new(4);
            entityParentChanged = new(4);
            entityDataChanged = new(4);

            Worlds.Chunk defaultChunk = new(schema);
            chunksMap.Add(default, defaultChunk);
            uniqueChunks.Add(defaultChunk);
        }
    }
}