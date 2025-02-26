using Collections;
using Collections.Generic;

namespace Worlds.Pointers
{
    public unsafe struct Chunk
    {
        public readonly Definition definition;
        public readonly Schema schema;

        internal List<uint> entities;
        internal Array<List> componentLists;
        internal readonly Array<byte> typeIndices;

        internal Chunk(Definition definition, Array<List> componentLists, Array<byte> typeIndices, Schema schema)
        {
            entities = new(4);
            this.typeIndices = typeIndices;
            this.componentLists = componentLists;
            this.definition = definition;
            this.schema = schema;
        }
    }
}