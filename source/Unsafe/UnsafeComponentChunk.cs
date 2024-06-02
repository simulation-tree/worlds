using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Game.Unsafe
{
    public unsafe struct UnsafeComponentChunk
    {
        private UnmanagedList<EntityID> entities;
        private UnmanagedArray<RuntimeType> types;
        private UnmanagedArray<nint> components;
        private readonly uint key;

        private UnsafeComponentChunk(UnmanagedList<EntityID> entities, UnmanagedArray<RuntimeType> types, UnmanagedArray<nint> components, uint key)
        {
            this.entities = entities;
            this.types = types;
            this.components = components;
            this.key = key;
        }

        public static UnsafeComponentChunk* Allocate(ReadOnlySpan<RuntimeType> types)
        {
            UnmanagedList<EntityID> entities = new();
            UnmanagedArray<RuntimeType> typeArray = new(types);
            UnmanagedArray<nint> componentArray = new((uint)types.Length);
            for (uint i = 0; i < types.Length; i++)
            {
                RuntimeType type = types[(int)i];
                componentArray[i] = (nint)UnsafeList.Allocate(type);
            }

            uint key = RuntimeType.CalculateHash(types);
            UnsafeComponentChunk* chunk = Allocations.Allocate<UnsafeComponentChunk>();
            chunk[0] = new(entities, typeArray, componentArray, key);
            return chunk;
        }

        public static bool IsDisposed(UnsafeComponentChunk* chunk)
        {
            return Allocations.IsNull(chunk) || chunk->entities.IsDisposed;
        }

        public static void Free(ref UnsafeComponentChunk* chunk)
        {
            Allocations.ThrowIfNull(chunk);
            chunk->entities.Dispose();
            for (uint i = 0; i < chunk->types.Length; i++)
            {
                UnsafeList* components = (UnsafeList*)chunk->components[i];
                UnsafeList.Free(ref components);
            }

            chunk->types.Dispose();
            chunk->components.Dispose();
            Allocations.Free(ref chunk);
        }

        public static UnmanagedList<EntityID> GetEntities(UnsafeComponentChunk* chunk)
        {
            Allocations.ThrowIfNull(chunk);
            return chunk->entities;
        }

        public static uint GetKey(UnsafeComponentChunk* chunk)
        {
            Allocations.ThrowIfNull(chunk);
            return chunk->key;
        }

        public static ReadOnlySpan<RuntimeType> GetTypes(UnsafeComponentChunk* chunk)
        {
            Allocations.ThrowIfNull(chunk);
            return chunk->types.AsSpan();
        }

        public static void Add(UnsafeComponentChunk* chunk, EntityID entity)
        {
            Allocations.ThrowIfNull(chunk);
            chunk->entities.Add(entity);
            for (uint i = 0; i < chunk->types.Length; i++)
            {
                UnsafeList* list = (UnsafeList*)chunk->components[i];
                UnsafeList.AddDefault(list);
            }
        }

        public static void Remove(UnsafeComponentChunk* chunk, EntityID entity)
        {
            Allocations.ThrowIfNull(chunk);
            uint index = chunk->entities.IndexOf(entity);
            chunk->entities.RemoveAtBySwapping(index);
            for (uint i = 0; i < chunk->types.Length; i++)
            {
                UnsafeList* list = (UnsafeList*)chunk->components[i];
                UnsafeList.RemoveAtBySwapping(list, index);
            }
        }

        public static uint Move(UnsafeComponentChunk* source, EntityID entity, UnsafeComponentChunk* destination)
        {
            Allocations.ThrowIfNull(source);
            Allocations.ThrowIfNull(destination);
            uint oldIndex = source->entities.IndexOf(entity);
            source->entities.RemoveAtBySwapping(oldIndex);
            uint newIndex = destination->entities.Count;
            destination->entities.Add(entity);
            for (uint i = 0; i < destination->types.Length; i++)
            {
                RuntimeType type = destination->types[i];
                UnsafeList* destinationList = (UnsafeList*)destination->components[i];
                UnsafeList.AddDefault(destinationList);
            }

            for (uint i = 0; i < source->types.Length; i++)
            {
                RuntimeType type = source->types[i];
                if (destination->types.Contains(type))
                {
                    uint d = destination->types.IndexOf(type);
                    UnsafeList* destinationList = (UnsafeList*)destination->components[d];
                    UnsafeList* sourceList = (UnsafeList*)source->components[i];
                    UnsafeList.CopyTo(sourceList, oldIndex, destinationList, newIndex);
                }

                UnsafeList* oldList = (UnsafeList*)source->components[i];
                UnsafeList.RemoveAtBySwapping(oldList, oldIndex);
            }

            return newIndex;
        }

        public static UnsafeList* GetComponents(UnsafeComponentChunk* chunk, RuntimeType type)
        {
            Allocations.ThrowIfNull(chunk);
            ReadOnlySpan<RuntimeType> types = chunk->types.AsSpan();
            int index = types.IndexOf(type);
            if (index == -1)
            {
                throw new ArgumentException($"Component list of type {type} not found in chunk.");
            }

            return (UnsafeList*)chunk->components[(uint)index];
        }
    }
}
