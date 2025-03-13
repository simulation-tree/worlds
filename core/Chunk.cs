using Collections;
using Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;
using Pointer = Worlds.Pointers.Chunk;

namespace Worlds
{
    /// <summary>
    /// Stores components of entities with the same component, array and tags.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct Chunk : IDisposable, IEquatable<Chunk>
    {
        private Pointer* chunk;

        /// <summary>
        /// Checks if this chunk is disposed.
        /// </summary>
        public readonly bool IsDisposed => chunk is null;

        /// <summary>
        /// Native address of this chunk.
        /// </summary>
        public readonly nint Address => (nint)chunk;

        /// <summary>
        /// Returns the entities in this chunk.
        /// </summary>
        public readonly ReadOnlySpan<uint> Entities
        {
            get
            {
                MemoryAddress.ThrowIfDefault(chunk);

                return chunk->entities.AsSpan();
            }
        }

        /// <summary>
        /// Rows of components for each entity in this chunk.
        /// </summary>
        internal readonly List Components
        {
            get
            {
                MemoryAddress.ThrowIfDefault(chunk);

                return chunk->components;
            }
        }

        /// <summary>
        /// Amount of entities stored in this chunk.
        /// </summary>
        public readonly int Count
        {
            get
            {
                MemoryAddress.ThrowIfDefault(chunk);

                return chunk->entities.Count;
            }
        }

        /// <summary>
        /// Returns the definition representing the types of components, arrays and tags
        /// this chunk is for.
        /// </summary>
        public readonly Definition Definition
        {
            get
            {
                MemoryAddress.ThrowIfDefault(chunk);

                return chunk->definition;
            }
        }

        public readonly uint this[int index] => Entities[index];

#if NET
        /// <summary>
        /// Creates a new chunk.
        /// </summary>
        public Chunk()
        {
            ref Pointer chunk = ref MemoryAddress.Allocate<Pointer>();
            chunk = Pointer.Create();
            fixed (Pointer* pointer = &chunk)
            {
                this.chunk = pointer;
            }
        }
#endif

        /// <summary>
        /// Initializes an existing chunk from the given <paramref name="pointer"/>.
        /// </summary>
        public Chunk(void* pointer)
        {
            chunk = (Pointer*)pointer;
        }

        /// <summary>
        /// Creates a new chunk with the given <paramref name="definition"/>.
        /// </summary>
        public Chunk(Definition definition, Schema schema)
        {
            BitMask componentTypes = definition.ComponentTypes;
            Span<int> componentOffsets = stackalloc int[BitMask.Capacity];
            Span<int> componentLengths = stackalloc int[BitMask.Capacity];
            int offset = 0;
            for (int c = 0; c < BitMask.Capacity; c++)
            {
                if (componentTypes.Contains(c))
                {
                    int componentSize = schema.GetComponentTypeSize(c);
                    componentOffsets[c] = offset;
                    componentLengths[c] = componentSize;
                    offset += componentSize;
                }
            }

            ref Pointer chunk = ref MemoryAddress.Allocate<Pointer>();
            chunk = new(definition, offset, componentOffsets, componentLengths);
            fixed (Pointer* pointer = &chunk)
            {
                this.chunk = pointer;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(chunk);

            chunk->entities.Dispose();
            chunk->components.Dispose();
            chunk->componentOffsets.Dispose();
            chunk->componentSizes.Dispose();
            MemoryAddress.Free(ref chunk);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeIsMissing(ComponentType componentType)
        {
            if (!chunk->definition.ComponentTypes.Contains(componentType.index))
            {
                throw new ArgumentException($"Component type `{componentType.ToString()}` is missing from the chunk");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeIsMissing(int componentType)
        {
            if (!chunk->definition.ComponentTypes.Contains(componentType))
            {
                throw new ArgumentException($"Component type `{new ComponentType(componentType).ToString()}` is missing from the chunk");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfEntityIsMissing(uint entity)
        {
            if (!chunk->entities.Contains(entity))
            {
                throw new ArgumentException($"Entity `{entity}` is missing from the chunk");
            }
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[512];
            int length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this chunk.
        /// </summary>
        public readonly int ToString(Span<char> buffer)
        {
            int length = 0;
            BitMask componentTypes = Definition.ComponentTypes;
            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (componentTypes.Contains(i))
                {
                    ComponentType componentType = new(i);
                    length += componentType.ToString(buffer.Slice(length));
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
        /// Retrieves the offset and size of the <paramref name="componentType"/>
        /// for this chunk.
        /// </summary>
        public readonly (int offset, int size) GetComponentRange(int componentType)
        {
            MemoryAddress.ThrowIfDefault(chunk);

            int offset = chunk->componentOffsets.ReadElement<int>(componentType);
            int size = chunk->componentSizes.ReadElement<int>(componentType);
            return (offset, size);
        }

        /// <summary>
        /// Retrieves the byte offset of the <paramref name="componentType"/>
        /// </summary>
        public readonly int GetComponentOffset(int componentType)
        {
            MemoryAddress.ThrowIfDefault(chunk);

            return chunk->componentOffsets.ReadElement<int>(componentType);
        }

        /// <summary>
        /// Adds the given <paramref name="entity"/> into this chunk and returns its referable index.
        /// </summary>
        public readonly int AddEntity(uint entity)
        {
            MemoryAddress.ThrowIfDefault(chunk);

            chunk->entities.Add(entity);
            chunk->components.AddDefault();
            return chunk->entities.Count - 1;
        }

        /// <summary>
        /// Removes the entity at the given <paramref name="index"/>.
        /// </summary>
        public readonly void RemoveEntityAt(int index)
        {
            MemoryAddress.ThrowIfDefault(chunk);

            chunk->entities.RemoveAtBySwapping(index);
            chunk->components.RemoveAtBySwapping(index);
        }

        /// <summary>
        /// Moves the entity at <paramref name="index"/> and all of its components to the <paramref name="destination"/> chunk,
        /// and modifies it to match the new index.
        /// </summary>
        public readonly void MoveEntityAt(ref int index, Chunk destination)
        {
            MemoryAddress.ThrowIfDefault(chunk);
            MemoryAddress.ThrowIfDefault(destination.chunk);

            chunk->entities.RemoveAtBySwapping(index, out uint entity);
            int newIndex = destination.chunk->entities.Count;
            destination.chunk->entities.Add(entity);

            MemoryAddress sourceComponentRow = chunk->components[index];
            destination.chunk->components.AddDefault(out MemoryAddress destinationComponentRow);

            Span<int> sourceOffsets = chunk->componentOffsets.GetSpan<int>(BitMask.Capacity);
            Span<int> sourceSizes = chunk->componentSizes.GetSpan<int>(BitMask.Capacity);
            Span<int> destinationOffsets = destination.chunk->componentOffsets.GetSpan<int>(BitMask.Capacity);
            BitMask sourceComponentTypes = chunk->definition.ComponentTypes;
            BitMask destinationComponentTypes = destination.chunk->definition.ComponentTypes;

            //copy from source to destination
            for (int t = 0; t < BitMask.Capacity; t++)
            {
                if (sourceComponentTypes.Contains(t) && destinationComponentTypes.Contains(t))
                {
                    int sourceOffset = sourceOffsets[t];
                    int destinationOffset = destinationOffsets[t];
                    int length = sourceSizes[t];
                    sourceComponentRow.CopyTo(destinationComponentRow, sourceOffset, destinationOffset, length);
                }
            }

            chunk->components.RemoveAtBySwapping(index);
            index = newIndex;
        }

        /// <summary>
        /// Retrieves an enumerator for iterating through each component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ComponentEnumerator<T> GetEnumerator<T>(int componentType) where T : unmanaged
        {
            int componentOffset = chunk->componentOffsets.ReadElement<int>(componentType);
            return new(chunk->components, componentOffset);
        }

        /// <summary>
        /// Retrieves the index of the given <paramref name="entity"/>.
        /// </summary>
        public readonly int IndexOf(uint entity)
        {
            MemoryAddress.ThrowIfDefault(chunk);
            ThrowIfEntityIsMissing(entity);

            return chunk->entities.IndexOf(entity);
        }

        /// <summary>
        /// Retrieves a reference to the component of type <paramref name="componentType"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(int index, int componentType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            int componentOffset = chunk->componentOffsets.ReadElement<int>(componentType);
            return ref chunk->components[index].Read<T>(componentOffset);
        }

        /// <summary>
        /// Retrieves a reference to the component of type <paramref name="componentType"/>.
        /// </summary>
        public readonly ref T GetComponentOfEntity<T>(uint entity, int componentType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(chunk);
            ThrowIfComponentTypeIsMissing(componentType);
            ThrowIfEntityIsMissing(entity);

            int componentOffset = chunk->componentOffsets.ReadElement<int>(componentType);
            return ref chunk->components[chunk->entities.IndexOf(entity)].Read<T>(componentOffset);
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <paramref name="componentType"/> at <paramref name="index"/>.
        /// </summary>
        public readonly MemoryAddress GetComponent(int index, int componentType)
        {
            MemoryAddress.ThrowIfDefault(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            int componentOffset = chunk->componentOffsets.ReadElement<int>(componentType);
            return chunk->components[index].Read(componentOffset);
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <paramref name="componentType"/> of <paramref name="entity"/>.
        /// </summary>
        public readonly MemoryAddress GetComponentOfEntity(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(chunk);
            ThrowIfComponentTypeIsMissing(componentType);
            ThrowIfEntityIsMissing(entity);

            int componentOffset = chunk->componentOffsets.ReadElement<int>(componentType);
            return chunk->components[chunk->entities.IndexOf(entity)].Read(componentOffset);
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <paramref name="componentType"/> at <paramref name="index"/>.
        /// </summary>
        public readonly MemoryAddress GetComponent(int index, int componentType, out int componentSize)
        {
            MemoryAddress.ThrowIfDefault(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            int componentOffset = chunk->componentOffsets.ReadElement<int>(componentType);
            componentSize = chunk->componentSizes.ReadElement<int>(componentType);
            return chunk->components[index].Read(componentOffset);
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <paramref name="componentType"/> of <paramref name="entity"/>.
        /// </summary>
        public readonly MemoryAddress GetComponentOfEntity(uint entity, int componentType, out int componentSize)
        {
            MemoryAddress.ThrowIfDefault(chunk);
            ThrowIfComponentTypeIsMissing(componentType);
            ThrowIfEntityIsMissing(entity);

            int componentOffset = chunk->componentOffsets.ReadElement<int>(componentType);
            componentSize = chunk->componentSizes.ReadElement<int>(componentType);
            return chunk->components[chunk->entities.IndexOf(entity)].Read(componentOffset);
        }

        /// <summary>
        /// Assigns the component for entity at <paramref name="index"/> to <paramref name="value"/>.
        /// </summary>
        public readonly void SetComponent<T>(int index, int componentType, T value) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            int componentOffset = chunk->componentOffsets.ReadElement<int>(componentType);
            chunk->components[index].Write(componentOffset, value);
        }

        /// <summary>
        /// Assigns the component for <paramref name="entity"/> to <paramref name="value"/>.
        /// </summary>
        public readonly void SetComponentOfEntity<T>(uint entity, int componentType, T value) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(chunk);
            ThrowIfComponentTypeIsMissing(componentType);
            ThrowIfEntityIsMissing(entity);

            int componentOffset = chunk->componentOffsets.ReadElement<int>(componentType);
            chunk->components[chunk->entities.IndexOf(entity)].Write(componentOffset, value);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Chunk chunk && Equals(chunk);
        }

        public readonly bool Equals(Chunk other)
        {
            return (nint)chunk == (nint)other.chunk;
        }

        public static bool operator ==(Chunk left, Chunk right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Chunk left, Chunk right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Creates a new empty chunk.
        /// </summary>
        public static Chunk Create()
        {
            ref Pointer chunk = ref MemoryAddress.Allocate<Pointer>();
            chunk = Pointer.Create();
            fixed (Pointer* pointer = &chunk)
            {
                return new(pointer);
            }
        }

        public readonly ref struct Entity<C1> where C1 : unmanaged
        {
            public readonly uint entity;
#if NET
            public readonly ref C1 component1;
#else
            private readonly void* c1;
            public readonly ref C1 component1 => ref *(C1*)c1;
#endif

            public Entity(uint entity, ref C1 component1)
            {
                this.entity = entity;
#if NET
                this.component1 = ref component1;
#else
                fixed (void* ptr = &component1)
                {
                    c1 = ptr;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2> where C1 : unmanaged where C2 : unmanaged
        {
            public readonly uint entity;
#if NET
            public readonly ref C1 component1;
            public readonly ref C2 component2;
#else
            private readonly void* c1;
            private readonly void* c2;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2)
            {
                this.entity = entity;
#if NET
                this.component1 = ref component1;
                this.component2 = ref component2;
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }

                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            public readonly uint entity;
#if NET
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
#endif
            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3)
            {
                this.entity = entity;
#if NET
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            public readonly uint entity;
#if NET
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
#endif
            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4)
            {
                this.entity = entity;
#if NET
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            public readonly uint entity;
#if NET
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5)
            {
                this.entity = entity;
#if NET
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            public readonly uint entity;
#if NET
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            private readonly void* c6;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
            public readonly ref C6 component6 => ref *(C6*)c6;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6)
            {
                this.entity = entity;
#if NET
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
                fixed (void* ptr6 = &component6)
                {
                    c6 = ptr6;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            public readonly uint entity;
#if NET
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            private readonly void* c6;
            private readonly void* c7;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
            public readonly ref C6 component6 => ref *(C6*)c6;
            public readonly ref C7 component7 => ref *(C7*)c7;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7)
            {
                this.entity = entity;
#if NET
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
                fixed (void* ptr6 = &component6)
                {
                    c6 = ptr6;
                }
                fixed (void* ptr7 = &component7)
                {
                    c7 = ptr7;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            public readonly uint entity;
#if NET
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
            public readonly ref C8 component8;
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            private readonly void* c6;
            private readonly void* c7;
            private readonly void* c8;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
            public readonly ref C6 component6 => ref *(C6*)c6;
            public readonly ref C7 component7 => ref *(C7*)c7;
            public readonly ref C8 component8 => ref *(C8*)c8;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8)
            {
                this.entity = entity;
#if NET
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
                this.component8 = ref component8;
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
                fixed (void* ptr6 = &component6)
                {
                    c6 = ptr6;
                }
                fixed (void* ptr7 = &component7)
                {
                    c7 = ptr7;
                }
                fixed (void* ptr8 = &component8)
                {
                    c8 = ptr8;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            public readonly uint entity;
#if NET
            public readonly ref C1 component1;
            public readonly ref C2 component2;
            public readonly ref C3 component3;
            public readonly ref C4 component4;
            public readonly ref C5 component5;
            public readonly ref C6 component6;
            public readonly ref C7 component7;
            public readonly ref C8 component8;
            public readonly ref C9 component9;
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            private readonly void* c6;
            private readonly void* c7;
            private readonly void* c8;
            private readonly void* c9;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
            public readonly ref C6 component6 => ref *(C6*)c6;
            public readonly ref C7 component7 => ref *(C7*)c7;
            public readonly ref C8 component8 => ref *(C8*)c8;
            public readonly ref C9 component9 => ref *(C9*)c9;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9)
            {
                this.entity = entity;
#if NET
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
                this.component8 = ref component8;
                this.component9 = ref component9;
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
                fixed (void* ptr6 = &component6)
                {
                    c6 = ptr6;
                }
                fixed (void* ptr7 = &component7)
                {
                    c7 = ptr7;
                }
                fixed (void* ptr8 = &component8)
                {
                    c8 = ptr8;
                }
                fixed (void* ptr9 = &component9)
                {
                    c9 = ptr9;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            public readonly uint entity;
#if NET
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
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            private readonly void* c6;
            private readonly void* c7;
            private readonly void* c8;
            private readonly void* c9;
            private readonly void* c10;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
            public readonly ref C6 component6 => ref *(C6*)c6;
            public readonly ref C7 component7 => ref *(C7*)c7;
            public readonly ref C8 component8 => ref *(C8*)c8;
            public readonly ref C9 component9 => ref *(C9*)c9;
            public readonly ref C10 component10 => ref *(C10*)c10;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10)
            {
                this.entity = entity;
#if NET
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
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
                fixed (void* ptr6 = &component6)
                {
                    c6 = ptr6;
                }
                fixed (void* ptr7 = &component7)
                {
                    c7 = ptr7;
                }
                fixed (void* ptr8 = &component8)
                {
                    c8 = ptr8;
                }
                fixed (void* ptr9 = &component9)
                {
                    c9 = ptr9;
                }
                fixed (void* ptr10 = &component10)
                {
                    c10 = ptr10;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            public readonly uint entity;
#if NET
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
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            private readonly void* c6;
            private readonly void* c7;
            private readonly void* c8;
            private readonly void* c9;
            private readonly void* c10;
            private readonly void* c11;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
            public readonly ref C6 component6 => ref *(C6*)c6;
            public readonly ref C7 component7 => ref *(C7*)c7;
            public readonly ref C8 component8 => ref *(C8*)c8;
            public readonly ref C9 component9 => ref *(C9*)c9;
            public readonly ref C10 component10 => ref *(C10*)c10;
            public readonly ref C11 component11 => ref *(C11*)c11;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11)
            {
                this.entity = entity;
#if NET
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
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
                fixed (void* ptr6 = &component6)
                {
                    c6 = ptr6;
                }
                fixed (void* ptr7 = &component7)
                {
                    c7 = ptr7;
                }
                fixed (void* ptr8 = &component8)
                {
                    c8 = ptr8;
                }
                fixed (void* ptr9 = &component9)
                {
                    c9 = ptr9;
                }
                fixed (void* ptr10 = &component10)
                {
                    c10 = ptr10;
                }
                fixed (void* ptr11 = &component11)
                {
                    c11 = ptr11;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            public readonly uint entity;
#if NET
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
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            private readonly void* c6;
            private readonly void* c7;
            private readonly void* c8;
            private readonly void* c9;
            private readonly void* c10;
            private readonly void* c11;
            private readonly void* c12;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
            public readonly ref C6 component6 => ref *(C6*)c6;
            public readonly ref C7 component7 => ref *(C7*)c7;
            public readonly ref C8 component8 => ref *(C8*)c8;
            public readonly ref C9 component9 => ref *(C9*)c9;
            public readonly ref C10 component10 => ref *(C10*)c10;
            public readonly ref C11 component11 => ref *(C11*)c11;
            public readonly ref C12 component12 => ref *(C12*)c12;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11, ref C12 component12)
            {
                this.entity = entity;
#if NET
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
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
                fixed (void* ptr6 = &component6)
                {
                    c6 = ptr6;
                }
                fixed (void* ptr7 = &component7)
                {
                    c7 = ptr7;
                }
                fixed (void* ptr8 = &component8)
                {
                    c8 = ptr8;
                }
                fixed (void* ptr9 = &component9)
                {
                    c9 = ptr9;
                }
                fixed (void* ptr10 = &component10)
                {
                    c10 = ptr10;
                }
                fixed (void* ptr11 = &component11)
                {
                    c11 = ptr11;
                }
                fixed (void* ptr12 = &component12)
                {
                    c12 = ptr12;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            public readonly uint entity;
#if NET
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
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            private readonly void* c6;
            private readonly void* c7;
            private readonly void* c8;
            private readonly void* c9;
            private readonly void* c10;
            private readonly void* c11;
            private readonly void* c12;
            private readonly void* c13;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
            public readonly ref C6 component6 => ref *(C6*)c6;
            public readonly ref C7 component7 => ref *(C7*)c7;
            public readonly ref C8 component8 => ref *(C8*)c8;
            public readonly ref C9 component9 => ref *(C9*)c9;
            public readonly ref C10 component10 => ref *(C10*)c10;
            public readonly ref C11 component11 => ref *(C11*)c11;
            public readonly ref C12 component12 => ref *(C12*)c12;
            public readonly ref C13 component13 => ref *(C13*)c13;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11, ref C12 component12, ref C13 component13)
            {
                this.entity = entity;
#if NET
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
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
                fixed (void* ptr6 = &component6)
                {
                    c6 = ptr6;
                }
                fixed (void* ptr7 = &component7)
                {
                    c7 = ptr7;
                }
                fixed (void* ptr8 = &component8)
                {
                    c8 = ptr8;
                }
                fixed (void* ptr9 = &component9)
                {
                    c9 = ptr9;
                }
                fixed (void* ptr10 = &component10)
                {
                    c10 = ptr10;
                }
                fixed (void* ptr11 = &component11)
                {
                    c11 = ptr11;
                }
                fixed (void* ptr12 = &component12)
                {
                    c12 = ptr12;
                }
                fixed (void* ptr13 = &component13)
                {
                    c13 = ptr13;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            public readonly uint entity;
#if NET
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
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            private readonly void* c6;
            private readonly void* c7;
            private readonly void* c8;
            private readonly void* c9;
            private readonly void* c10;
            private readonly void* c11;
            private readonly void* c12;
            private readonly void* c13;
            private readonly void* c14;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
            public readonly ref C6 component6 => ref *(C6*)c6;
            public readonly ref C7 component7 => ref *(C7*)c7;
            public readonly ref C8 component8 => ref *(C8*)c8;
            public readonly ref C9 component9 => ref *(C9*)c9;
            public readonly ref C10 component10 => ref *(C10*)c10;
            public readonly ref C11 component11 => ref *(C11*)c11;
            public readonly ref C12 component12 => ref *(C12*)c12;
            public readonly ref C13 component13 => ref *(C13*)c13;
            public readonly ref C14 component14 => ref *(C14*)c14;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11, ref C12 component12, ref C13 component13, ref C14 component14)
            {
                this.entity = entity;
#if NET
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
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
                fixed (void* ptr6 = &component6)
                {
                    c6 = ptr6;
                }
                fixed (void* ptr7 = &component7)
                {
                    c7 = ptr7;
                }
                fixed (void* ptr8 = &component8)
                {
                    c8 = ptr8;
                }
                fixed (void* ptr9 = &component9)
                {
                    c9 = ptr9;
                }
                fixed (void* ptr10 = &component10)
                {
                    c10 = ptr10;
                }
                fixed (void* ptr11 = &component11)
                {
                    c11 = ptr11;
                }
                fixed (void* ptr12 = &component12)
                {
                    c12 = ptr12;
                }
                fixed (void* ptr13 = &component13)
                {
                    c13 = ptr13;
                }
                fixed (void* ptr14 = &component14)
                {
                    c14 = ptr14;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            public readonly uint entity;
#if NET
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
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            private readonly void* c6;
            private readonly void* c7;
            private readonly void* c8;
            private readonly void* c9;
            private readonly void* c10;
            private readonly void* c11;
            private readonly void* c12;
            private readonly void* c13;
            private readonly void* c14;
            private readonly void* c15;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
            public readonly ref C6 component6 => ref *(C6*)c6;
            public readonly ref C7 component7 => ref *(C7*)c7;
            public readonly ref C8 component8 => ref *(C8*)c8;
            public readonly ref C9 component9 => ref *(C9*)c9;
            public readonly ref C10 component10 => ref *(C10*)c10;
            public readonly ref C11 component11 => ref *(C11*)c11;
            public readonly ref C12 component12 => ref *(C12*)c12;
            public readonly ref C13 component13 => ref *(C13*)c13;
            public readonly ref C14 component14 => ref *(C14*)c14;
            public readonly ref C15 component15 => ref *(C15*)c15;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11, ref C12 component12, ref C13 component13, ref C14 component14, ref C15 component15)
            {
                this.entity = entity;
#if NET
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
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
                fixed (void* ptr6 = &component6)
                {
                    c6 = ptr6;
                }
                fixed (void* ptr7 = &component7)
                {
                    c7 = ptr7;
                }
                fixed (void* ptr8 = &component8)
                {
                    c8 = ptr8;
                }
                fixed (void* ptr9 = &component9)
                {
                    c9 = ptr9;
                }
                fixed (void* ptr10 = &component10)
                {
                    c10 = ptr10;
                }
                fixed (void* ptr11 = &component11)
                {
                    c11 = ptr11;
                }
                fixed (void* ptr12 = &component12)
                {
                    c12 = ptr12;
                }
                fixed (void* ptr13 = &component13)
                {
                    c13 = ptr13;
                }
                fixed (void* ptr14 = &component14)
                {
                    c14 = ptr14;
                }
                fixed (void* ptr15 = &component15)
                {
                    c15 = ptr15;
                }
#endif
            }
        }

        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            public readonly uint entity;
#if NET
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
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            private readonly void* c4;
            private readonly void* c5;
            private readonly void* c6;
            private readonly void* c7;
            private readonly void* c8;
            private readonly void* c9;
            private readonly void* c10;
            private readonly void* c11;
            private readonly void* c12;
            private readonly void* c13;
            private readonly void* c14;
            private readonly void* c15;
            private readonly void* c16;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
            public readonly ref C4 component4 => ref *(C4*)c4;
            public readonly ref C5 component5 => ref *(C5*)c5;
            public readonly ref C6 component6 => ref *(C6*)c6;
            public readonly ref C7 component7 => ref *(C7*)c7;
            public readonly ref C8 component8 => ref *(C8*)c8;
            public readonly ref C9 component9 => ref *(C9*)c9;
            public readonly ref C10 component10 => ref *(C10*)c10;
            public readonly ref C11 component11 => ref *(C11*)c11;
            public readonly ref C12 component12 => ref *(C12*)c12;
            public readonly ref C13 component13 => ref *(C13*)c13;
            public readonly ref C14 component14 => ref *(C14*)c14;
            public readonly ref C15 component15 => ref *(C15*)c15;
            public readonly ref C16 component16 => ref *(C16*)c16;
#endif

            public Entity(uint entity, ref C1 component1, ref C2 component2, ref C3 component3, ref C4 component4, ref C5 component5, ref C6 component6, ref C7 component7, ref C8 component8, ref C9 component9, ref C10 component10, ref C11 component11, ref C12 component12, ref C13 component13, ref C14 component14, ref C15 component15, ref C16 component16)
            {
                this.entity = entity;
#if NET
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
#else
                fixed (void* ptr1 = &component1)
                {
                    c1 = ptr1;
                }
                fixed (void* ptr2 = &component2)
                {
                    c2 = ptr2;
                }
                fixed (void* ptr3 = &component3)
                {
                    c3 = ptr3;
                }
                fixed (void* ptr4 = &component4)
                {
                    c4 = ptr4;
                }
                fixed (void* ptr5 = &component5)
                {
                    c5 = ptr5;
                }
                fixed (void* ptr6 = &component6)
                {
                    c6 = ptr6;
                }
                fixed (void* ptr7 = &component7)
                {
                    c7 = ptr7;
                }
                fixed (void* ptr8 = &component8)
                {
                    c8 = ptr8;
                }
                fixed (void* ptr9 = &component9)
                {
                    c9 = ptr9;
                }
                fixed (void* ptr10 = &component10)
                {
                    c10 = ptr10;
                }
                fixed (void* ptr11 = &component11)
                {
                    c11 = ptr11;
                }
                fixed (void* ptr12 = &component12)
                {
                    c12 = ptr12;
                }
                fixed (void* ptr13 = &component13)
                {
                    c13 = ptr13;
                }
                fixed (void* ptr14 = &component14)
                {
                    c14 = ptr14;
                }
                fixed (void* ptr15 = &component15)
                {
                    c15 = ptr15;
                }
                fixed (void* ptr16 = &component16)
                {
                    c16 = ptr16;
                }
#endif
            }
        }
    }
}