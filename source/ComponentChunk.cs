using Game.Unsafe;
using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public unsafe struct ComponentChunk : IDisposable
    {
        private UnsafeComponentChunk* value;

        public readonly bool IsDisposed => UnsafeComponentChunk.IsDisposed(value);
        public readonly UnmanagedList<EntityID> Entities => UnsafeComponentChunk.GetEntities(value);
        public readonly ReadOnlySpan<RuntimeType> Types => UnsafeComponentChunk.GetTypes(value);
        public readonly int Key => UnsafeComponentChunk.GetKey(value);

        public ComponentChunk()
        {
            throw new NotImplementedException();
        }

        public ComponentChunk(ReadOnlySpan<RuntimeType> types)
        {
            value = UnsafeComponentChunk.Allocate(types);
        }

        public void Dispose()
        {
            UnsafeComponentChunk.Free(ref value);
        }

        public readonly void Add(EntityID entity)
        {
            UnsafeComponentChunk.Add(value, entity);
        }

        public readonly void Remove(EntityID entity)
        {
            UnsafeComponentChunk.Remove(value, entity);
        }

        public readonly uint Move(EntityID entity, ComponentChunk destination)
        {
            return UnsafeComponentChunk.Move(value, entity, destination.value);
        }

        public readonly UnsafeList* GetComponents(RuntimeType type)
        {
            return UnsafeComponentChunk.GetComponents(value, type);
        }

        public readonly UnmanagedList<T> GetComponents<T>() where T : unmanaged
        {
            return UnsafeComponentChunk.GetComponents<T>(value);
        }

        public readonly ref T GetComponentRef<T>(EntityID entity) where T : unmanaged
        {
            void* component = UnsafeComponentChunk.GetComponent(value, entity, RuntimeType.Get<T>());
            T* ptr = (T*)component;
            return ref *ptr;
        }

        public readonly ref T GetComponentRef<T>(uint index) where T : unmanaged
        {
            void* component = GetComponent(index, RuntimeType.Get<T>());
            T* ptr = (T*)component;
            return ref *ptr;
        }

        public readonly Span<byte> GetComponentBytes(EntityID entity, RuntimeType type)
        {
            void* component = UnsafeComponentChunk.GetComponent(value, entity, type);
            return new Span<byte>(component, type.size);
        }

        public readonly void* GetComponent(uint index, RuntimeType type)
        {
            return UnsafeComponentChunk.GetComponent(value, index, type);
        }
    }
}
