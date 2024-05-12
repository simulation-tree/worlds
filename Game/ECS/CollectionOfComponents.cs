using System;
using Unmanaged.Collections;

namespace Game.ECS
{
    internal unsafe sealed class CollectionOfComponents : IDisposable
    {
        public readonly ComponentTypeMask types;
        public readonly UnmanagedList<EntityID> entities;
        public readonly UnsafeList*[] lists;

        public CollectionOfComponents(ComponentTypeMask types)
        {
            this.types = types;
            lists = new UnsafeList*[ComponentTypeMask.MaxValues];
            for (int i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i);
                if (types.Contains(type))
                {
                    lists[i] = UnsafeList.Allocate(type.RuntimeType);
                }
            }

            entities = new();
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
                ComponentType typeToTest = new(i);
                if (destination.types.Contains(typeToTest))
                {
                    ref UnsafeList* newList = ref destination.lists[i];
                    UnsafeList.AddDefault(newList);
                    if (types.Contains(typeToTest))
                    {
                        ref UnsafeList* oldList = ref lists[i];
                        uint newIndex = UnsafeList.GetCount(newList) - 1;
                        UnsafeList.CopyTo(oldList, oldIndex, newList, newIndex);
                        UnsafeList.RemoveAt(oldList, oldIndex);
                    }
                }
                else
                {
                    if (types.Contains(typeToTest))
                    {
                        ref var oldList = ref lists[i];
                        UnsafeList.RemoveAt(oldList, oldIndex);
                    }
                }
            }

            destination.entities.Add(id);
            return (uint)(destination.entities.Count - 1);
        }

        public void Dispose()
        {
            entities.Dispose();
            for (int i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i);
                if (types.Contains(type))
                {
                    UnsafeList.Free(lists[i]);
                }
            }
        }
    }
}
