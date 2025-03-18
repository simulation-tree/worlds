﻿using System;
using Unmanaged;
using Pointer = Worlds.Pointers.ChunkMap;

namespace Worlds
{
    internal unsafe struct ChunkMap
    {
        public Pointer* chunkMap;

        public ChunkMap(Schema schema)
        {
            ref Pointer chunkMap = ref MemoryAddress.Allocate<Pointer>();
            chunkMap = new(schema);
            fixed (Pointer* pointer = &chunkMap)
            {
                this.chunkMap = pointer;
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < chunkMap->chunks.Count; i++)
            {
                chunkMap->chunks[i].Dispose();
            }

            chunkMap->keys.Dispose();
            chunkMap->hashCodes.Dispose();
            chunkMap->occupied.Dispose();
            chunkMap->chunks.Dispose();
            MemoryAddress.Free(ref chunkMap);
            chunkMap = default;
        }

        private readonly void Resize(int newCapacity, ref Span<bool> occupied, ref Span<ChunkKey> values, ref Span<long> hashCodes)
        {
            int oldCapacity = chunkMap->capacity;
            chunkMap->capacity = newCapacity;

            MemoryAddress oldValues = chunkMap->keys;
            MemoryAddress oldOccupied = chunkMap->occupied;
            MemoryAddress oldKeyHashCodes = chunkMap->hashCodes;

            chunkMap->keys = MemoryAddress.Allocate(newCapacity * sizeof(ChunkKey));
            chunkMap->hashCodes = MemoryAddress.Allocate(newCapacity * sizeof(long));
            chunkMap->occupied = MemoryAddress.AllocateZeroed(newCapacity);
            Span<ChunkKey> oldValuesSpan = new(oldValues.Pointer, oldCapacity);
            Span<bool> oldOccupiedSpan = new(oldOccupied.Pointer, oldCapacity);
            occupied = new(chunkMap->occupied.Pointer, newCapacity);
            hashCodes = new(chunkMap->hashCodes.Pointer, newCapacity);
            values = new(chunkMap->keys.Pointer, newCapacity);

            for (int i = 0; i < oldCapacity; i++)
            {
                if (oldOccupiedSpan[i])
                {
                    ChunkKey value = oldValuesSpan[i];
                    long hashCode = value.GetLongHashCode();
                    int index = GetIndex(hashCode, newCapacity);
                    while (occupied[index])
                    {
                        index = (index + 1) % newCapacity;
                    }

                    occupied[index] = true;
                    values[index] = value;
                    hashCodes[index] = hashCode;
                }
            }

            oldValues.Dispose();
            oldOccupied.Dispose();
            oldKeyHashCodes.Dispose();
        }

        /// <summary>
        /// Retrieves an existing <see cref="Chunk"/> for the given <paramref name="definition"/>,
        /// or creates a new instance.
        /// </summary>
        /// <returns><see langword="true"/> if created.</returns>
        public readonly Chunk GetOrCreate(Definition definition)
        {
            int capacity = chunkMap->capacity;
            long hashCode = definition.GetLongHashCode();
            int index = GetIndex(hashCode, capacity);
            int startIndex = index;
            Span<bool> occupied = new(chunkMap->occupied.Pointer, capacity);
            Span<ChunkKey> values = new(chunkMap->keys.Pointer, capacity);
            Span<long> hashCodes = new(chunkMap->hashCodes.Pointer, capacity);
            while (occupied[index])
            {
                if (hashCodes[index] == hashCode)
                {
                    ChunkKey key = values[index];
                    if (key.Equals(definition))
                    {
                        return key.chunk;
                    }
                }

                index = (index + 1) % capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            int newCount = chunkMap->count + 1;
            if (newCount > capacity)
            {
                capacity *= 4;
                Resize(capacity, ref occupied, ref values, ref hashCodes);
            }

            chunkMap->count = newCount;
            while (occupied[index])
            {
                index = (index + 1) % capacity;
            }

            Chunk newChunk = new(definition, chunkMap->schema);
            occupied[index] = true;
            hashCodes[index] = hashCode;
            values[index] = new(newChunk);
            chunkMap->chunks.Add(newChunk);
            return newChunk;
        }

        public readonly void Clear()
        {
            for (int i = 0; i < chunkMap->chunks.Count; i++)
            {
                chunkMap->chunks[i].Dispose();
            }

            chunkMap->chunks.Clear();
            chunkMap->occupied.Clear(chunkMap->capacity);
            chunkMap->count = 0;
        }

        private static int GetIndex(long hashCode, int capacity)
        {
            unchecked
            {
                return (int)(((uint)hashCode) % (uint)capacity);
            }
        }
    }
}