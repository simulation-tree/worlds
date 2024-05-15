using System;
using Unmanaged.Collections;

namespace Game.ECS
{
    internal unsafe sealed class CollectionOfComponents : IDisposable
    {
        private readonly ComponentTypeMask types;
        private readonly UnmanagedList<EntityID> entities;
        private readonly UnsafeList*[] lists;

        public ReadOnlySpan<EntityID> Entities => entities.AsSpan();

        public CollectionOfComponents(ComponentTypeMask types)
        {
            this.types = types;
            lists = new UnsafeList*[ComponentTypeMask.MaxValues];
            for (int i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                if (types.Contains(type))
                {
                    lists[i] = UnsafeList.Allocate(type.RuntimeType);
                }
            }

            entities = new();
        }

        public void Add(EntityID id)
        {
            entities.Add(id);
            for (int i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                if (types.Contains(type))
                {
                    ref UnsafeList* list = ref lists[i];
                    UnsafeList.AddDefault(list);
                }
            }
        }

        public void Remove(EntityID entity)
        {
            uint index = entities.IndexOf(entity);
            entities.RemoveAt(index);
            for (int i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                if (types.Contains(type))
                {
                    ref UnsafeList* list = ref lists[i];
                    UnsafeList.RemoveAt(list, index);
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
            ref UnsafeList* list = ref lists[type.value - 1];
            return ref UnsafeList.GetRef<T>(list, index);
        }

        public Span<byte> GetComponentBytes(EntityID entity, ComponentType type)
        {
            uint index = entities.IndexOf(entity);
            return GetComponentBytes(index, type);
        }

        public Span<byte> GetComponentBytes(uint index, ComponentType type)
        {
            ref UnsafeList* list = ref lists[type.value - 1];
            return UnsafeList.Get(list, index);
        }

        public UnmanagedList<T> GetComponents<T>() where T : unmanaged
        {
            ComponentType type = ComponentType.Get<T>();
            ref UnsafeList* list = ref lists[type.value - 1];
            return new(list);
        }

        /// <summary>
        /// Moves the entity to another block by removing it, and then adding
        /// to the destination.
        /// </summary>
        public uint MoveTo(EntityID id, CollectionOfComponents destination)
        {
            uint oldIndex = entities.IndexOf(id);
            entities.RemoveAt(oldIndex);
            for (int i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                if (destination.types.Contains(type))
                {
                    ref UnsafeList* newList = ref destination.lists[i];
                    UnsafeList.AddDefault(newList);
                    if (types.Contains(type))
                    {
                        ref UnsafeList* oldList = ref lists[i];
                        uint newIndex = UnsafeList.GetCount(newList) - 1;
                        UnsafeList.CopyTo(oldList, oldIndex, newList, newIndex);
                        UnsafeList.RemoveAt(oldList, oldIndex);
                    }
                }
                else
                {
                    if (types.Contains(type))
                    {
                        ref var oldList = ref lists[i];
                        UnsafeList.RemoveAt(oldList, oldIndex);
                    }
                }
            }

            destination.entities.Add(id);
            return destination.entities.Count - 1;
        }

        public void Dispose()
        {
            entities.Dispose();
            for (int i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i + 1);
                if (types.Contains(type))
                {
                    UnsafeList.Free(lists[i]);
                }
            }
        }
    }
}
