using Collections;
using Collections.Unsafe;
using Simulation.Unsafe;
using System;
using Unmanaged;

namespace Simulation
{
    public unsafe struct ComponentChunk : IDisposable
    {
        private UnsafeComponentChunk* value;

        public readonly bool IsDisposed => value is null;
        public readonly List<uint> Entities => UnsafeComponentChunk.GetEntities(value);
        public readonly BitSet TypesMask => UnsafeComponentChunk.GetTypesMask(value);

#if NET
        public ComponentChunk()
        {
            value = UnsafeComponentChunk.Allocate(default);
        }
#endif

        public ComponentChunk(BitSet componentTypes)
        {
            value = UnsafeComponentChunk.Allocate(componentTypes);
        }

        public ComponentChunk(USpan<ComponentType> componentTypes)
        {
            BitSet typesMask = new();
            for (byte i = 0; i < componentTypes.Length; i++)
            {
                typesMask.Set(componentTypes[i]);
            }

            value = UnsafeComponentChunk.Allocate(typesMask);
        }

        public void Dispose()
        {
            UnsafeComponentChunk.Free(ref value);
        }

        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[512];
            uint length = 0;
            BitSet typeMask = TypesMask;
            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (typeMask.Contains(i))
                {
                    ComponentType type = new(i);
                    length += type.ToString(buffer.Slice(length));
                    buffer[length++] = ',';
                    buffer[length++] = ' ';
                }
            }

            if (length > 0)
            {
                length -= 2;
                return buffer.Slice(0, length).ToString();
            }
            else
            {
                return "Empty";
            }
        }

        public readonly byte CopyTypesTo(USpan<ComponentType> buffer)
        {
            BitSet typeMask = TypesMask;
            byte count = 0;
            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (typeMask.Contains(i))
                {
                    buffer[count++] = new(i);
                }
            }

            return count;
        }

        /// <summary>
        /// Checks if this chunk contains all of the given <paramref name="componentTypes"/>.
        /// </summary>
        public readonly bool ContainsAllTypes(BitSet componentTypes)
        {
            return TypesMask.ContainsAll(componentTypes);
        }

        public readonly bool ContainsAllTypes(USpan<ComponentType> componentTypes)
        {
            for (byte i = 0; i < componentTypes.Length; i++)
            {
                if (!ContainsType(componentTypes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if this chunk contains the given <paramref name="componentType"/>
        /// </summary>
        public readonly bool ContainsType(ComponentType componentType)
        {
            return TypesMask.Contains(componentType);
        }

        /// <summary>
        /// Adds the given <paramref name="entity"/> into this chunk and returns its referrable index.
        /// </summary>
        public readonly uint AddEntity(uint entity)
        {
            return UnsafeComponentChunk.Add(value, entity);
        }

        /// <summary>
        /// Removes the given <paramref name="entity"/> from this chunk.
        /// </summary>
        public readonly void RemoveEntity(uint entity)
        {
            UnsafeComponentChunk.Remove(value, entity);
        }

        /// <summary>
        /// Moves the <paramref name="entity"/> and all of its components to the <paramref name="destination"/> chunk.
        /// </summary>
        public readonly uint MoveEntity(uint entity, ComponentChunk destination)
        {
            return UnsafeComponentChunk.Move(value, entity, destination.value);
        }

        public readonly UnsafeList* GetComponents(ComponentType type)
        {
            return UnsafeComponentChunk.GetComponents(value, type);
        }

        public readonly List<T> GetComponents<T>() where T : unmanaged
        {
            return new(GetComponents(ComponentType.Get<T>()));
        }

        public readonly ref T GetComponentRef<T>(uint index) where T : unmanaged
        {
            List<T> components = GetComponents<T>();
            return ref components[index];
        }

        public readonly USpan<byte> GetComponentBytes(uint index, ComponentType type)
        {
            void* component = GetComponentPointer(index, type);
            return new USpan<byte>(component, type.Size);
        }

        public readonly void* GetComponentPointer(uint index, ComponentType type)
        {
            UnsafeList* components = GetComponents(type);
            nint address = UnsafeList.GetStartAddress(components);
            return (void*)(address + index * type.Size);
        }

        public readonly nint GetComponentAddress<T>(uint index) where T : unmanaged
        {
            return (nint)GetComponentPointer(index, ComponentType.Get<T>());
        }

        public readonly void SetComponentBytes(uint index, ComponentType type, USpan<byte> bytes)
        {
            void* component = GetComponentPointer(index, type);
            bytes.CopyTo(new USpan<byte>(component, bytes.Length));
        }
    }
}
