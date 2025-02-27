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
        public readonly USpan<uint> Entities
        {
            get
            {
                Allocations.ThrowIfNull(chunk);

                return chunk->entities.AsSpan();
            }
        }

        /// <summary>
        /// Amount of entities stored in this chunk.
        /// </summary>
        public readonly uint Count
        {
            get
            {
                Allocations.ThrowIfNull(chunk);

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
                Allocations.ThrowIfNull(chunk);

                return chunk->definition;
            }
        }

        public readonly uint this[uint index] => Entities[index];

#if NET
        /// <summary>
        /// Creates a new chunk.
        /// </summary>
        public Chunk()
        {
            Array<List> componentArrays = new(BitMask.Capacity);
            ref Pointer chunk = ref Allocations.Allocate<Pointer>();
            chunk = new(default, componentArrays, new(0));
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
            Array<List> componentArrays = new(BitMask.Capacity);
            USpan<byte> typeIndices = stackalloc byte[(int)BitMask.Capacity];
            byte typeCount = 0;
            for (uint c = 0; c < BitMask.Capacity; c++)
            {
                if (definition.ComponentTypes.Contains(c))
                {
                    ushort componentSize = schema.GetComponentTypeSize(c);
                    componentArrays[c] = new(4, componentSize);
                    typeIndices[typeCount++] = (byte)c;
                }
            }

            ref Pointer chunk = ref Allocations.Allocate<Pointer>();
            chunk = new(definition, componentArrays, new(typeIndices.GetSpan(typeCount)));
            fixed (Pointer* pointer = &chunk)
            {
                this.chunk = pointer;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Allocations.ThrowIfNull(chunk);

            chunk->entities.Dispose();
            uint typeCount = chunk->typeIndices.Length;
            for (uint t = 0; t < typeCount; t++)
            {
                List components = chunk->componentLists[chunk->typeIndices[t]];
                components.Dispose();
            }

            chunk->componentLists.Dispose();
            chunk->typeIndices.Dispose();
            Allocations.Free(ref chunk);
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
        private readonly void ThrowIfComponentTypeIsMissing(uint componentType)
        {
            if (!chunk->definition.ComponentTypes.Contains(componentType))
            {
                throw new ArgumentException($"Component type `{new ComponentType(componentType).ToString()}` is missing from the chunk");
            }
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[512];
            uint length = ToString(buffer);
            return buffer.GetSpan(length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this chunk.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            uint length = 0;
            BitMask componentTypes = Definition.ComponentTypes;
            for (uint i = 0; i < BitMask.Capacity; i++)
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
        /// Adds the given <paramref name="entity"/> into this chunk and returns its referable index.
        /// </summary>
        public readonly uint AddEntity(uint entity)
        {
            Allocations.ThrowIfNull(chunk);

            chunk->entities.Add(entity);
            uint typeCount = chunk->typeIndices.Length;
            for (uint t = 0; t < typeCount; t++)
            {
                List components = chunk->componentLists[chunk->typeIndices[t]];
                components.AddDefault();
            }

            return chunk->entities.Count - 1;
        }

        /// <summary>
        /// Removes the given <paramref name="entity"/> from this chunk.
        /// </summary>
        public readonly void RemoveEntity(uint entity)
        {
            Allocations.ThrowIfNull(chunk);

            uint index = chunk->entities.IndexOf(entity);
            chunk->entities.RemoveAtBySwapping(index);
            uint typeCount = chunk->typeIndices.Length;
            for (uint t = 0; t < typeCount; t++)
            {
                List components = chunk->componentLists[chunk->typeIndices[t]];
                components.RemoveAtBySwapping(index);
            }
        }

        /// <summary>
        /// Moves the <paramref name="entity"/> and all of its components to the <paramref name="destination"/> chunk.
        /// </summary>
        public readonly uint MoveEntity(uint entity, Chunk destination)
        {
            Allocations.ThrowIfNull(chunk);
            Allocations.ThrowIfNull(destination.chunk);

            uint oldIndex = chunk->entities.IndexOf(entity);
            chunk->entities.RemoveAtBySwapping(oldIndex);
            uint newIndex = destination.chunk->entities.Count;
            destination.chunk->entities.Add(entity);

            //copy from source to destination
            for (uint t = 0; t < destination.chunk->typeIndices.Length; t++)
            {
                byte destinationComponentType = destination.chunk->typeIndices[t];
                List destinationComponents = destination.chunk->componentLists[destinationComponentType];
                if (chunk->typeIndices.Contains(destinationComponentType))
                {
                    List sourceComponents = chunk->componentLists[destinationComponentType];
                    Allocation oldComponent = sourceComponents[oldIndex];
                    destinationComponents.Insert(newIndex, oldComponent);
                }
                else
                {
                    destinationComponents.AddDefault();
                }
            }

            //remove from source
            for (uint t = 0; t < chunk->typeIndices.Length; t++)
            {
                List components = chunk->componentLists[chunk->typeIndices[t]];
                components.RemoveAtBySwapping(oldIndex);
            }

            return newIndex;
        }

        /// <summary>
        /// Retrieves the list of all components of the given <paramref name="componentType"/>.
        /// </summary>
        public readonly List GetComponents(ComponentType componentType)
        {
            Allocations.ThrowIfNull(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            return chunk->componentLists[componentType.index];
        }

        /// <summary>
        /// Retrieves a span containing all <typeparamref name="T"/> components.
        /// </summary>
        public readonly USpan<T> GetComponents<T>(ComponentType componentType) where T : unmanaged
        {
            Allocations.ThrowIfNull(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            return chunk->componentLists[componentType.index].AsSpan<T>();
        }

        /// <summary>
        /// Retrieves a span containing all <typeparamref name="T"/> components.
        /// </summary>
        public readonly USpan<T> GetComponents<T>(uint componentType) where T : unmanaged
        {
            Allocations.ThrowIfNull(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            return chunk->componentLists[componentType].AsSpan<T>();
        }

        /// <summary>
        /// Retrieves a reference to the component of type <paramref name="componentType"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint index, ComponentType componentType) where T : unmanaged
        {
            Allocations.ThrowIfNull(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            return ref chunk->componentLists[componentType.index].Get<T>(index);
        }

        /// <summary>
        /// Retrieves a reference to the component of type <paramref name="componentType"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint index, uint componentType) where T : unmanaged
        {
            Allocations.ThrowIfNull(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            return ref chunk->componentLists[componentType].Get<T>(index);
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <paramref name="componentType"/> at <paramref name="index"/>.
        /// </summary>
        public readonly Allocation GetComponent(uint index, ComponentType componentType)
        {
            return GetComponent(index, componentType.index);
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <paramref name="componentType"/> at <paramref name="index"/>.
        /// </summary>
        public readonly Allocation GetComponent(uint index, uint componentType)
        {
            Allocations.ThrowIfNull(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            return chunk->componentLists[componentType][index];
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <paramref name="componentType"/> at <paramref name="index"/>.
        /// </summary>
        public readonly Allocation GetComponent(uint index, ComponentType componentType, out ushort componentSize)
        {
            Allocations.ThrowIfNull(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            List components = chunk->componentLists[componentType.index];
            componentSize = (ushort)components.Stride;
            return components[index];
        }

        /// <summary>
        /// Retrieves the pointer for the specific component of the type <paramref name="componentType"/> at <paramref name="index"/>.
        /// </summary>
        public readonly Allocation GetComponent(uint index, uint componentType, out ushort componentSize)
        {
            Allocations.ThrowIfNull(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            List components = chunk->componentLists[componentType];
            componentSize = (ushort)components.Stride;
            return components[index];
        }

        /// <summary>
        /// Assigns the component for entity at <paramref name="index"/> to <paramref name="value"/>.
        /// </summary>
        public readonly void SetComponent<T>(uint index, ComponentType componentType, T value) where T : unmanaged
        {
            SetComponent(index, componentType.index, value);
        }

        /// <summary>
        /// Assigns the component for entity at <paramref name="index"/> to <paramref name="value"/>.
        /// </summary>
        public readonly void SetComponent<T>(uint index, uint componentType, T value) where T : unmanaged
        {
            Allocations.ThrowIfNull(chunk);
            ThrowIfComponentTypeIsMissing(componentType);

            chunk->componentLists[componentType].Set(index, value);
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
            Array<List> componentArrays = new(BitMask.Capacity);
            ref Pointer chunk = ref Allocations.Allocate<Pointer>();
            chunk = new(default, componentArrays, new(0));
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