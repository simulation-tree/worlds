using Simulation.Unsafe;
using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    public unsafe struct ComponentChunk : IDisposable
    {
        private UnsafeComponentChunk* value;

        public readonly bool IsDisposed => UnsafeComponentChunk.IsDisposed(value);
        public readonly UnmanagedList<EntityID> Entities => UnsafeComponentChunk.GetEntities(value);
        public readonly ReadOnlySpan<RuntimeType> Types => UnsafeComponentChunk.GetTypes(value);
        public readonly uint Key => UnsafeComponentChunk.GetKey(value);

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

        /// <summary>
        /// Checks if the chunk contains all of the given types.
        /// </summary>
        public readonly bool ContainsTypes(ReadOnlySpan<RuntimeType> types, bool exact = false)
        {
            ReadOnlySpan<RuntimeType> myTypes = Types;
            if (types.Length > myTypes.Length)
            {
                return false;
            }

            if (exact)
            {
                if (types.Length != myTypes.Length)
                {
                    return false;
                }
            }

            for (int i = 0; i < types.Length; i++)
            {
                if (!myTypes.Contains(types[i]))
                {
                    return false;
                }
            }

            return true;
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
            return new(GetComponents(RuntimeType.Get<T>()));
        }

        public readonly ref T GetComponentRef<T>(EntityID entity) where T : unmanaged
        {
            uint index = Entities.IndexOf(entity);
            return ref GetComponentRef<T>(index);
        }

        public readonly ref T GetComponentRef<T>(uint index) where T : unmanaged
        {
            UnmanagedList<T> components = GetComponents<T>();
            return ref components.GetRef(index);
        }

        public readonly Span<byte> GetComponentBytes(EntityID entity, RuntimeType type)
        {
            void* component = GetComponent(entity, type);
            return new Span<byte>(component, type.Size);
        }

        public readonly void* GetComponent(EntityID entity, RuntimeType type)
        {
            uint index = Entities.IndexOf(entity);
            return GetComponent(index, type);
        }

        //todo: rename this to become GetComponentAddress instead
        public readonly void* GetComponent(uint index, RuntimeType type)
        {
            UnsafeList* components = GetComponents(type);
            nint address = UnsafeList.GetAddress(components);
            return (void*)(address + (int)index * type.Size);
        }

        public readonly nint GetComponentAddress<T>(uint index) where T : unmanaged
        {
            return (nint)GetComponent(index, RuntimeType.Get<T>());
        }
    }
}
