using System;
using Unmanaged;
using Worlds.Pointers;

namespace Worlds
{
    internal unsafe struct ChunkMap
    {
        public ChunkMapPointer* chunkMap;

        public ChunkMap(Schema schema)
        {
            chunkMap = MemoryAddress.AllocatePointer<ChunkMapPointer>();
            chunkMap->schema = schema;
            chunkMap->count = 0;
            chunkMap->capacity = 32;
            chunkMap->chunks = new(32);
            chunkMap->keys = MemoryAddress.Allocate(32 * sizeof(ChunkKey));
            chunkMap->hashCodes = MemoryAddress.Allocate(32 * sizeof(ulong));
            chunkMap->occupied = MemoryAddress.AllocateZeroed(32);
            chunkMap->defaultChunk = CreateDefault();
        }

        public void Dispose()
        {
            Span<Chunk> chunks = chunkMap->chunks.AsSpan();
            for (int i = 0; i < chunks.Length; i++)
            {
                chunks[i].Dispose();
            }

            chunkMap->keys.Dispose();
            chunkMap->hashCodes.Dispose();
            chunkMap->occupied.Dispose();
            chunkMap->chunks.Dispose();
            MemoryAddress.Free(ref chunkMap);
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
            newCapacity--;
            for (int i = 0; i < oldCapacity; i++)
            {
                if (oldOccupiedSpan[i])
                {
                    ChunkKey value = oldValuesSpan[i];
                    long hashCode = value.definition.GetLongHashCode();
                    int index = unchecked((int)(((uint)hashCode) & (uint)newCapacity));
                    while (occupied[index])
                    {
                        index = (index + 1) & newCapacity;
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
            if (definition == default)
            {
                return chunkMap->defaultChunk;
            }

            int capacity = chunkMap->capacity;
            long hashCode = definition.GetLongHashCode();
            Span<bool> occupied = new(chunkMap->occupied.Pointer, capacity);
            Span<ChunkKey> values = new(chunkMap->keys.Pointer, capacity);
            Span<long> hashCodes = new(chunkMap->hashCodes.Pointer, capacity);
            capacity--;
            int index = unchecked((int)(((uint)hashCode) & (uint)capacity));
            int startIndex = index;
            while (occupied[index])
            {
                if (hashCodes[index] == hashCode)
                {
                    ChunkKey key = values[index];
                    if (key.definition.componentTypes.a == definition.componentTypes.a &&
                           key.definition.componentTypes.b == definition.componentTypes.b &&
                           key.definition.componentTypes.c == definition.componentTypes.c &&
                           key.definition.componentTypes.d == definition.componentTypes.d &&
                           key.definition.arrayTypes.a == definition.arrayTypes.a &&
                           key.definition.arrayTypes.b == definition.arrayTypes.b &&
                           key.definition.arrayTypes.c == definition.arrayTypes.c &&
                           key.definition.arrayTypes.d == definition.arrayTypes.d &&
                           key.definition.tagTypes.a == definition.tagTypes.a &&
                           key.definition.tagTypes.b == definition.tagTypes.b &&
                           key.definition.tagTypes.c == definition.tagTypes.c &&
                           key.definition.tagTypes.d == definition.tagTypes.d)
                    {
                        return key.chunk;
                    }
                }

                index = (index + 1) & capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            //resize if necessary
            int newCount = chunkMap->count + 1;
            if (newCount >= capacity)
            {
                capacity++;
                capacity *= 4;
                Resize(capacity, ref occupied, ref values, ref hashCodes);
            }

            //create new chunk here
            chunkMap->count = newCount;
            Chunk newChunk = new(chunkMap->schema, definition);
            occupied[index] = true;
            hashCodes[index] = hashCode;
            values[index] = new(newChunk);
            chunkMap->chunks.Add(newChunk);
            return newChunk;
        }

        private readonly Chunk CreateDefault()
        {
            int capacity = chunkMap->capacity;
            long hashCode = default(Definition).GetLongHashCode();
            Span<bool> occupied = new(chunkMap->occupied.Pointer, capacity);
            Span<ChunkKey> values = new(chunkMap->keys.Pointer, capacity);
            Span<long> hashCodes = new(chunkMap->hashCodes.Pointer, capacity);
            Chunk newDefaultChunk = new(chunkMap->schema);
            capacity--;
            int index = unchecked((int)(((uint)hashCode) & (uint)capacity));
            occupied[index] = true;
            hashCodes[index] = hashCode;
            values[index] = new(newDefaultChunk);
            chunkMap->chunks.Add(newDefaultChunk);
            chunkMap->count = 1;
            return newDefaultChunk;
        }

        internal readonly void UpdateDefaultChunkStrideToMatchSchema()
        {
            chunkMap->defaultChunk.UpdateStrideToMatchSchema();
        }

        public readonly void Clear()
        {
            for (int i = chunkMap->chunks.Count - 1; i > 0; i--) //start at 1 to skip the default chunk
            {
                chunkMap->chunks[i].Dispose();
                chunkMap->chunks.RemoveAt(i);
            }

            chunkMap->occupied.Clear(chunkMap->capacity);
            chunkMap->occupied.WriteElement(0, true);
            chunkMap->count = 1;
        }
    }
}