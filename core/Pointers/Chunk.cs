using Collections;
using Collections.Generic;
using System;
using Unmanaged;

namespace Worlds.Pointers
{
    internal struct Chunk
    {
        public uint lastEntity;
        public int count;
        public readonly Definition definition;
        public readonly List<uint> entities;
        public readonly List components;
        public readonly ushort stride;
        public readonly MemoryAddress componentOffsets;
        public readonly MemoryAddress componentSizes;

        public Chunk(Definition definition, ushort stride, ReadOnlySpan<ushort> componentOffsets, ReadOnlySpan<ushort> componentSizes)
        {
            lastEntity = 0;
            count = 0;
            entities = new(4);
            entities.AddDefault(); //reserved
            this.stride = stride;
            this.components = new(4, stride);
            components.AddDefault(); //reserved
            this.componentOffsets = MemoryAddress.Allocate(componentOffsets);
            this.componentSizes = MemoryAddress.Allocate(componentSizes);
            this.definition = definition;
        }

        private Chunk(Definition definition, ushort stride, MemoryAddress componentOffsets, MemoryAddress componentSizes)
        {
            lastEntity = 0;
            count = 0;
            entities = new(4);
            entities.AddDefault(); //reserved
            this.stride = stride;
            this.components = new(4, stride);
            components.AddDefault(); //reserved
            this.componentOffsets = componentOffsets;
            this.componentSizes = componentSizes;
            this.definition = definition;
        }

        public static Chunk Create()
        {
            return new(default, 1, MemoryAddress.Allocate(BitMask.Capacity * sizeof(ushort)), MemoryAddress.Allocate(BitMask.Capacity * sizeof(ushort)));
        }
    }
}