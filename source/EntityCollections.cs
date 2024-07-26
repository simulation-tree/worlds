using Simulation.Unsafe;
using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    public unsafe struct EntityCollections : IDisposable
    {
        private UnsafeEntityCollections* value;

        public readonly bool IsDisposed => UnsafeEntityCollections.IsDisposed(value);
        public readonly ReadOnlySpan<RuntimeType> Types => UnsafeEntityCollections.GetTypes(value).AsSpan();

        private EntityCollections(UnsafeEntityCollections* value)
        {
            this.value = value;
        }

        public void Dispose()
        {
            UnsafeEntityCollections.Free(ref value);
        }

        public readonly UnmanagedList<T> CreateCollection<T>() where T : unmanaged
        {
            UnsafeList* list = CreateCollection(RuntimeType.Get<T>());
            return new(list);
        }

        public readonly UnsafeList* CreateCollection(RuntimeType type, uint initialCapacity = 1)
        {
            return UnsafeEntityCollections.CreateCollection(value, type, initialCapacity);
        }

        public readonly UnmanagedList<T> GetCollection<T>() where T : unmanaged
        {
            UnsafeList* list = UnsafeEntityCollections.GetCollection(value, RuntimeType.Get<T>());
            return new(list);
        }

        public readonly UnsafeList* GetCollection(RuntimeType type)
        {
            return UnsafeEntityCollections.GetCollection(value, type);
        }

        public readonly void RemoveCollection<T>() where T : unmanaged
        {
            RemoveCollection(RuntimeType.Get<T>());
        }

        public readonly void RemoveCollection(RuntimeType type)
        {
            UnsafeEntityCollections.RemoveCollection(value, type);
        }

        public static EntityCollections Create()
        {
            UnsafeEntityCollections* value = UnsafeEntityCollections.Allocate();
            return new(value);
        }
    }
}