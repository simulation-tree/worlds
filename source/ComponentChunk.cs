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
    public unsafe struct ComponentChunk : IDisposable, IEquatable<ComponentChunk>
    {
        private UnsafeComponentChunk* value;

        /// <summary>
        /// Checks if this chunk is disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// Native address of this chunk.
        /// </summary>
        public readonly nint Address => (nint)value;

        /// <summary>
        /// Returns the entities in this chunk.
        /// </summary>
        public readonly List<uint> Entities => UnsafeComponentChunk.GetEntities(value);

        /// <summary>
        /// Amount of entities stored in this chunk.
        /// </summary>
        public readonly uint Count => UnsafeComponentChunk.GetCount(value);

        /// <summary>
        /// Returns the <see cref="BitSet"/> representing the types of components in this chunk.
        /// </summary>
        public readonly BitSet TypesMask => UnsafeComponentChunk.GetTypesMask(value);

        /// <summary>
        /// The schema that this chunk was created with.
        /// </summary>
        public readonly Schema Schema => UnsafeComponentChunk.GetSchema(value);

#if NET
        [Obsolete("Default constructor not supported", true)]
        public ComponentChunk()
        {
            throw new NotSupportedException();
        }
#endif
        /// <summary>
        /// Creates a new component chunk.
        /// </summary>
        public ComponentChunk(Schema schema)
        {
            value = UnsafeComponentChunk.Allocate(default, schema);
        }

        /// <summary>
        /// Creates a new component chunk with the given <paramref name="componentTypes"/>.
        /// </summary>
        public ComponentChunk(BitSet componentTypes, Schema schema)
        {
            value = UnsafeComponentChunk.Allocate(componentTypes, schema);
        }

        /// <summary>
        /// Creates a new component chunk with the given <paramref name="componentTypes"/>.
        /// </summary>
        public ComponentChunk(USpan<ComponentType> componentTypes, Schema schema)
        {
            BitSet typesMask = new();
            for (byte i = 0; i < componentTypes.Length; i++)
            {
                typesMask |= componentTypes[i];
            }

            value = UnsafeComponentChunk.Allocate(typesMask, schema);
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
                if (typeMask == i)
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
            for (byte c = 0; c < BitSet.Capacity; c++)
            {
                if (typeMask == c)
                {
                    buffer[count++] = new(c);
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

        public readonly Entity<C1> GetEntity<C1>(uint index) where C1 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1>(value, index);
        }

        public readonly Entity<C1, C2> GetEntity<C1, C2>(uint index) where C1 : unmanaged where C2 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2>(value, index);
        }

        public readonly Entity<C1, C2, C3> GetEntity<C1, C2, C3>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4> GetEntity<C1, C2, C3, C4>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5> GetEntity<C1, C2, C3, C4, C5>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6> GetEntity<C1, C2, C3, C4, C5, C6>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7> GetEntity<C1, C2, C3, C4, C5, C6, C7>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            return UnsafeComponentChunk.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(value, index);
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
            Schema schema = Schema;
            return new(GetComponents(schema.GetComponent<T>()));
        }

        /// <summary>
        /// Retrieves a specific component of the type <typeparamref name="T"/> at <paramref name="index"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint index) where T : unmanaged
        {
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            UnsafeList* components = GetComponents(componentType);
            nint address = UnsafeList.GetStartAddress(components);
            return ref *(T*)(address + index * TypeInfo<T>.size);
        }

        /// <summary>
        /// Retrieves the bytes for the specific component of the type <paramref name="type"/> at <paramref name="index"/>.
        /// </summary>
        public readonly USpan<byte> GetComponentBytes(uint index, ComponentType type)
        {
            Schema schema = Schema;
            UnsafeList* components = GetComponents(type);
            nint address = UnsafeList.GetStartAddress(components);
            ushort componentSize = schema.GetComponentSize(type);
            void* component = (void*)(address + index * componentSize);
            return new(component, componentSize);
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <paramref name="type"/> at <paramref name="index"/>.
        /// </summary>
        public readonly void* GetComponent(uint index, ComponentType type)
        {
            Schema schema = Schema;
            UnsafeList* components = GetComponents(type);
            nint address = UnsafeList.GetStartAddress(components);
            ushort componentSize = schema.GetComponentSize(type);
            return (void*)(address + index * componentSize);
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <typeparamref name="T"/> at <paramref name="index"/>.
        /// </summary>
        public readonly nint GetComponentAddress<T>(uint index) where T : unmanaged
        {
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
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

        public readonly override bool Equals(object? obj)
        {
            return obj is ComponentChunk chunk && Equals(chunk);
        }

        public readonly bool Equals(ComponentChunk other)
        {
            return (nint)value == (nint)other.value;
        }

        public static bool operator ==(ComponentChunk left, ComponentChunk right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentChunk left, ComponentChunk right)
        {
            return !(left == right);
        }

        public readonly ref struct Entity
        {
            public readonly uint entity;

            public Entity(uint entity)
            {
                this.entity = entity;
            }
        }

        public readonly ref struct Entity<C1> where C1 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;

            public Entity(uint entity, ref C1 component1)
            {
                this.entity = entity;
                this.component1 = ref component1;
            }
        }

        public readonly ref struct Entity<C1, C2> where C1 : unmanaged where C2 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;

            public Entity(uint entity, ref C1 component1, ref C2 component2)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
            }
        }

        public readonly ref struct Entity<C1, C2, C3> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
            public readonly ref C8 component8;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
                this.component8 = ref component8;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
            public readonly ref C8 component8;
            public readonly ref C9 component9;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
                this.component8 = ref component8;
                this.component9 = ref component9;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
            public readonly ref C8 component8;
            public readonly ref C9 component9;
            public readonly ref C10 component10;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
                this.component8 = ref component8;
                this.component9 = ref component9;
                this.component10 = ref component10;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
            public readonly ref C8 component8;
            public readonly ref C9 component9;
            public readonly ref C10 component10;
            public readonly ref C11 component11;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
                this.component8 = ref component8;
                this.component9 = ref component9;
                this.component10 = ref component10;
                this.component11 = ref component11;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
            public readonly ref C8 component8;
            public readonly ref C9 component9;
            public readonly ref C10 component10;
            public readonly ref C11 component11;
            public readonly ref C12 component12;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11, ref C12 component12)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
                this.component8 = ref component8;
                this.component9 = ref component9;
                this.component10 = ref component10;
                this.component11 = ref component11;
                this.component12 = ref component12;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
            public readonly ref C8 component8;
            public readonly ref C9 component9;
            public readonly ref C10 component10;
            public readonly ref C11 component11;
            public readonly ref C12 component12;
            public readonly ref C13 component13;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11, ref C12 component12, ref C13 component13)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
                this.component8 = ref component8;
                this.component9 = ref component9;
                this.component10 = ref component10;
                this.component11 = ref component11;
                this.component12 = ref component12;
                this.component13 = ref component13;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
            public readonly ref C8 component8;
            public readonly ref C9 component9;
            public readonly ref C10 component10;
            public readonly ref C11 component11;
            public readonly ref C12 component12;
            public readonly ref C13 component13;
            public readonly ref C14 component14;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11, ref C12 component12, ref C13 component13, ref C14 component14)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
                this.component8 = ref component8;
                this.component9 = ref component9;
                this.component10 = ref component10;
                this.component11 = ref component11;
                this.component12 = ref component12;
                this.component13 = ref component13;
                this.component14 = ref component14;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
            public readonly ref C8 component8;
            public readonly ref C9 component9;
            public readonly ref C10 component10;
            public readonly ref C11 component11;
            public readonly ref C12 component12;
            public readonly ref C13 component13;
            public readonly ref C14 component14;
            public readonly ref C15 component15;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11, ref C12 component12, ref C13 component13, ref C14 component14, ref C15 component15)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
                this.component8 = ref component8;
                this.component9 = ref component9;
                this.component10 = ref component10;
                this.component11 = ref component11;
                this.component12 = ref component12;
                this.component13 = ref component13;
                this.component14 = ref component14;
                this.component15 = ref component15;
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            public readonly uint entity;
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
            public readonly ref C8 component8;
            public readonly ref C9 component9;
            public readonly ref C10 component10;
            public readonly ref C11 component11;
            public readonly ref C12 component12;
            public readonly ref C13 component13;
            public readonly ref C14 component14;
            public readonly ref C15 component15;
            public readonly ref C16 component16;

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11, ref C12 component12, ref C13 component13, ref C14 component14, ref C15 component15, ref C16 component16)
            {
                this.entity = entity;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
                this.component8 = ref component8;
                this.component9 = ref component9;
                this.component10 = ref component10;
                this.component11 = ref component11;
                this.component12 = ref component12;
                this.component13 = ref component13;
                this.component14 = ref component14;
                this.component15 = ref component15;
                this.component16 = ref component16;
            }
        }
    }
}
