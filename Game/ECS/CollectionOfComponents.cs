using System;
using Unmanaged.Collections;

namespace Game.ECS
{
    internal unsafe sealed class CollectionOfComponents : IDisposable
    {
        private readonly ComponentTypeMask types;
        private readonly UnmanagedList<EntityID> entities;
        private readonly UnmanagedArray<nint> lists;

        public UnmanagedList<EntityID> Entities => entities;

        public CollectionOfComponents(ComponentTypeMask types)
        {
            this.types = types;
            lists = new(ComponentTypeMask.MaxValues);
            for (uint i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                if (types.Contains(type))
                {
                    lists[i] = (nint)UnsafeList.Allocate(type.RuntimeType);
                }
            }

            entities = new();
        }

        public void Add(EntityID id)
        {
            if (entities.Contains(id))
            {
                throw new InvalidOperationException("Entity already exists.");
            }

            entities.Add(id);
            for (uint i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                if (types.Contains(type))
                {
                    UnsafeList* list = (UnsafeList*)lists[i];
                    UnsafeList.AddDefault(list);
                }
            }
        }

        public void Remove(EntityID entity)
        {
            uint index = entities.IndexOf(entity);
            entities.RemoveAtBySwapping(index);
            for (uint i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                if (types.Contains(type))
                {
                    UnsafeList* list = (UnsafeList*)lists[i];
                    UnsafeList.RemoveAtBySwapping(list, index);
                }
            }
        }

        public ref T GetComponentRef<T>(EntityID entity) where T : unmanaged
        {
            uint index = entities.IndexOf(entity);
            return ref GetComponentRef<T>(index);
        }

        public ref T GetComponentRef<T>(uint index) where T : unmanaged
        {
            ComponentType type = ComponentType.Get<T>();
            UnsafeList* list = GetComponents(type);
            return ref UnsafeList.GetRef<T>(list, index);
        }

        public Span<byte> GetComponentBytes(EntityID entity, ComponentType type)
        {
            uint index = entities.IndexOf(entity);
            return GetComponentBytes(index, type);
        }

        public Span<byte> GetComponentBytes(uint index, ComponentType type)
        {
            UnsafeList* list = GetComponents(type);
            return UnsafeList.Get(list, index);
        }

        public UnmanagedList<T> GetComponents<T>() where T : unmanaged
        {
            ComponentType type = ComponentType.Get<T>();
            UnsafeList* list = GetComponents(type);
            return new(list);
        }

        public UnsafeList* GetComponents(ComponentType type)
        {
            return (UnsafeList*)lists[(uint)(type.value - 1)];
        }

        /// <summary>
        /// Moves the entity to another block by removing it, and then adding
        /// to the destination.
        /// </summary>
        public uint MoveTo(EntityID id, CollectionOfComponents destination)
        {
            if (destination.entities.Contains(id))
            {
                throw new InvalidOperationException("Entity already exists in destination.");
            }

            uint oldIndex = entities.IndexOf(id);
            entities.RemoveAtBySwapping(oldIndex);

            uint newIndex = destination.entities.Count;
            destination.entities.Add(id);

            for (uint i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                bool sourceHas = types.Contains(type);
                if (destination.types.Contains(type))
                {
                    UnsafeList* destinationList = (UnsafeList*)destination.lists[i];
                    UnsafeList.AddDefault(destinationList);

                    if (sourceHas)
                    {
                        //copy
                        UnsafeList* sourceList = (UnsafeList*)lists[i];
                        UnsafeList.CopyTo(sourceList, oldIndex, destinationList, newIndex);
                    }
                }
                
                if (sourceHas)
                {
                    //remove
                    UnsafeList* oldList = (UnsafeList*)lists[i];
                    UnsafeList.RemoveAtBySwapping(oldList, oldIndex);
                }
            }

            return newIndex;
        }

        public void Dispose()
        {
            entities.Dispose();
            for (uint i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                if (types.Contains(type))
                {
                    UnsafeList* list = (UnsafeList*)lists.GetRef(i);
                    UnsafeList.Free(ref list);
                }
            }

            lists.Dispose();
        }
    }
}
