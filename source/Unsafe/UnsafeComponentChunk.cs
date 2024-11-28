using Collections;
using Collections.Unsafe;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds.Unsafe
{
    /// <summary>
    /// Opaque pointer implementation of a <see cref="ComponentChunk"/>.
    /// </summary>
    public unsafe struct UnsafeComponentChunk
    {
        private List<uint> entities;
        private Array<nint> componentArrays;
        private readonly Array<byte> typeIndices;
        private readonly BitSet typeMask;

        private UnsafeComponentChunk(BitSet componentTypesMask, Array<nint> componentArrays, Array<byte> typeIndices)
        {
            entities = new(4);
            this.typeIndices = typeIndices;
            this.componentArrays = componentArrays;
            typeMask = componentTypesMask;
        }

        /// <summary>
        /// Allocates a new <see cref="UnsafeComponentChunk"/> with the given <paramref name="componentTypesMask"/>.
        /// </summary>
        public static UnsafeComponentChunk* Allocate(BitSet componentTypesMask)
        {
            Array<nint> componentArrays = new(BitSet.Capacity);
            USpan<ComponentType> componentTypes = stackalloc ComponentType[BitSet.Capacity];
            USpan<byte> typeIndices = stackalloc byte[BitSet.Capacity];
            byte typeCount = 0;
            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (componentTypesMask.Contains(i))
                {
                    ComponentType componentType = new(i);
                    componentArrays[i] = (nint)UnsafeList.Allocate(4, componentType.Size);
                    typeIndices[typeCount++] = i;
                }
            }

            int key = componentTypesMask.GetHashCode();
            UnsafeComponentChunk* chunk = Allocations.Allocate<UnsafeComponentChunk>();
            chunk[0] = new(componentTypesMask, componentArrays, new(typeIndices.Slice(0, typeCount)));
            return chunk;
        }

        /// <summary>
        /// Frees the given <paramref name="chunk"/>.
        /// </summary>
        public static void Free(ref UnsafeComponentChunk* chunk)
        {
            Allocations.ThrowIfNull(chunk);

            chunk->entities.Dispose();
            uint typeCount = chunk->typeIndices.Length;
            for (byte i = 0; i < typeCount; i++)
            {
                byte typeIndex = chunk->typeIndices[i];
                UnsafeList* components = (UnsafeList*)chunk->componentArrays[typeIndex];
                UnsafeList.Free(ref components);
            }

            chunk->componentArrays.Dispose();
            chunk->typeIndices.Dispose();
            Allocations.Free(ref chunk);
        }

        /// <summary>
        /// Retrieves the list of entities stored.
        /// </summary>
        public static List<uint> GetEntities(UnsafeComponentChunk* chunk)
        {
            Allocations.ThrowIfNull(chunk);

            return chunk->entities;
        }

        /// <summary>
        /// Retrieves the <see cref="BitSet"/> representing the types of components in the chunk.
        /// </summary>
        public static BitSet GetTypesMask(UnsafeComponentChunk* chunk)
        {
            Allocations.ThrowIfNull(chunk);

            return chunk->typeMask;
        }

        /// <summary>
        /// Adds a new entity to the chunk.
        /// </summary>
        public static uint Add(UnsafeComponentChunk* chunk, uint entity)
        {
            Allocations.ThrowIfNull(chunk);

            chunk->entities.Add(entity);
            uint typeCount = chunk->typeIndices.Length;
            for (byte i = 0; i < typeCount; i++)
            {
                byte typeIndex = chunk->typeIndices[i];
                UnsafeList* list = (UnsafeList*)chunk->componentArrays[typeIndex];
                UnsafeList.AddDefault(list);
            }

            return chunk->entities.Count - 1;
        }

        /// <summary>
        /// Removes the <paramref name="entity"/> from the chunk.
        /// </summary>
        public static void Remove(UnsafeComponentChunk* chunk, uint entity)
        {
            Allocations.ThrowIfNull(chunk);

            uint index = chunk->entities.IndexOf(entity);
            chunk->entities.RemoveAtBySwapping(index);
            uint typeCount = chunk->typeIndices.Length;
            for (byte i = 0; i < typeCount; i++)
            {
                byte typeIndex = chunk->typeIndices[i];
                UnsafeList* list = (UnsafeList*)chunk->componentArrays[typeIndex];
                UnsafeList.RemoveAtBySwapping(list, index);
            }
        }

        /// <summary>
        /// Moves the <paramref name="entity"/> and all of its components to the <paramref name="destination"/> chunk.
        /// </summary>
        /// <returns>New local index in the <paramref name="destination"/> chunk.</returns>
        public static uint Move(UnsafeComponentChunk* source, uint entity, UnsafeComponentChunk* destination)
        {
            Allocations.ThrowIfNull(source);
            Allocations.ThrowIfNull(destination);

            uint oldIndex = source->entities.IndexOf(entity);
            source->entities.RemoveAtBySwapping(oldIndex);
            uint newIndex = destination->entities.Count;
            destination->entities.Add(entity);

            //add a default slot into destination, then copy from source
            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (destination->typeMask.Contains(i))
                {
                    UnsafeList* destinationList = (UnsafeList*)destination->componentArrays[i];
                    UnsafeList.AddDefault(destinationList);

                    if (source->typeMask.Contains(i))
                    {
                        UnsafeList* sourceList = (UnsafeList*)source->componentArrays[i];
                        UnsafeList.CopyElementTo(sourceList, oldIndex, destinationList, newIndex);
                        UnsafeList.RemoveAtBySwapping(sourceList, oldIndex);
                    }
                }
                else
                {
                    if (source->typeMask.Contains(i))
                    {
                        UnsafeList* sourceList = (UnsafeList*)source->componentArrays[i];
                        UnsafeList.RemoveAtBySwapping(sourceList, oldIndex);
                    }
                }
            }

            return newIndex;
        }

        /// <summary>
        /// Retrieves a list of components of the given <paramref name="componentType"/>.
        /// </summary>
        public static UnsafeList* GetComponents(UnsafeComponentChunk* chunk, ComponentType componentType)
        {
            Allocations.ThrowIfNull(chunk);
            ThrowIfComponentTypeIsMissing(chunk, componentType);

            return (UnsafeList*)chunk->componentArrays[componentType];
        }

        [Conditional("DEBUG")]
        private static void ThrowIfComponentTypeIsMissing(UnsafeComponentChunk* chunk, ComponentType componentType)
        {
            if (!chunk->typeMask.Contains(componentType))
            {
                throw new ArgumentException($"Component type `{componentType}` is missing from the chunk");
            }
        }
    }
}
