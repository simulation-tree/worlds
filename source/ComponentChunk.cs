using Collections;
using Collections.Unsafe;
using System;
using Unmanaged;
using Worlds.Unsafe;

namespace Worlds
{
    /// <summary>
    /// Stores components of entities with the same types.
    /// </summary>
    public unsafe struct ComponentChunk : IDisposable
    {
        private UnsafeComponentChunk* value;

        /// <summary>
        /// Checks if this chunk is disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// Returns the entities in this chunk.
        /// </summary>
        public readonly List<uint> Entities => UnsafeComponentChunk.GetEntities(value);

        /// <summary>
        /// Returns the <see cref="BitSet"/> representing the types of components in this chunk.
        /// </summary>
        public readonly BitSet TypesMask => UnsafeComponentChunk.GetTypesMask(value);

#if NET
        /// <summary>
        /// Creates a new component chunk.
        /// </summary>
        public ComponentChunk()
        {
            value = UnsafeComponentChunk.Allocate(default);
        }
#endif

        /// <summary>
        /// Creates a new component chunk with the given <paramref name="componentTypes"/>.
        /// </summary>
        public ComponentChunk(BitSet componentTypes)
        {
            value = UnsafeComponentChunk.Allocate(componentTypes);
        }

        /// <summary>
        /// Creates a new component chunk with the given <paramref name="componentTypes"/>.
        /// </summary>
        public ComponentChunk(USpan<ComponentType> componentTypes)
        {
            BitSet typesMask = new();
            for (byte i = 0; i < componentTypes.Length; i++)
            {
                typesMask.Set(componentTypes[i]);
            }

            value = UnsafeComponentChunk.Allocate(typesMask);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            UnsafeComponentChunk.Free(ref value);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[512];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this component chunk.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
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
                return length;
            }
            else
            {
                buffer[0] = 'E';
                buffer[1] = 'm';
                buffer[2] = 'p';
                buffer[3] = 't';
                buffer[4] = 'y';
                return 5;
            }
        }

        public readonly override int GetHashCode()
        {
            return TypesMask.GetHashCode();
        }

        /// <summary>
        /// Copies the types of components this chunk stores to the given <paramref name="buffer"/>.
        /// </summary>
        public readonly byte CopyTypesTo(USpan<ComponentType> buffer)
        {
            BitSet typeMask = TypesMask;
            byte count = 0;
            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (typeMask.Contains(i))
                {
                    buffer[count++] = ComponentType.All[i];
                }
            }

            return count;
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

        /// <summary>
        /// Retrieves the list of all components of the given <paramref name="type"/>.
        /// </summary>
        public readonly UnsafeList* GetComponents(ComponentType type)
        {
            return UnsafeComponentChunk.GetComponents(value, type);
        }

        /// <summary>
        /// Retrieves the list of all components of the given <typeparamref name="T"/> type.
        /// </summary>
        public readonly List<T> GetComponents<T>() where T : unmanaged
        {
            return new(GetComponents(ComponentType.Get<T>()));
        }

        /// <summary>
        /// Retrieves a specific component of the type <typeparamref name="T"/> at <paramref name="index"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint index) where T : unmanaged
        {
            ComponentType componentType = ComponentType.Get<T>();
            UnsafeList* components = GetComponents(componentType);
            nint address = UnsafeList.GetStartAddress(components);
            return ref *(T*)(address + index * TypeInfo<T>.size);
        }

        /// <summary>
        /// Retrieves the bytes for the specific component of the type <paramref name="type"/> at <paramref name="index"/>.
        /// </summary>
        public readonly USpan<byte> GetComponentBytes(uint index, ComponentType type)
        {
            void* component = GetComponent(index, type);
            return new USpan<byte>(component, type.Size);
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <paramref name="type"/> at <paramref name="index"/>.
        /// </summary>
        public readonly void* GetComponent(uint index, ComponentType type)
        {
            UnsafeList* components = GetComponents(type);
            nint address = UnsafeList.GetStartAddress(components);
            return (void*)(address + index * type.Size);
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <typeparamref name="T"/> at <paramref name="index"/>.
        /// </summary>
        public readonly nint GetComponentAddress<T>(uint index) where T : unmanaged
        {
            ComponentType componentType = ComponentType.Get<T>();
            UnsafeList* components = GetComponents(componentType);
            nint address = UnsafeList.GetStartAddress(components);
            return (nint)(address + index * TypeInfo<T>.size);
        }

        /// <summary>
        /// Assigns the given <paramref name="bytes"/> to the component of the type <paramref name="type"/> at <paramref name="index"/>.
        /// </summary>
        public readonly void SetComponentBytes(uint index, ComponentType type, USpan<byte> bytes)
        {
            void* component = GetComponent(index, type);
            bytes.CopyTo(new USpan<byte>(component, bytes.Length));
        }
    }
}
