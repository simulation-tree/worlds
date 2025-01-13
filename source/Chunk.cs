﻿using Collections;
using Collections.Implementations;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Stores components of entities with the same component, array and tags.
    /// </summary>
    public unsafe struct Chunk : IDisposable, IEquatable<Chunk>
    {
        private Implementation* value;

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
        public readonly USpan<uint> Entities => value->entities.AsSpan();

        /// <summary>
        /// Amount of entities stored in this chunk.
        /// </summary>
        public readonly uint Count => value->entities.Count;

        /// <summary>
        /// Returns the definition representing the types of components, arrays and tags
        /// this chunk is for.
        /// </summary>
        public readonly Definition Definition => value->definition;

        /// <summary>
        /// The schema that this chunk was created with.
        /// </summary>
        public readonly Schema Schema => value->schema;

        public readonly uint this[uint index] => Entities[index];

#if NET
        [Obsolete("Default constructor not supported", true)]
        public Chunk()
        {
            throw new NotSupportedException();
        }
#endif
        /// <summary>
        /// Creates a new chunk.
        /// </summary>
        public Chunk(Schema schema)
        {
            value = Implementation.Allocate(default, schema);
        }

        /// <summary>
        /// Creates a new chunk with the given <paramref name="definition"/>.
        /// </summary>
        public Chunk(Definition definition, Schema schema)
        {
            value = Implementation.Allocate(definition, schema);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Implementation.Free(ref value);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[512];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this chunk.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            uint length = 0;
            BitSet componentTypes = Definition.ComponentTypes;
            for (byte i = 0; i < BitSet.Capacity; i++)
            {
                if (componentTypes.Contains(i))
                {
                    ComponentType componentType = new(i);
                    length += componentType.ToString(Schema, buffer.Slice(length));
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
            return Definition.GetHashCode();
        }

        /// <summary>
        /// Adds the given <paramref name="entity"/> into this chunk and returns its referable index.
        /// </summary>
        public readonly uint AddEntity(uint entity)
        {
            return Implementation.Add(value, entity);
        }

        /// <summary>
        /// Removes the given <paramref name="entity"/> from this chunk.
        /// </summary>
        public readonly void RemoveEntity(uint entity)
        {
            Implementation.Remove(value, entity);
        }

        /// <summary>
        /// Moves the <paramref name="entity"/> and all of its components to the <paramref name="destination"/> chunk.
        /// </summary>
        public readonly uint MoveEntity(uint entity, Chunk destination)
        {
            return Implementation.Move(value, entity, destination.value);
        }

        public readonly Entity<C1> GetEntity<C1>(uint index) where C1 : unmanaged
        {
            return Implementation.GetEntity<C1>(value, index);
        }

        public readonly Entity<C1> GetEntity<C1>(uint index, ComponentType c1) where C1 : unmanaged
        {
            return Implementation.GetEntity<C1>(value, index, c1);
        }

        public readonly Entity<C1, C2> GetEntity<C1, C2>(uint index) where C1 : unmanaged where C2 : unmanaged
        {
            return Implementation.GetEntity<C1, C2>(value, index);
        }

        public readonly Entity<C1, C2> GetEntity<C1, C2>(uint index, ComponentType c1, ComponentType c2) where C1 : unmanaged where C2 : unmanaged
        {
            return Implementation.GetEntity<C1, C2>(value, index, c1, c2);
        }

        public readonly Entity<C1, C2, C3> GetEntity<C1, C2, C3>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3>(value, index);
        }

        public readonly Entity<C1, C2, C3> GetEntity<C1, C2, C3>(uint index, ComponentType c1, ComponentType c2, ComponentType c3) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3>(value, index, c1, c2, c3);
        }

        public readonly Entity<C1, C2, C3, C4> GetEntity<C1, C2, C3, C4>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4> GetEntity<C1, C2, C3, C4>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4>(value, index, c1, c2, c3, c4);
        }

        public readonly Entity<C1, C2, C3, C4, C5> GetEntity<C1, C2, C3, C4, C5>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5> GetEntity<C1, C2, C3, C4, C5>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5>(value, index, c1, c2, c3, c4, c5);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6> GetEntity<C1, C2, C3, C4, C5, C6>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6> GetEntity<C1, C2, C3, C4, C5, C6>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6>(value, index, c1, c2, c3, c4, c5, c6);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7> GetEntity<C1, C2, C3, C4, C5, C6, C7>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7> GetEntity<C1, C2, C3, C4, C5, C6, C7>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7>(value, index, c1, c2, c3, c4, c5, c6, c7);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(value, index, c1, c2, c3, c4, c5, c6, c7, c8);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(value, index, c1, c2, c3, c4, c5, c6, c7, c8, c9);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(value, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(value, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(value, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(value, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13, ComponentType c14) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(value, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13, ComponentType c14, ComponentType c15) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(value, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(value, index);
        }

        public readonly Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13, ComponentType c14, ComponentType c15, ComponentType c16) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            return Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(value, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16);
        }

        /// <summary>
        /// Retrieves the list of all components of the given <paramref name="componentType"/>.
        /// </summary>
        public readonly List* GetComponents(ComponentType componentType)
        {
            return Implementation.GetComponents(value, componentType);
        }

        /// <summary>
        /// Retrieves all components of the given <typeparamref name="T"/> type.
        /// </summary>
        public readonly USpan<T> GetComponents<T>() where T : unmanaged
        {
            Schema schema = Schema;
            List<T> list = new(GetComponents(schema.GetComponent<T>()));
            return list.AsSpan();
        }

        public readonly USpan<T> GetComponents<T>(ComponentType componentType) where T : unmanaged
        {
            List<T> list = new(GetComponents(componentType));
            return list.AsSpan();
        }

        /// <summary>
        /// Retrieves a specific component of the type <typeparamref name="T"/> at <paramref name="index"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint index) where T : unmanaged
        {
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            List* components = GetComponents(componentType);
            nint address = List.GetStartAddress(components);
            return ref *(T*)(address + index * sizeof(T));
        }

        public readonly ref T GetComponent<T>(uint index, ComponentType componentType) where T : unmanaged
        {
            List* components = GetComponents(componentType);
            nint address = List.GetStartAddress(components);
            return ref *(T*)(address + index * sizeof(T));
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <paramref name="componentType"/> at <paramref name="index"/>.
        /// </summary>
        public readonly Allocation GetComponent(uint index, ComponentType componentType, ushort componentSize)
        {
            List* components = GetComponents(componentType);
            nint address = List.GetStartAddress(components);
            return new(address + index * componentSize);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Chunk chunk && Equals(chunk);
        }

        public readonly bool Equals(Chunk other)
        {
            return (nint)value == (nint)other.value;
        }

        public static bool operator ==(Chunk left, Chunk right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Chunk left, Chunk right)
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

        public unsafe struct Implementation
        {
            public List<uint> entities;
            public Array<nint> componentArrays;
            public readonly Array<byte> typeIndices;
            public readonly Definition definition;
            public readonly Schema schema;

            private Implementation(Definition definition, Array<nint> componentArrays, Array<byte> typeIndices, Schema schema)
            {
                entities = new(4);
                this.typeIndices = typeIndices;
                this.componentArrays = componentArrays;
                this.definition = definition;
                this.schema = schema;
            }

            /// <summary>
            /// Allocates a new <see cref="Implementation"/> with the given <paramref name="definition"/>.
            /// </summary>
            public static Implementation* Allocate(Definition definition, Schema schema)
            {
                Array<nint> componentArrays = new(BitSet.Capacity);
                USpan<byte> typeIndices = stackalloc byte[BitSet.Capacity];
                byte typeCount = 0;
                for (byte c = 0; c < BitSet.Capacity; c++)
                {
                    if (definition.ComponentTypes.Contains(c))
                    {
                        ComponentType componentType = new(c);
                        ushort componentSize = schema.GetSize(componentType);
                        componentArrays[c] = (nint)List.Allocate(4, componentSize);
                        typeIndices[typeCount++] = c;
                    }
                }

                Implementation* chunk = Allocations.Allocate<Implementation>();
                chunk[0] = new(definition, componentArrays, new(typeIndices.Slice(0, typeCount)), schema);
                return chunk;
            }

            /// <summary>
            /// Frees the given <paramref name="chunk"/>.
            /// </summary>
            public static void Free(ref Implementation* chunk)
            {
                Allocations.ThrowIfNull(chunk);

                chunk->entities.Dispose();
                uint typeCount = chunk->typeIndices.Length;
                for (byte i = 0; i < typeCount; i++)
                {
                    ComponentType componentType = new(chunk->typeIndices[i]);
                    List* components = (List*)chunk->componentArrays[componentType];
                    List.Free(ref components);
                }

                chunk->componentArrays.Dispose();
                chunk->typeIndices.Dispose();
                Allocations.Free(ref chunk);
            }

            /// <summary>
            /// Adds a new entity to the chunk.
            /// </summary>
            public static uint Add(Implementation* chunk, uint entity)
            {
                Allocations.ThrowIfNull(chunk);

                chunk->entities.Add(entity);
                uint typeCount = chunk->typeIndices.Length;
                for (byte i = 0; i < typeCount; i++)
                {
                    ComponentType componentType = new(chunk->typeIndices[i]);
                    List* list = (List*)chunk->componentArrays[componentType];
                    List.AddDefault(list);
                }

                return chunk->entities.Count - 1;
            }

            /// <summary>
            /// Removes the <paramref name="entity"/> from the chunk.
            /// </summary>
            public static void Remove(Implementation* chunk, uint entity)
            {
                Allocations.ThrowIfNull(chunk);

                uint index = chunk->entities.IndexOf(entity);
                chunk->entities.RemoveAtBySwapping(index);
                uint typeCount = chunk->typeIndices.Length;
                for (byte i = 0; i < typeCount; i++)
                {
                    ComponentType componentType = new(chunk->typeIndices[i]);
                    List* list = (List*)chunk->componentArrays[componentType];
                    List.RemoveAtBySwapping(list, index);
                }
            }

            /// <summary>
            /// Moves the <paramref name="entity"/> and all of its components to the <paramref name="destination"/> chunk.
            /// </summary>
            /// <returns>New local index in the <paramref name="destination"/> chunk.</returns>
            public static uint Move(Implementation* source, uint entity, Implementation* destination)
            {
                Allocations.ThrowIfNull(source);
                Allocations.ThrowIfNull(destination);

                uint oldIndex = source->entities.IndexOf(entity);
                source->entities.RemoveAtBySwapping(oldIndex);
                uint newIndex = destination->entities.Count;
                destination->entities.Add(entity);

                //copy from source to destination
                for (uint i = 0; i < destination->typeIndices.Length; i++)
                {
                    ComponentType destinationComponentType = new(destination->typeIndices[i]);
                    List* destinationList = (List*)destination->componentArrays[destinationComponentType];
                    if (source->typeIndices.Contains(destinationComponentType.index))
                    {
                        List* sourceList = (List*)source->componentArrays[destinationComponentType];
                        List.Insert(destinationList, newIndex, List.GetElementBytes(sourceList, oldIndex));
                    }
                    else
                    {
                        List.AddDefault(destinationList);
                    }
                }

                //remove from source
                for (uint i = 0; i < source->typeIndices.Length; i++)
                {
                    ComponentType sourceComponentType = new(source->typeIndices[i]);
                    List* sourceList = (List*)source->componentArrays[sourceComponentType];
                    List.RemoveAtBySwapping(sourceList, oldIndex);
                }

                return newIndex;
            }

            /// <summary>
            /// Retrieves a list of components of the given <paramref name="componentType"/>.
            /// </summary>
            public static List* GetComponents(Implementation* chunk, ComponentType componentType)
            {
                Allocations.ThrowIfNull(chunk);
                ThrowIfComponentTypeIsMissing(chunk, componentType);

                return (List*)chunk->componentArrays[componentType];
            }

            public static Entity<C1> GetEntity<C1>(Implementation* chunk, uint index) where C1 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                ComponentType c1 = chunk->schema.GetComponent<C1>();
                return GetEntity<C1>(chunk, index, c1);
            }

            public static Entity<C1> GetEntity<C1>(Implementation* chunk, uint index, ComponentType c1) where C1 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                return new Entity<C1>(chunk->entities[index], ref v1);
            }

            public static Entity<C1, C2> GetEntity<C1, C2>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                return GetEntity<C1, C2>(chunk, index, c1, c2);
            }

            public static Entity<C1, C2> GetEntity<C1, C2>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2) where C1 : unmanaged where C2 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                return new Entity<C1, C2>(chunk->entities[index], ref v1, ref v2);
            }

            public static Entity<C1, C2, C3> GetEntity<C1, C2, C3>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                return GetEntity<C1, C2, C3>(chunk, index, c1, c2, c3);
            }

            public static Entity<C1, C2, C3> GetEntity<C1, C2, C3>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                return new Entity<C1, C2, C3>(chunk->entities[index], ref v1, ref v2, ref v3);
            }

            public static Entity<C1, C2, C3, C4> GetEntity<C1, C2, C3, C4>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                return GetEntity<C1, C2, C3, C4>(chunk, index, c1, c2, c3, c4);
            }

            public static Entity<C1, C2, C3, C4> GetEntity<C1, C2, C3, C4>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                return new Entity<C1, C2, C3, C4>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4);
            }

            public static Entity<C1, C2, C3, C4, C5> GetEntity<C1, C2, C3, C4, C5>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                return GetEntity<C1, C2, C3, C4, C5>(chunk, index, c1, c2, c3, c4, c5);
            }

            public static Entity<C1, C2, C3, C4, C5> GetEntity<C1, C2, C3, C4, C5>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                return new Entity<C1, C2, C3, C4, C5>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5);
            }

            public static Entity<C1, C2, C3, C4, C5, C6> GetEntity<C1, C2, C3, C4, C5, C6>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                ComponentType c6 = schema.GetComponent<C6>();
                return GetEntity<C1, C2, C3, C4, C5, C6>(chunk, index, c1, c2, c3, c4, c5, c6);
            }

            public static Entity<C1, C2, C3, C4, C5, C6> GetEntity<C1, C2, C3, C4, C5, C6>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                nint list6Address = List.GetStartAddress(GetComponents(chunk, c6));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                ref C6 v6 = ref *(C6*)(list6Address + index * sizeof(C6));
                return new Entity<C1, C2, C3, C4, C5, C6>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7> GetEntity<C1, C2, C3, C4, C5, C6, C7>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                ComponentType c6 = schema.GetComponent<C6>();
                ComponentType c7 = schema.GetComponent<C7>();
                return GetEntity<C1, C2, C3, C4, C5, C6, C7>(chunk, index, c1, c2, c3, c4, c5, c6, c7);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7> GetEntity<C1, C2, C3, C4, C5, C6, C7>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                nint list6Address = List.GetStartAddress(GetComponents(chunk, c6));
                nint list7Address = List.GetStartAddress(GetComponents(chunk, c7));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                ref C6 v6 = ref *(C6*)(list6Address + index * sizeof(C6));
                ref C7 v7 = ref *(C7*)(list7Address + index * sizeof(C7));
                return new Entity<C1, C2, C3, C4, C5, C6, C7>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                ComponentType c6 = schema.GetComponent<C6>();
                ComponentType c7 = schema.GetComponent<C7>();
                ComponentType c8 = schema.GetComponent<C8>();
                return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                nint list6Address = List.GetStartAddress(GetComponents(chunk, c6));
                nint list7Address = List.GetStartAddress(GetComponents(chunk, c7));
                nint list8Address = List.GetStartAddress(GetComponents(chunk, c8));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                ref C6 v6 = ref *(C6*)(list6Address + index * sizeof(C6));
                ref C7 v7 = ref *(C7*)(list7Address + index * sizeof(C7));
                ref C8 v8 = ref *(C8*)(list8Address + index * sizeof(C8));
                return new Entity<C1, C2, C3, C4, C5, C6, C7, C8>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                ComponentType c6 = schema.GetComponent<C6>();
                ComponentType c7 = schema.GetComponent<C7>();
                ComponentType c8 = schema.GetComponent<C8>();
                ComponentType c9 = schema.GetComponent<C9>();
                return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                nint list6Address = List.GetStartAddress(GetComponents(chunk, c6));
                nint list7Address = List.GetStartAddress(GetComponents(chunk, c7));
                nint list8Address = List.GetStartAddress(GetComponents(chunk, c8));
                nint list9Address = List.GetStartAddress(GetComponents(chunk, c9));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                ref C6 v6 = ref *(C6*)(list6Address + index * sizeof(C6));
                ref C7 v7 = ref *(C7*)(list7Address + index * sizeof(C7));
                ref C8 v8 = ref *(C8*)(list8Address + index * sizeof(C8));
                ref C9 v9 = ref *(C9*)(list9Address + index * sizeof(C9));
                return new Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                ComponentType c6 = schema.GetComponent<C6>();
                ComponentType c7 = schema.GetComponent<C7>();
                ComponentType c8 = schema.GetComponent<C8>();
                ComponentType c9 = schema.GetComponent<C9>();
                ComponentType c10 = schema.GetComponent<C10>();
                return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                nint list6Address = List.GetStartAddress(GetComponents(chunk, c6));
                nint list7Address = List.GetStartAddress(GetComponents(chunk, c7));
                nint list8Address = List.GetStartAddress(GetComponents(chunk, c8));
                nint list9Address = List.GetStartAddress(GetComponents(chunk, c9));
                nint list10Address = List.GetStartAddress(GetComponents(chunk, c10));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                ref C6 v6 = ref *(C6*)(list6Address + index * sizeof(C6));
                ref C7 v7 = ref *(C7*)(list7Address + index * sizeof(C7));
                ref C8 v8 = ref *(C8*)(list8Address + index * sizeof(C8));
                ref C9 v9 = ref *(C9*)(list9Address + index * sizeof(C9));
                ref C10 v10 = ref *(C10*)(list10Address + index * sizeof(C10));
                return new Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                ComponentType c6 = schema.GetComponent<C6>();
                ComponentType c7 = schema.GetComponent<C7>();
                ComponentType c8 = schema.GetComponent<C8>();
                ComponentType c9 = schema.GetComponent<C9>();
                ComponentType c10 = schema.GetComponent<C10>();
                ComponentType c11 = schema.GetComponent<C11>();
                return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                nint list6Address = List.GetStartAddress(GetComponents(chunk, c6));
                nint list7Address = List.GetStartAddress(GetComponents(chunk, c7));
                nint list8Address = List.GetStartAddress(GetComponents(chunk, c8));
                nint list9Address = List.GetStartAddress(GetComponents(chunk, c9));
                nint list10Address = List.GetStartAddress(GetComponents(chunk, c10));
                nint list11Address = List.GetStartAddress(GetComponents(chunk, c11));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                ref C6 v6 = ref *(C6*)(list6Address + index * sizeof(C6));
                ref C7 v7 = ref *(C7*)(list7Address + index * sizeof(C7));
                ref C8 v8 = ref *(C8*)(list8Address + index * sizeof(C8));
                ref C9 v9 = ref *(C9*)(list9Address + index * sizeof(C9));
                ref C10 v10 = ref *(C10*)(list10Address + index * sizeof(C10));
                ref C11 v11 = ref *(C11*)(list11Address + index * sizeof(C11));
                return new Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                ComponentType c6 = schema.GetComponent<C6>();
                ComponentType c7 = schema.GetComponent<C7>();
                ComponentType c8 = schema.GetComponent<C8>();
                ComponentType c9 = schema.GetComponent<C9>();
                ComponentType c10 = schema.GetComponent<C10>();
                ComponentType c11 = schema.GetComponent<C11>();
                ComponentType c12 = schema.GetComponent<C12>();
                return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                nint list6Address = List.GetStartAddress(GetComponents(chunk, c6));
                nint list7Address = List.GetStartAddress(GetComponents(chunk, c7));
                nint list8Address = List.GetStartAddress(GetComponents(chunk, c8));
                nint list9Address = List.GetStartAddress(GetComponents(chunk, c9));
                nint list10Address = List.GetStartAddress(GetComponents(chunk, c10));
                nint list11Address = List.GetStartAddress(GetComponents(chunk, c11));
                nint list12Address = List.GetStartAddress(GetComponents(chunk, c12));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                ref C6 v6 = ref *(C6*)(list6Address + index * sizeof(C6));
                ref C7 v7 = ref *(C7*)(list7Address + index * sizeof(C7));
                ref C8 v8 = ref *(C8*)(list8Address + index * sizeof(C8));
                ref C9 v9 = ref *(C9*)(list9Address + index * sizeof(C9));
                ref C10 v10 = ref *(C10*)(list10Address + index * sizeof(C10));
                ref C11 v11 = ref *(C11*)(list11Address + index * sizeof(C11));
                ref C12 v12 = ref *(C12*)(list12Address + index * sizeof(C12));
                return new Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11, ref v12);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                ComponentType c6 = schema.GetComponent<C6>();
                ComponentType c7 = schema.GetComponent<C7>();
                ComponentType c8 = schema.GetComponent<C8>();
                ComponentType c9 = schema.GetComponent<C9>();
                ComponentType c10 = schema.GetComponent<C10>();
                ComponentType c11 = schema.GetComponent<C11>();
                ComponentType c12 = schema.GetComponent<C12>();
                ComponentType c13 = schema.GetComponent<C13>();
                return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                nint list6Address = List.GetStartAddress(GetComponents(chunk, c6));
                nint list7Address = List.GetStartAddress(GetComponents(chunk, c7));
                nint list8Address = List.GetStartAddress(GetComponents(chunk, c8));
                nint list9Address = List.GetStartAddress(GetComponents(chunk, c9));
                nint list10Address = List.GetStartAddress(GetComponents(chunk, c10));
                nint list11Address = List.GetStartAddress(GetComponents(chunk, c11));
                nint list12Address = List.GetStartAddress(GetComponents(chunk, c12));
                nint list13Address = List.GetStartAddress(GetComponents(chunk, c13));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                ref C6 v6 = ref *(C6*)(list6Address + index * sizeof(C6));
                ref C7 v7 = ref *(C7*)(list7Address + index * sizeof(C7));
                ref C8 v8 = ref *(C8*)(list8Address + index * sizeof(C8));
                ref C9 v9 = ref *(C9*)(list9Address + index * sizeof(C9));
                ref C10 v10 = ref *(C10*)(list10Address + index * sizeof(C10));
                ref C11 v11 = ref *(C11*)(list11Address + index * sizeof(C11));
                ref C12 v12 = ref *(C12*)(list12Address + index * sizeof(C12));
                ref C13 v13 = ref *(C13*)(list13Address + index * sizeof(C13));
                return new Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11, ref v12, ref v13);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                ComponentType c6 = schema.GetComponent<C6>();
                ComponentType c7 = schema.GetComponent<C7>();
                ComponentType c8 = schema.GetComponent<C8>();
                ComponentType c9 = schema.GetComponent<C9>();
                ComponentType c10 = schema.GetComponent<C10>();
                ComponentType c11 = schema.GetComponent<C11>();
                ComponentType c12 = schema.GetComponent<C12>();
                ComponentType c13 = schema.GetComponent<C13>();
                ComponentType c14 = schema.GetComponent<C14>();
                return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13, ComponentType c14) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                nint list6Address = List.GetStartAddress(GetComponents(chunk, c6));
                nint list7Address = List.GetStartAddress(GetComponents(chunk, c7));
                nint list8Address = List.GetStartAddress(GetComponents(chunk, c8));
                nint list9Address = List.GetStartAddress(GetComponents(chunk, c9));
                nint list10Address = List.GetStartAddress(GetComponents(chunk, c10));
                nint list11Address = List.GetStartAddress(GetComponents(chunk, c11));
                nint list12Address = List.GetStartAddress(GetComponents(chunk, c12));
                nint list13Address = List.GetStartAddress(GetComponents(chunk, c13));
                nint list14Address = List.GetStartAddress(GetComponents(chunk, c14));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                ref C6 v6 = ref *(C6*)(list6Address + index * sizeof(C6));
                ref C7 v7 = ref *(C7*)(list7Address + index * sizeof(C7));
                ref C8 v8 = ref *(C8*)(list8Address + index * sizeof(C8));
                ref C9 v9 = ref *(C9*)(list9Address + index * sizeof(C9));
                ref C10 v10 = ref *(C10*)(list10Address + index * sizeof(C10));
                ref C11 v11 = ref *(C11*)(list11Address + index * sizeof(C11));
                ref C12 v12 = ref *(C12*)(list12Address + index * sizeof(C12));
                ref C13 v13 = ref *(C13*)(list13Address + index * sizeof(C13));
                ref C14 v14 = ref *(C14*)(list14Address + index * sizeof(C14));
                return new Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11, ref v12, ref v13, ref v14);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                ComponentType c6 = schema.GetComponent<C6>();
                ComponentType c7 = schema.GetComponent<C7>();
                ComponentType c8 = schema.GetComponent<C8>();
                ComponentType c9 = schema.GetComponent<C9>();
                ComponentType c10 = schema.GetComponent<C10>();
                ComponentType c11 = schema.GetComponent<C11>();
                ComponentType c12 = schema.GetComponent<C12>();
                ComponentType c13 = schema.GetComponent<C13>();
                ComponentType c14 = schema.GetComponent<C14>();
                ComponentType c15 = schema.GetComponent<C15>();
                return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13, ComponentType c14, ComponentType c15) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                nint list6Address = List.GetStartAddress(GetComponents(chunk, c6));
                nint list7Address = List.GetStartAddress(GetComponents(chunk, c7));
                nint list8Address = List.GetStartAddress(GetComponents(chunk, c8));
                nint list9Address = List.GetStartAddress(GetComponents(chunk, c9));
                nint list10Address = List.GetStartAddress(GetComponents(chunk, c10));
                nint list11Address = List.GetStartAddress(GetComponents(chunk, c11));
                nint list12Address = List.GetStartAddress(GetComponents(chunk, c12));
                nint list13Address = List.GetStartAddress(GetComponents(chunk, c13));
                nint list14Address = List.GetStartAddress(GetComponents(chunk, c14));
                nint list15Address = List.GetStartAddress(GetComponents(chunk, c15));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                ref C6 v6 = ref *(C6*)(list6Address + index * sizeof(C6));
                ref C7 v7 = ref *(C7*)(list7Address + index * sizeof(C7));
                ref C8 v8 = ref *(C8*)(list8Address + index * sizeof(C8));
                ref C9 v9 = ref *(C9*)(list9Address + index * sizeof(C9));
                ref C10 v10 = ref *(C10*)(list10Address + index * sizeof(C10));
                ref C11 v11 = ref *(C11*)(list11Address + index * sizeof(C11));
                ref C12 v12 = ref *(C12*)(list12Address + index * sizeof(C12));
                ref C13 v13 = ref *(C13*)(list13Address + index * sizeof(C13));
                ref C14 v14 = ref *(C14*)(list14Address + index * sizeof(C14));
                ref C15 v15 = ref *(C15*)(list15Address + index * sizeof(C15));
                return new Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11, ref v12, ref v13, ref v14, ref v15);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(Implementation* chunk, uint index) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                Schema schema = chunk->schema;
                ComponentType c1 = schema.GetComponent<C1>();
                ComponentType c2 = schema.GetComponent<C2>();
                ComponentType c3 = schema.GetComponent<C3>();
                ComponentType c4 = schema.GetComponent<C4>();
                ComponentType c5 = schema.GetComponent<C5>();
                ComponentType c6 = schema.GetComponent<C6>();
                ComponentType c7 = schema.GetComponent<C7>();
                ComponentType c8 = schema.GetComponent<C8>();
                ComponentType c9 = schema.GetComponent<C9>();
                ComponentType c10 = schema.GetComponent<C10>();
                ComponentType c11 = schema.GetComponent<C11>();
                ComponentType c12 = schema.GetComponent<C12>();
                ComponentType c13 = schema.GetComponent<C13>();
                ComponentType c14 = schema.GetComponent<C14>();
                ComponentType c15 = schema.GetComponent<C15>();
                ComponentType c16 = schema.GetComponent<C16>();
                return GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(chunk, index, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16);
            }

            public static Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(Implementation* chunk, uint index, ComponentType c1, ComponentType c2, ComponentType c3, ComponentType c4, ComponentType c5, ComponentType c6, ComponentType c7, ComponentType c8, ComponentType c9, ComponentType c10, ComponentType c11, ComponentType c12, ComponentType c13, ComponentType c14, ComponentType c15, ComponentType c16) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
            {
                Allocations.ThrowIfNull(chunk);

                nint list1Address = List.GetStartAddress(GetComponents(chunk, c1));
                nint list2Address = List.GetStartAddress(GetComponents(chunk, c2));
                nint list3Address = List.GetStartAddress(GetComponents(chunk, c3));
                nint list4Address = List.GetStartAddress(GetComponents(chunk, c4));
                nint list5Address = List.GetStartAddress(GetComponents(chunk, c5));
                nint list6Address = List.GetStartAddress(GetComponents(chunk, c6));
                nint list7Address = List.GetStartAddress(GetComponents(chunk, c7));
                nint list8Address = List.GetStartAddress(GetComponents(chunk, c8));
                nint list9Address = List.GetStartAddress(GetComponents(chunk, c9));
                nint list10Address = List.GetStartAddress(GetComponents(chunk, c10));
                nint list11Address = List.GetStartAddress(GetComponents(chunk, c11));
                nint list12Address = List.GetStartAddress(GetComponents(chunk, c12));
                nint list13Address = List.GetStartAddress(GetComponents(chunk, c13));
                nint list14Address = List.GetStartAddress(GetComponents(chunk, c14));
                nint list15Address = List.GetStartAddress(GetComponents(chunk, c15));
                nint list16Address = List.GetStartAddress(GetComponents(chunk, c16));
                ref C1 v1 = ref *(C1*)(list1Address + index * sizeof(C1));
                ref C2 v2 = ref *(C2*)(list2Address + index * sizeof(C2));
                ref C3 v3 = ref *(C3*)(list3Address + index * sizeof(C3));
                ref C4 v4 = ref *(C4*)(list4Address + index * sizeof(C4));
                ref C5 v5 = ref *(C5*)(list5Address + index * sizeof(C5));
                ref C6 v6 = ref *(C6*)(list6Address + index * sizeof(C6));
                ref C7 v7 = ref *(C7*)(list7Address + index * sizeof(C7));
                ref C8 v8 = ref *(C8*)(list8Address + index * sizeof(C8));
                ref C9 v9 = ref *(C9*)(list9Address + index * sizeof(C9));
                ref C10 v10 = ref *(C10*)(list10Address + index * sizeof(C10));
                ref C11 v11 = ref *(C11*)(list11Address + index * sizeof(C11));
                ref C12 v12 = ref *(C12*)(list12Address + index * sizeof(C12));
                ref C13 v13 = ref *(C13*)(list13Address + index * sizeof(C13));
                ref C14 v14 = ref *(C14*)(list14Address + index * sizeof(C14));
                ref C15 v15 = ref *(C15*)(list15Address + index * sizeof(C15));
                ref C16 v16 = ref *(C16*)(list16Address + index * sizeof(C16));
                return new Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>(chunk->entities[index], ref v1, ref v2, ref v3, ref v4, ref v5, ref v6, ref v7, ref v8, ref v9, ref v10, ref v11, ref v12, ref v13, ref v14, ref v15, ref v16);
            }

            [Conditional("DEBUG")]
            private static void ThrowIfComponentTypeIsMissing(Implementation* chunk, ComponentType componentType)
            {
                if (!chunk->definition.ComponentTypes.Contains(componentType))
                {
                    throw new ArgumentException($"Component type `{componentType.ToString(chunk->schema)}` is missing from the chunk");
                }
            }
        }
    }
}
