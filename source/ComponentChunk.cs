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
        public readonly UnmanagedList<uint> Entities => UnsafeComponentChunk.GetEntities(value);
        public readonly USpan<RuntimeType> Types => UnsafeComponentChunk.GetTypes(value);
        public readonly int Key => UnsafeComponentChunk.GetKey(value);

        public ComponentChunk(USpan<RuntimeType> types)
        {
            value = UnsafeComponentChunk.Allocate(types);
        }

        public void Dispose()
        {
            UnsafeComponentChunk.Free(ref value);
        }

        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[512];
            uint bufferCount = 0;
            foreach (RuntimeType type in Types)
            {
                bufferCount += type.ToString(buffer.Slice(bufferCount));
                buffer[bufferCount++] = ',';
                buffer[bufferCount++] = ' ';
            }

            if (bufferCount > 0)
            {
                bufferCount -= 2;
                return new string(buffer.pointer, 0, (int)bufferCount);
            }
            else
            {
                return "Empty";
            }
        }

        /// <summary>
        /// Checks if the chunk contains all of the given component types.
        /// </summary>
        public readonly bool ContainsTypes(USpan<RuntimeType> componentTypes)
        {
            USpan<RuntimeType> myTypes = Types;
            if (componentTypes.length > myTypes.length)
            {
                return false;
            }

            for (uint i = 0; i < componentTypes.length; i++)
            {
                if (!myTypes.Contains(componentTypes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Adds an entity into this chunk and returns its referrable index.
        /// </summary>
        public readonly uint AddEntity(uint entity)
        {
            return UnsafeComponentChunk.Add(value, entity);
        }

        public readonly void RemoveEntity(uint entity)
        {
            UnsafeComponentChunk.Remove(value, entity);
        }

        /// <summary>
        /// Moves the entity and all of its components to another chunk.
        /// </summary>
        public readonly uint MoveEntity(uint entity, ComponentChunk destination)
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

        public readonly ref T GetComponentRef<T>(uint index) where T : unmanaged
        {
            UnmanagedList<T> components = GetComponents<T>();
            return ref components[index];
        }

        public readonly USpan<byte> GetComponentBytes(uint index, RuntimeType type)
        {
            void* component = GetComponentPointer(index, type);
            return new USpan<byte>(component, type.Size);
        }

        public readonly void* GetComponentPointer(uint index, RuntimeType type)
        {
            UnsafeList* components = GetComponents(type);
            nint address = UnsafeList.GetStartAddress(components);
            return (void*)(address + index * type.Size);
        }

        public readonly nint GetComponentAddress<T>(uint index) where T : unmanaged
        {
            return (nint)GetComponentPointer(index, RuntimeType.Get<T>());
        }

        public readonly void SetComponentBytes(uint index, RuntimeType type, USpan<byte> bytes)
        {
            void* component = GetComponentPointer(index, type);
            bytes.CopyTo(new USpan<byte>(component, bytes.length));
        }
    }
}
