﻿using Collections;
using Collections.Generic;

namespace Worlds.Pointers
{
    internal struct Chunk
    {
        public uint lastEntity;
        public int count;
        public readonly Definition definition;
        public readonly Worlds.Schema schema;
        public readonly List<uint> entities;
        public readonly List components;
        public readonly int stride;

        public Chunk(Definition definition, Worlds.Schema schema)
        {
            lastEntity = 0;
            count = 0;
            entities = new(4);
            entities.AddDefault(); //reserved
            stride = schema.ComponentRowSize;
            components = new(4, stride);
            components.AddDefault(); //reserved
            this.schema = schema;
            this.definition = definition;
        }
    }
}