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
                if (componentTypesMask == i)
                {
                    ComponentType componentType = ComponentType.All[i];
                    componentArrays[i] = (nint)UnsafeList.Allocate(4, componentType.Size);
                    typeIndices[typeCount++] = i;
                }
            }

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
                ComponentType componentType = new(chunk->typeIndices[i]);
                UnsafeList* components = (UnsafeList*)chunk->componentArrays[componentType];
                UnsafeList.Free(ref components);
            }

            chunk->componentArrays.Dispose();
            chunk->typeIndices.Dispose();
            Allocations.Free(ref chunk);
        }

        public static uint GetCount(UnsafeComponentChunk* chunk)
        {
            Allocations.ThrowIfNull(chunk);

            return chunk->entities.Count;
        }

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
                ComponentType componentType = new(chunk->typeIndices[i]);
                UnsafeList* list = (UnsafeList*)chunk->componentArrays[componentType];
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
                ComponentType componentType = new(chunk->typeIndices[i]);
                UnsafeList* list = (UnsafeList*)chunk->componentArrays[componentType];
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

            //copy from source to destination
            for (uint i = 0; i < destination->typeIndices.Length; i++)
            {
                ComponentType destinationComponentType = new(destination->typeIndices[i]);
                UnsafeList* destinationList = (UnsafeList*)destination->componentArrays[destinationComponentType];
                if (source->typeIndices.Contains(destinationComponentType.index))
                {
                    UnsafeList* sourceList = (UnsafeList*)source->componentArrays[destinationComponentType];
                    UnsafeList.Insert(destinationList, newIndex, UnsafeList.GetElementBytes(sourceList, oldIndex));
                }
                else
                {
                    UnsafeList.AddDefault(destinationList);
                }
            }

            //remove from source
            for (uint i = 0; i < source->typeIndices.Length; i++)
            {
                ComponentType sourceComponentType = new(source->typeIndices[i]);
                UnsafeList* sourceList = (UnsafeList*)source->componentArrays[sourceComponentType];
                UnsafeList.RemoveAtBySwapping(sourceList, oldIndex);
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

        public static ComponentChunk.Entity<C1> GetEntity<C1>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            return new ComponentChunk.Entity<C1>(chunk->entities[index], ref c1);
        }

        public static ComponentChunk.Entity<C1, C2> GetEntity<C1, C2>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            return new ComponentChunk.Entity<C1, C2>(chunk->entities[index], ref c1, ref c2);
        }

        public static ComponentChunk.Entity<C1, C2, C3> GetEntity<C1, C2, C3>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            return new ComponentChunk.Entity<C1, C2, C3>(chunk->entities[index], ref c1, ref c2, ref c3);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4> GetEntity<C1, C2, C3, C4>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5> GetEntity<C1, C2, C3, C4, C5>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6> GetEntity<C1, C2, C3, C4, C5, C6>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C6>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 c6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5, ref c6);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7> GetEntity<C1, C2, C3, C4, C5, C6, C7>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C6>()));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C7>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 c6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 c7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C6>()));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C7>()));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C8>()));
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 c6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 c7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 c8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C6>()));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C7>()));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C8>()));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C9>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 c6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 c7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 c8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 c9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C6>()));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C7>()));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C8>()));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C9>()));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C10>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 c6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 c7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 c8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 c9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 c10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C6>()));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C7>()));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C8>()));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C9>()));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C10>()));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C11>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 c6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 c7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 c8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 c9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 c10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 c11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);
            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C6>()));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C7>()));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C8>()));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C9>()));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C10>()));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C11>()));
            nint list12Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C12>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 c6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 c7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 c8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 c9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 c10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 c11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            ref C12 c12 = ref *(C12*)(list12Address + index * TypeInfo<C12>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11, ref c12);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);
            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C6>()));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C7>()));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C8>()));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C9>()));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C10>()));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C11>()));
            nint list12Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C12>()));
            nint list13Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C13>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 c6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 c7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 c8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 c9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 c10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 c11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            ref C12 c12 = ref *(C12*)(list12Address + index * TypeInfo<C12>.size);
            ref C13 c13 = ref *(C13*)(list13Address + index * TypeInfo<C13>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11, ref c12, ref c13);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);
            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C6>()));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C7>()));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C8>()));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C9>()));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C10>()));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C11>()));
            nint list12Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C12>()));
            nint list13Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C13>()));
            nint list14Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C14>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 c6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 c7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 c8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 c9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 c10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 c11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            ref C12 c12 = ref *(C12*)(list12Address + index * TypeInfo<C12>.size);
            ref C13 c13 = ref *(C13*)(list13Address + index * TypeInfo<C13>.size);
            ref C14 c14 = ref *(C14*)(list14Address + index * TypeInfo<C14>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11, ref c12, ref c13, ref c14);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);
            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C6>()));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C7>()));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C8>()));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C9>()));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C10>()));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C11>()));
            nint list12Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C12>()));
            nint list13Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C13>()));
            nint list14Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C14>()));
            nint list15Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C15>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 c6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 c7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 c8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 c9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 c10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 c11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            ref C12 c12 = ref *(C12*)(list12Address + index * TypeInfo<C12>.size);
            ref C13 c13 = ref *(C13*)(list13Address + index * TypeInfo<C13>.size);
            ref C14 c14 = ref *(C14*)(list14Address + index * TypeInfo<C14>.size);
            ref C15 c15 = ref *(C15*)(list15Address + index * TypeInfo<C15>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11, ref c12, ref c13, ref c14, ref c15);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);
            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C1>()));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C2>()));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C3>()));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C4>()));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C5>()));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C6>()));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C7>()));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C8>()));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C9>()));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C10>()));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C11>()));
            nint list12Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C12>()));
            nint list13Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C13>()));
            nint list14Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C14>()));
            nint list15Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C15>()));
            nint list16Address = UnsafeList.GetStartAddress(GetComponents(chunk, ComponentType.Get<C16>()));
            ref C1 c1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 c2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 c3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 c4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 c5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 c6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 c7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 c8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 c9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 c10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 c11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            ref C12 c12 = ref *(C12*)(list12Address + index * TypeInfo<C12>.size);
            ref C13 c13 = ref *(C13*)(list13Address + index * TypeInfo<C13>.size);
            ref C14 c14 = ref *(C14*)(list14Address + index * TypeInfo<C14>.size);
            ref C15 c15 = ref *(C15*)(list15Address + index * TypeInfo<C15>.size);
            ref C16 c16 = ref *(C16*)(list16Address + index * TypeInfo<C16>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(chunk->entities[index], ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11, ref c12, ref c13, ref c14, ref c15, ref c16);
        }

        [Conditional("DEBUG")]
        private static void ThrowIfComponentTypeIsMissing(UnsafeComponentChunk* chunk, ComponentType componentType)
        {
            if (chunk->typeMask != componentType)
            {
                throw new ArgumentException($"Component type `{componentType}` is missing from the chunk");
            }
        }
    }
}
