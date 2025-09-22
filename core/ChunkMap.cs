using System;
using System.Runtime.CompilerServices;
using Unmanaged;
using Worlds.Pointers;

namespace Worlds
{
    internal unsafe struct ChunkMap
    {
        public ChunkMapPointer* chunkMap;
        public SchemaPointer* schema;

        public ChunkMap(SchemaPointer* schema)
        {
            this.schema = schema;
            chunkMap = MemoryAddress.AllocatePointer<ChunkMapPointer>();
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
            bool* occupied = (bool*)chunkMap->occupied.pointer;
            ChunkKey* values = (ChunkKey*)chunkMap->keys.pointer;
            long* hashCodes = (long*)chunkMap->hashCodes.pointer;
            uint capacityMask = (uint)(capacity - 1);
            uint index = (uint)hashCode & capacityMask;
            uint startIndex = index;

            // main loop
            do
            {
                if (!occupied[index])
                {
                    goto CreateNew;
                }

                if (hashCodes[index] == hashCode)
                {
                    Chunk key = values[index].chunk;
                    if (key.ComponentTypes == definition.componentTypes && key.ArrayTypes == definition.arrayTypes && key.TagTypes == definition.tagTypes)
                    {
                        return key;
                    }
                }

                index = (index + 1) & capacityMask;
            }
            while (index != startIndex);

        CreateNew:
            // resize if necessary
            int newCount = chunkMap->count + 1;
            if (newCount >= capacityMask)
            {
                int newCapacity = capacity * 4;
                Resize(newCapacity, &occupied, &values, &hashCodes);
                capacityMask = (uint)(newCapacity - 1);

                index = (uint)hashCode & capacityMask;
                while (occupied[index])
                {
                    index = (index + 1) & capacityMask;
                }
            }

            // create new chunk here
            chunkMap->count = newCount;
            Chunk newChunk = new(schema, definition.componentTypes, definition.arrayTypes, definition.tagTypes);
            occupied[index] = true;
            hashCodes[index] = hashCode;
            values[index] = new(newChunk);
            chunkMap->chunks.Add(newChunk);
            return newChunk;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithAddedComponent(Chunk sourceChunk, int componentType)
        {
            return GetOrCreate(BitMask.Set(sourceChunk.ComponentTypes, componentType), sourceChunk.ArrayTypes, sourceChunk.TagTypes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithAddedComponents(Chunk sourceChunk, BitMask componentTypes)
        {
            return GetOrCreate(sourceChunk.ComponentTypes | componentTypes, sourceChunk.ArrayTypes, sourceChunk.TagTypes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithRemovedComponent(Chunk sourceChunk, int componentType)
        {
            BitMask componentTypes = BitMask.Clear(sourceChunk.ComponentTypes, componentType);
            if (componentTypes.IsEmpty && sourceChunk.ArrayTypes.IsEmpty && sourceChunk.TagTypes.IsEmpty)
            {
                return chunkMap->defaultChunk;
            }

            return GetOrCreate(componentTypes, sourceChunk.ArrayTypes, sourceChunk.TagTypes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithRemovedComponents(Chunk sourceChunk, BitMask componentTypes)
        {
            componentTypes = sourceChunk.ComponentTypes & ~componentTypes;
            if (componentTypes.IsEmpty && sourceChunk.ArrayTypes.IsEmpty && sourceChunk.TagTypes.IsEmpty)
            {
                return chunkMap->defaultChunk;
            }

            return GetOrCreate(componentTypes, sourceChunk.ArrayTypes, sourceChunk.TagTypes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithAddedArray(Chunk sourceChunk, int arrayType)
        {
            return GetOrCreate(sourceChunk.ComponentTypes, BitMask.Set(sourceChunk.ArrayTypes, arrayType), sourceChunk.TagTypes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithAddedArrays(Chunk sourceChunk, BitMask arrayTypes)
        {
            return GetOrCreate(sourceChunk.ComponentTypes, sourceChunk.ArrayTypes | arrayTypes, sourceChunk.TagTypes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithRemovedArray(Chunk sourceChunk, int arrayType)
        {
            BitMask arrayTypes = BitMask.Clear(sourceChunk.ArrayTypes, arrayType);
            if (sourceChunk.ComponentTypes.IsEmpty && arrayTypes.IsEmpty && sourceChunk.TagTypes.IsEmpty)
            {
                return chunkMap->defaultChunk;
            }

            return GetOrCreate(sourceChunk.ComponentTypes, arrayTypes, sourceChunk.TagTypes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithRemovedArrays(Chunk sourceChunk, BitMask arrayTypes)
        {
            arrayTypes = sourceChunk.ArrayTypes & ~arrayTypes;
            if (sourceChunk.ComponentTypes.IsEmpty && arrayTypes.IsEmpty && sourceChunk.TagTypes.IsEmpty)
            {
                return chunkMap->defaultChunk;
            }

            return GetOrCreate(sourceChunk.ComponentTypes, arrayTypes, sourceChunk.TagTypes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithAddedTag(Chunk sourceChunk, int tagType)
        {
            return GetOrCreate(sourceChunk.ComponentTypes, sourceChunk.ArrayTypes, BitMask.Set(sourceChunk.TagTypes, tagType));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithAddedTags(Chunk sourceChunk, BitMask tagTypes)
        {
            return GetOrCreate(sourceChunk.ComponentTypes, sourceChunk.ArrayTypes, sourceChunk.TagTypes | tagTypes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithRemovedTag(Chunk sourceChunk, int tagType)
        {
            BitMask tagTypes = BitMask.Clear(sourceChunk.TagTypes, tagType);
            if (sourceChunk.ComponentTypes.IsEmpty && sourceChunk.ArrayTypes.IsEmpty && tagTypes.IsEmpty)
            {
                return chunkMap->defaultChunk;
            }

            return GetOrCreate(sourceChunk.ComponentTypes, sourceChunk.ArrayTypes, tagTypes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Chunk GetOrCreateWithRemovedTags(Chunk sourceChunk, BitMask tagTypes)
        {
            tagTypes = sourceChunk.TagTypes & ~tagTypes;
            if (sourceChunk.ComponentTypes.IsEmpty && sourceChunk.ArrayTypes.IsEmpty && tagTypes.IsEmpty)
            {
                return chunkMap->defaultChunk;
            }

            return GetOrCreate(sourceChunk.ComponentTypes, sourceChunk.ArrayTypes, tagTypes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly Chunk GetOrCreate(BitMask componentTypes, BitMask arrayTypes, BitMask tagTypes)
        {
            long hashCode = Definition.GetLongHashCode(componentTypes, arrayTypes, tagTypes);
            int capacity = chunkMap->capacity;
            bool* occupied = (bool*)chunkMap->occupied.pointer;
            ChunkKey* values = (ChunkKey*)chunkMap->keys.pointer;
            long* hashCodes = (long*)chunkMap->hashCodes.pointer;
            uint capacityMask = (uint)(capacity - 1);
            uint index = (uint)hashCode & capacityMask;
            uint startIndex = index;

            do
            {
                if (!occupied[index])
                {
                    goto CreateNew;
                }

                if (hashCodes[index] == hashCode)
                {
                    Chunk key = values[index].chunk;
                    if (key.ComponentTypes == componentTypes && key.ArrayTypes == arrayTypes && key.TagTypes == tagTypes)
                    {
                        return key;
                    }
                }

                index = (index + 1) & capacityMask;
            }
            while (index != startIndex);

        CreateNew:
            int newCount = chunkMap->count + 1;
            if (newCount >= capacityMask)
            {
                int newCapacity = capacity * 4;
                Resize(newCapacity, &occupied, &values, &hashCodes);
                capacityMask = (uint)(newCapacity - 1);

                index = (uint)hashCode & capacityMask;
                while (occupied[index])
                {
                    index = (index + 1) & capacityMask;
                }
            }

            chunkMap->count = newCount;
            Chunk newChunk = new(schema, componentTypes, arrayTypes, tagTypes);
            occupied[index] = true;
            hashCodes[index] = hashCode;
            values[index] = new(newChunk);
            chunkMap->chunks.Add(newChunk);
            return newChunk;
        }

        private readonly void Resize(int newCapacity, bool** occupied, ChunkKey** values, long** hashCodes)
        {
            int oldCapacity = chunkMap->capacity;
            chunkMap->capacity = newCapacity;

            MemoryAddress oldValues = chunkMap->keys;
            MemoryAddress oldOccupied = chunkMap->occupied;
            MemoryAddress oldHashCodes = chunkMap->hashCodes;

            ChunkKey* oldValuesPtr = (ChunkKey*)oldValues.pointer;
            bool* oldOccupiedPtr = (bool*)oldOccupied.pointer;

            chunkMap->keys = MemoryAddress.Allocate(newCapacity * sizeof(ChunkKey));
            chunkMap->hashCodes = MemoryAddress.Allocate(newCapacity * sizeof(long));
            chunkMap->occupied = MemoryAddress.AllocateZeroed(newCapacity);

            *occupied = (bool*)chunkMap->occupied.pointer;
            *hashCodes = (long*)chunkMap->hashCodes.pointer;
            *values = (ChunkKey*)chunkMap->keys.pointer;

            uint newCapacityMask = (uint)(newCapacity - 1);
            for (int i = 0; i < oldCapacity; i++)
            {
                if (oldOccupiedPtr[i])
                {
                    ChunkKey value = oldValuesPtr[i];
                    uint newIndex = (uint)value.definitionHash & newCapacityMask;

                    while ((*occupied)[newIndex])
                    {
                        newIndex = (newIndex + 1) & newCapacityMask;
                    }

                    (*occupied)[newIndex] = true;
                    (*values)[newIndex] = value;
                    (*hashCodes)[newIndex] = value.definitionHash;
                }
            }

            oldValues.Dispose();
            oldOccupied.Dispose();
            oldHashCodes.Dispose();
        }

        private readonly Chunk CreateDefault()
        {
            long hashCode = default(Definition).GetLongHashCode();
            bool* occupied = (bool*)chunkMap->occupied.pointer;
            ChunkKey* values = (ChunkKey*)chunkMap->keys.pointer;
            long* hashCodes = (long*)chunkMap->hashCodes.pointer;
            Chunk newDefaultChunk = new(schema);
            uint index = (uint)hashCode & (uint)(chunkMap->capacity - 1);
            occupied[index] = true;
            hashCodes[index] = hashCode;
            values[index] = new(newDefaultChunk);
            chunkMap->chunks.Add(newDefaultChunk);
            chunkMap->count = 1;
            return newDefaultChunk;
        }

        internal readonly void UpdateDefaultChunkStrideToMatchSchema()
        {
            chunkMap->defaultChunk.UpdateStrideToMatchSchema(schema->componentRowSize);
        }

        public readonly void Clear()
        {
            for (int i = chunkMap->chunks.Count - 1; i > 0; i--) // start at 1 to skip the default chunk
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