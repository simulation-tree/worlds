using System;
using System.Diagnostics;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation.Unsafe
{
    public unsafe struct UnsafeComponentChunk
    {
        private UnmanagedList<uint> entities;
        private UnmanagedArray<RuntimeType> types;
        private UnmanagedArray<nint> components;
        private readonly int key;

        private UnsafeComponentChunk(UnmanagedList<uint> entities, UnmanagedArray<RuntimeType> types, UnmanagedArray<nint> components, int key)
        {
            this.entities = entities;
            this.types = types;
            this.components = components;
            this.key = key;
        }

        public static UnsafeComponentChunk* Allocate(USpan<RuntimeType> types)
        {
            UnmanagedList<uint> entities = UnmanagedList<uint>.Create();
            UnmanagedArray<RuntimeType> typeArray = new(types);
            UnmanagedArray<nint> componentArray = new(types.length);
            for (uint i = 0; i < types.length; i++)
            {
                RuntimeType type = types[i];
                componentArray[i] = (nint)UnsafeList.Allocate(type);
            }

            int key = RuntimeType.CombineHash(types);
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

        public static UnmanagedList<uint> GetEntities(UnsafeComponentChunk* chunk)
        {
            Allocations.ThrowIfNull(chunk);
            return chunk->entities;
        }

        public static int GetKey(UnsafeComponentChunk* chunk)
        {
            Allocations.ThrowIfNull(chunk);
            return chunk->key;
        }

        public static USpan<RuntimeType> GetTypes(UnsafeComponentChunk* chunk)
        {
            Allocations.ThrowIfNull(chunk);
            return chunk->types.AsSpan();
        }

        public static uint Add(UnsafeComponentChunk* chunk, uint entity)
        {
            Allocations.ThrowIfNull(chunk);
            chunk->entities.Add(entity);
            for (uint i = 0; i < chunk->types.Length; i++)
            {
                UnsafeList* list = (UnsafeList*)chunk->components[i];
                UnsafeList.AddDefault(list);
            }

            return chunk->entities.Count - 1;
        }

        public static void Remove(UnsafeComponentChunk* chunk, uint entity)
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

        public static uint Move(UnsafeComponentChunk* source, uint entity, UnsafeComponentChunk* destination)
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
                    UnsafeList.CopyElementTo(sourceList, oldIndex, destinationList, newIndex);
                }

                UnsafeList* oldList = (UnsafeList*)source->components[i];
                UnsafeList.RemoveAtBySwapping(oldList, oldIndex);
            }

            return newIndex;
        }

        public static UnsafeList* GetComponents(UnsafeComponentChunk* chunk, RuntimeType type)
        {
            Allocations.ThrowIfNull(chunk);
            ThrowIfComponentTypeIsMissing(chunk, type);
            USpan<RuntimeType> types = chunk->types.AsSpan();
            uint index = types.IndexOf(type);
            return (UnsafeList*)chunk->components[index];
        }

        [Conditional("DEBUG")]
        private static void ThrowIfComponentTypeIsMissing(UnsafeComponentChunk* chunk, RuntimeType type)
        {
            USpan<RuntimeType> types = chunk->types.AsSpan();
            if (!types.Contains(type))
            {
                throw new ArgumentException($"Component list of type {type} not found in chunk.");
            }
        }
    }
}
