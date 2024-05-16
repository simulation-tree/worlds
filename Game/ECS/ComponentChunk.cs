using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Game.ECS
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
            return ref UnsafeComponentChunk.GetComponentRef<T>(value, entity);
        }

        public readonly ref T GetComponentRef<T>(uint index) where T : unmanaged
        {
            return ref UnsafeComponentChunk.GetComponentRef<T>(value, index);
        }

        public readonly Span<byte> GetComponentBytes(EntityID entity, RuntimeType type)
        {
            return UnsafeComponentChunk.GetComponentBytes(value, entity, type);
        }
    }
}
