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

            ComponentType c1 = ComponentType.Get<C1>();
            return GetEntity<C1>(chunk, index, c1);
        }

        public static ComponentChunk.Entity<C1> GetEntity<C1>(UnsafeComponentChunk* chunk, uint index, ComponentType c1) where C1 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            return new ComponentChunk.Entity<C1>(chunk->entities[index], ref v1);
        }

        public static ComponentChunk.Entity<C1, C2> GetEntity<C1, C2>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            return GetEntity<C1, C2>(chunk, index, c1, c2);
        }

        public static ComponentChunk.Entity<C1, C2> GetEntity<C1, C2>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2) where C1 : unmanaged where C2 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            return new ComponentChunk.Entity<C1, C2>(chunk->entities[index], ref v1, ref v2);
        }

        public static ComponentChunk.Entity<C1, C2, C3> GetEntity<C1, C2, C3>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            return GetEntity<C1, C2, C3>(chunk, index, c1, c2, c3);
        }

        public static ComponentChunk.Entity<C1, C2, C3> GetEntity<C1, C2, C3>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            return new ComponentChunk.Entity<C1, C2, C3>(chunk->entities[index], ref v1, ref v2, ref v3);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4> GetEntity<C1, C2, C3, C4>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            return GetEntity<C1, C2, C3, C4>(chunk, index, c1, c2, c3, c4);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4> GetEntity<C1, C2, C3, C4>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5> GetEntity<C1, C2, C3, C4, C5>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            return GetEntity<C1, C2, C3, C4, C5>(chunk, index, c1, c2, c3, c4, c5);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5> GetEntity<C1, C2, C3, C4, C5>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6> GetEntity<C1, C2, C3, C4, C5, C6>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            ComponentType c6 = ComponentType.Get<C6>();
            return GetEntity<C1, C2, C3, C4, C5, C6>(chunk, index, c1, c2, c3, c4, c5, c6);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6> GetEntity<C1, C2, C3, C4, C5, C6>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, c6));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 v6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7> GetEntity<C1, C2, C3, C4, C5, C6, C7>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            ComponentType c6 = ComponentType.Get<C6>();
            ComponentType c7 = ComponentType.Get<C7>();
            return GetEntity<C1, C2, C3, C4, C5, C6, C7>(chunk, index, c1, c2, c3, c4, c5, c6, c7);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7> GetEntity<C1, C2, C3, C4, C5, C6, C7>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, c6));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, c7));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 v6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 v7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            ComponentType c6 = ComponentType.Get<C6>();
            ComponentType c7 = ComponentType.Get<C7>();
            ComponentType c8 = ComponentType.Get<C8>();
            return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, c6));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, c7));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, c8));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 v6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 v7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 v8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            ComponentType c6 = ComponentType.Get<C6>();
            ComponentType c7 = ComponentType.Get<C7>();
            ComponentType c8 = ComponentType.Get<C8>();
            ComponentType c9 = ComponentType.Get<C9>();
            return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, c6));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, c7));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, c8));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, c9));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 v6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 v7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 v8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 v9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            ComponentType c6 = ComponentType.Get<C6>();
            ComponentType c7 = ComponentType.Get<C7>();
            ComponentType c8 = ComponentType.Get<C8>();
            ComponentType c9 = ComponentType.Get<C9>();
            ComponentType c10 = ComponentType.Get<C10>();
            return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, c6));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, c7));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, c8));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, c9));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, c10));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 v6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 v7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 v8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 v9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 v10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            ComponentType c6 = ComponentType.Get<C6>();
            ComponentType c7 = ComponentType.Get<C7>();
            ComponentType c8 = ComponentType.Get<C8>();
            ComponentType c9 = ComponentType.Get<C9>();
            ComponentType c10 = ComponentType.Get<C10>();
            ComponentType c11 = ComponentType.Get<C11>();
            return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, c6));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, c7));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, c8));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, c9));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, c10));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, c11));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 v6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 v7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 v8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 v9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 v10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 v11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            ComponentType c6 = ComponentType.Get<C6>();
            ComponentType c7 = ComponentType.Get<C7>();
            ComponentType c8 = ComponentType.Get<C8>();
            ComponentType c9 = ComponentType.Get<C9>();
            ComponentType c10 = ComponentType.Get<C10>();
            ComponentType c11 = ComponentType.Get<C11>();
            ComponentType c12 = ComponentType.Get<C12>();
            return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, c6));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, c7));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, c8));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, c9));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, c10));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, c11));
            nint list12Address = UnsafeList.GetStartAddress(GetComponents(chunk, c12));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 v6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 v7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 v8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 v9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 v10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 v11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            ref C12 v12 = ref *(C12*)(list12Address + index * TypeInfo<C12>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11, ref v12);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            ComponentType c6 = ComponentType.Get<C6>();
            ComponentType c7 = ComponentType.Get<C7>();
            ComponentType c8 = ComponentType.Get<C8>();
            ComponentType c9 = ComponentType.Get<C9>();
            ComponentType c10 = ComponentType.Get<C10>();
            ComponentType c11 = ComponentType.Get<C11>();
            ComponentType c12 = ComponentType.Get<C12>();
            ComponentType c13 = ComponentType.Get<C13>();
            return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, c6));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, c7));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, c8));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, c9));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, c10));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, c11));
            nint list12Address = UnsafeList.GetStartAddress(GetComponents(chunk, c12));
            nint list13Address = UnsafeList.GetStartAddress(GetComponents(chunk, c13));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 v6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 v7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 v8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 v9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 v10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 v11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            ref C12 v12 = ref *(C12*)(list12Address + index * TypeInfo<C12>.size);
            ref C13 v13 = ref *(C13*)(list13Address + index * TypeInfo<C13>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11, ref v12, ref v13);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            ComponentType c6 = ComponentType.Get<C6>();
            ComponentType c7 = ComponentType.Get<C7>();
            ComponentType c8 = ComponentType.Get<C8>();
            ComponentType c9 = ComponentType.Get<C9>();
            ComponentType c10 = ComponentType.Get<C10>();
            ComponentType c11 = ComponentType.Get<C11>();
            ComponentType c12 = ComponentType.Get<C12>();
            ComponentType c13 = ComponentType.Get<C13>();
            ComponentType c14 = ComponentType.Get<C14>();
            return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13, ComponentType c14) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, c6));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, c7));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, c8));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, c9));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, c10));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, c11));
            nint list12Address = UnsafeList.GetStartAddress(GetComponents(chunk, c12));
            nint list13Address = UnsafeList.GetStartAddress(GetComponents(chunk, c13));
            nint list14Address = UnsafeList.GetStartAddress(GetComponents(chunk, c14));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 v6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 v7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 v8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 v9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 v10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 v11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            ref C12 v12 = ref *(C12*)(list12Address + index * TypeInfo<C12>.size);
            ref C13 v13 = ref *(C13*)(list13Address + index * TypeInfo<C13>.size);
            ref C14 v14 = ref *(C14*)(list14Address + index * TypeInfo<C14>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11, ref v12, ref v13, ref v14);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            ComponentType c6 = ComponentType.Get<C6>();
            ComponentType c7 = ComponentType.Get<C7>();
            ComponentType c8 = ComponentType.Get<C8>();
            ComponentType c9 = ComponentType.Get<C9>();
            ComponentType c10 = ComponentType.Get<C10>();
            ComponentType c11 = ComponentType.Get<C11>();
            ComponentType c12 = ComponentType.Get<C12>();
            ComponentType c13 = ComponentType.Get<C13>();
            ComponentType c14 = ComponentType.Get<C14>();
            ComponentType c15 = ComponentType.Get<C15>();
            return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13, ComponentType c14, ComponentType c15) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, c6));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, c7));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, c8));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, c9));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, c10));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, c11));
            nint list12Address = UnsafeList.GetStartAddress(GetComponents(chunk, c12));
            nint list13Address = UnsafeList.GetStartAddress(GetComponents(chunk, c13));
            nint list14Address = UnsafeList.GetStartAddress(GetComponents(chunk, c14));
            nint list15Address = UnsafeList.GetStartAddress(GetComponents(chunk, c15));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 v6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 v7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 v8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 v9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 v10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 v11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            ref C12 v12 = ref *(C12*)(list12Address + index * TypeInfo<C12>.size);
            ref C13 v13 = ref *(C13*)(list13Address + index * TypeInfo<C13>.size);
            ref C14 v14 = ref *(C14*)(list14Address + index * TypeInfo<C14>.size);
            ref C15 v15 = ref *(C15*)(list15Address + index * TypeInfo<C15>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11, ref v12, ref v13, ref v14, ref v15);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(UnsafeComponentChunk* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            ComponentType c1 = ComponentType.Get<C1>();
            ComponentType c2 = ComponentType.Get<C2>();
            ComponentType c3 = ComponentType.Get<C3>();
            ComponentType c4 = ComponentType.Get<C4>();
            ComponentType c5 = ComponentType.Get<C5>();
            ComponentType c6 = ComponentType.Get<C6>();
            ComponentType c7 = ComponentType.Get<C7>();
            ComponentType c8 = ComponentType.Get<C8>();
            ComponentType c9 = ComponentType.Get<C9>();
            ComponentType c10 = ComponentType.Get<C10>();
            ComponentType c11 = ComponentType.Get<C11>();
            ComponentType c12 = ComponentType.Get<C12>();
            ComponentType c13 = ComponentType.Get<C13>();
            ComponentType c14 = ComponentType.Get<C14>();
            ComponentType c15 = ComponentType.Get<C15>();
            ComponentType c16 = ComponentType.Get<C16>();
            return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16);
        }

        public static ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(UnsafeComponentChunk* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13, ComponentType c14, ComponentType c15, ComponentType c16) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            Allocations.ThrowIfNull(chunk);

            nint list1Address = UnsafeList.GetStartAddress(GetComponents(chunk, c1));
            nint list2Address = UnsafeList.GetStartAddress(GetComponents(chunk, c2));
            nint list3Address = UnsafeList.GetStartAddress(GetComponents(chunk, c3));
            nint list4Address = UnsafeList.GetStartAddress(GetComponents(chunk, c4));
            nint list5Address = UnsafeList.GetStartAddress(GetComponents(chunk, c5));
            nint list6Address = UnsafeList.GetStartAddress(GetComponents(chunk, c6));
            nint list7Address = UnsafeList.GetStartAddress(GetComponents(chunk, c7));
            nint list8Address = UnsafeList.GetStartAddress(GetComponents(chunk, c8));
            nint list9Address = UnsafeList.GetStartAddress(GetComponents(chunk, c9));
            nint list10Address = UnsafeList.GetStartAddress(GetComponents(chunk, c10));
            nint list11Address = UnsafeList.GetStartAddress(GetComponents(chunk, c11));
            nint list12Address = UnsafeList.GetStartAddress(GetComponents(chunk, c12));
            nint list13Address = UnsafeList.GetStartAddress(GetComponents(chunk, c13));
            nint list14Address = UnsafeList.GetStartAddress(GetComponents(chunk, c14));
            nint list15Address = UnsafeList.GetStartAddress(GetComponents(chunk, c15));
            nint list16Address = UnsafeList.GetStartAddress(GetComponents(chunk, c16));
            ref C1 v1 = ref *(C1*)(list1Address + index * TypeInfo<C1>.size);
            ref C2 v2 = ref *(C2*)(list2Address + index * TypeInfo<C2>.size);
            ref C3 v3 = ref *(C3*)(list3Address + index * TypeInfo<C3>.size);
            ref C4 v4 = ref *(C4*)(list4Address + index * TypeInfo<C4>.size);
            ref C5 v5 = ref *(C5*)(list5Address + index * TypeInfo<C5>.size);
            ref C6 v6 = ref *(C6*)(list6Address + index * TypeInfo<C6>.size);
            ref C7 v7 = ref *(C7*)(list7Address + index * TypeInfo<C7>.size);
            ref C8 v8 = ref *(C8*)(list8Address + index * TypeInfo<C8>.size);
            ref C9 v9 = ref *(C9*)(list9Address + index * TypeInfo<C9>.size);
            ref C10 v10 = ref *(C10*)(list10Address + index * TypeInfo<C10>.size);
            ref C11 v11 = ref *(C11*)(list11Address + index * TypeInfo<C11>.size);
            ref C12 v12 = ref *(C12*)(list12Address + index * TypeInfo<C12>.size);
            ref C13 v13 = ref *(C13*)(list13Address + index * TypeInfo<C13>.size);
            ref C14 v14 = ref *(C14*)(list14Address + index * TypeInfo<C14>.size);
            ref C15 v15 = ref *(C15*)(list15Address + index * TypeInfo<C15>.size);
            ref C16 v16 = ref *(C16*)(list16Address + index * TypeInfo<C16>.size);
            return new ComponentChunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11, ref v12, ref v13, ref v14, ref v15, ref v16);
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
