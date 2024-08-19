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
        public readonly UnmanagedList<eint> Entities => UnsafeComponentChunk.GetEntities(value);
        public readonly ReadOnlySpan<RuntimeType> Types => UnsafeComponentChunk.GetTypes(value);
        public readonly uint Key => UnsafeComponentChunk.GetKey(value);

        public ComponentChunk(ReadOnlySpan<RuntimeType> types)
        {
            value = UnsafeComponentChunk.Allocate(types);
        }

        public void Dispose()
        {
            UnsafeComponentChunk.Free(ref value);
        }

        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[512];
            int bufferCount = 0;
            foreach (RuntimeType type in Types)
            {
                bufferCount += type.ToString(buffer[bufferCount..]);
                buffer[bufferCount++] = ',';
                buffer[bufferCount++] = ' ';
            }

            if (bufferCount > 0)
            {
                bufferCount -= 2;
                return new string(buffer[..bufferCount]);
            }
            else
            {
                return "Empty";
            }
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

        public readonly void Add(eint entity)
        {
            UnsafeComponentChunk.Add(value, entity);
        }

        public readonly void Remove(eint entity)
        {
            UnsafeComponentChunk.Remove(value, entity);
        }

        public readonly uint Move(eint entity, ComponentChunk destination)
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

        public readonly ref T GetComponentRef<T>(eint entity) where T : unmanaged
        {
            uint index = Entities.IndexOf(entity);
            return ref GetComponentRef<T>(index);
        }

        public readonly ref T GetComponentRef<T>(uint index) where T : unmanaged
        {
            UnmanagedList<T> components = GetComponents<T>();
            return ref components.GetRef(index);
        }

        public readonly Span<byte> GetComponentBytes(eint entity, RuntimeType type)
        {
            void* component = GetComponentPointer(entity, type);
            return new Span<byte>(component, type.Size);
        }

        public readonly Span<byte> GetComponentBytes(uint index, RuntimeType type)
        {
            void* component = GetComponentPointer(index, type);
            return new Span<byte>(component, type.Size);
        }

        public readonly void* GetComponentPointer(eint entity, RuntimeType type)
        {
            uint index = Entities.IndexOf(entity);
            return GetComponentPointer(index, type);
        }

        public readonly void* GetComponentPointer(uint index, RuntimeType type)
        {
            UnsafeList* components = GetComponents(type);
            nint address = UnsafeList.GetAddress(components);
            return (void*)(address + (int)index * type.Size);
        }

        public readonly nint GetComponentAddress<T>(uint index) where T : unmanaged
        {
            return (nint)GetComponentPointer(index, RuntimeType.Get<T>());
        }

        public readonly nint GetComponentAddress<T>(eint entity) where T : unmanaged
        {
            uint index = Entities.IndexOf(entity);
            return GetComponentAddress<T>(index);
        }

        public readonly void SetComponentBytes(eint entity, RuntimeType type, ReadOnlySpan<byte> bytes)
        {
            void* component = GetComponentPointer(entity, type);
            bytes.CopyTo(new Span<byte>(component, bytes.Length));
        }
    }
}
