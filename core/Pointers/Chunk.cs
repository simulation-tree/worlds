using Collections;
using Collections.Generic;

namespace Worlds.Pointers
{
    public readonly struct Chunk
    {
        public readonly Definition definition;

        internal readonly List<uint> entities;
        internal readonly Array<List> componentLists;
        internal readonly Array<byte> typeIndices;

        internal Chunk(Definition definition, Array<List> componentLists, Array<byte> typeIndices)
        {
            entities = new(4);
            this.typeIndices = typeIndices;
            this.componentLists = componentLists;
            this.definition = definition;
        }
    }
}