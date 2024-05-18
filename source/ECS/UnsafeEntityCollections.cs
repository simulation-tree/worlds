using Unmanaged;
using Unmanaged.Collections;

namespace Game.ECS
{
    public unsafe struct UnsafeEntityCollections
    {
        private UnmanagedList<nint> collections;
        private UnmanagedList<RuntimeType> types;

        private UnsafeEntityCollections(UnmanagedList<nint> collections, UnmanagedList<RuntimeType> types)
        {
            this.collections = collections;
            this.types = types;
        }

        public static UnsafeEntityCollections* Allocate()
        {
            UnmanagedList<nint> collections = new();
            UnmanagedList<RuntimeType> types = new();
            UnsafeEntityCollections* entityCollections = Allocations.Allocate<UnsafeEntityCollections>();
            entityCollections[0] = new(collections, types);
            return entityCollections;
        }

        public static bool IsDisposed(UnsafeEntityCollections* entityCollections)
        {
            return Allocations.IsNull(entityCollections) || entityCollections->collections.IsDisposed;
        }

        public static void Free(ref UnsafeEntityCollections* entityCollections)
        {
            Allocations.ThrowIfNull(entityCollections);
            for (uint i = 0; i < entityCollections->collections.Count; i++)
            {
                UnsafeList* list = (UnsafeList*)entityCollections->collections[i];
                UnsafeList.Free(ref list);
            }

            entityCollections->collections.Dispose();
            entityCollections->types.Dispose();
            Allocations.Free(ref entityCollections);
        }

        public static UnmanagedList<RuntimeType> GetTypes(UnsafeEntityCollections* entityCollections)
        {
            Allocations.ThrowIfNull(entityCollections);
            return entityCollections->types;
        }

        public static UnsafeList* CreateCollection(UnsafeEntityCollections* entityCollections, RuntimeType type, uint initialCapacity = 1)
        {
            Allocations.ThrowIfNull(entityCollections);
            entityCollections->types.Add(type);

            UnsafeList* list = UnsafeList.Allocate(type, initialCapacity);
            entityCollections->collections.Add((nint)list);
            return list;
        }

        public static UnsafeList* GetCollection(UnsafeEntityCollections* entityCollections, RuntimeType type)
        {
            Allocations.ThrowIfNull(entityCollections);
            uint index = entityCollections->types.IndexOf(type);
            return (UnsafeList*)entityCollections->collections[index];
        }

        public static void RemoveCollection(UnsafeEntityCollections* entityCollections, RuntimeType type)
        {
            Allocations.ThrowIfNull(entityCollections);
            uint index = entityCollections->types.IndexOf(type);
            UnsafeList* list = (UnsafeList*)entityCollections->collections[index];
            UnsafeList.Free(ref list);
            entityCollections->collections.RemoveAtBySwapping(index);
            entityCollections->types.RemoveAtBySwapping(index);
        }
    }
}