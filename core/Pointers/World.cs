using Collections.Generic;
using Worlds.Functions;

namespace Worlds.Pointers
{
    public readonly struct World
    {
        public readonly List<Slot> slots;
        public readonly Stack<uint> freeEntities;
        public readonly HashSet<ChunkKey> chunks;
        public readonly List<Worlds.Chunk> chunkValues;
        public readonly Worlds.Schema schema;
        public readonly List<(EntityCreatedOrDestroyed, ulong)> entityCreatedOrDestroyed;
        public readonly List<(EntityParentChanged, ulong)> entityParentChanged;
        public readonly List<(EntityDataChanged, ulong)> entityDataChanged;

        internal World(Worlds.Schema schema)
        {
            slots = new(4);
            slots.AddDefault(); //reserved

            freeEntities = new(4);
            chunks = new(4);
            chunkValues = new(4);
            this.schema = schema;
            entityCreatedOrDestroyed = new(4);
            entityParentChanged = new(4);
            entityDataChanged = new(4);

            Worlds.Chunk defaultChunk = new(schema);
            chunks.Add(new(defaultChunk));
            chunkValues.Add(defaultChunk);
        }
    }
}