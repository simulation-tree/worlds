using Collections;
using Collections.Generic;
using System;
using Unmanaged;

namespace Worlds.Pointers
{
    internal readonly struct Chunk
    {
        public readonly Definition definition;
        public readonly List<uint> entities;
        public readonly List components;
        public readonly MemoryAddress componentOffsets;
        public readonly MemoryAddress componentSizes;

        public Chunk(Definition definition, int componentRowSize, ReadOnlySpan<int> componentOffsets, ReadOnlySpan<int> componentSizes)
        {
            entities = new(4);
            this.components = new(4, componentRowSize);
            this.componentOffsets = MemoryAddress.Allocate(componentOffsets);
            this.componentSizes = MemoryAddress.Allocate(componentSizes);
            this.definition = definition;
        }

        private Chunk(Definition definition, int componentRowSize, MemoryAddress componentOffsets, MemoryAddress componentSizes)
        {
            entities = new(4);
            this.components = new(4, componentRowSize);
            this.componentOffsets = componentOffsets;
            this.componentSizes = componentSizes;
            this.definition = definition;
        }

        public static Chunk Create()
        {
            return new(default, 1, MemoryAddress.Allocate(BitMask.Capacity * sizeof(int)), MemoryAddress.Allocate(BitMask.Capacity * sizeof(int)));
        }
    }
}