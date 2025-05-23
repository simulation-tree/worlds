using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;
using Worlds.Pointers;

namespace Worlds
{
    /// <summary>
    /// Stores components of entities with the same component, array and tags.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct Chunk : IDisposable, IEquatable<Chunk>
    {
        internal ChunkPointer* chunk;

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

                return new ReadOnlySpan<uint>(chunk->entities.Items.Pointer + sizeof(uint), chunk->count);
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

                return chunk->count;
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

        /// <summary>
        /// Retrieves the components for the entity in the row at the given <paramref name="index"/>.
        /// </summary>
        public readonly Row this[int index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(chunk);

                MemoryAddress row = chunk->components[index];
                return new Row(chunk->schema, row);
            }
        }

        /// <summary>
        /// Version of the chunk that increments when entities are added and removed.
        /// </summary>
        public readonly int Version
        {
            get
            {
                MemoryAddress.ThrowIfDefault(chunk);

                return chunk->version;
            }
        }

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public Chunk()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Creates a new chunk with the given <paramref name="definition"/>.
        /// </summary>
        public Chunk(Schema schema, Definition definition = default)
        {
            chunk = MemoryAddress.AllocatePointer<ChunkPointer>();
            chunk->lastEntity = 0;
            chunk->count = 0;
            chunk->version = 0;
            chunk->entities = new(4);
            chunk->entities.AddDefault(); //reserved
            chunk->components = new(4, schema.schema->componentRowSize);
            chunk->components.AddDefault(); //reserved
            chunk->schema = schema;
            chunk->definition = definition;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(chunk);

            chunk->entities.Dispose();
            chunk->components.Dispose();
            MemoryAddress.Free(ref chunk);
        }

        internal readonly void UpdateStrideToMatchSchema()
        {
            chunk->components.Dispose();
            chunk->components = new(4, chunk->schema.schema->componentRowSize);
            chunk->components.AddDefault(); //reserved
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeIsMissing(int componentType)
        {
            if (!chunk->definition.componentTypes.Contains(componentType))
            {
                throw new ArgumentException($"Component type `{DataType.GetComponent(componentType, chunk->schema).ToString(chunk->schema)}` is missing from the chunk");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfIndexIsOutOfRange(int index)
        {
            if (index == 0)
            {
                throw new ArgumentException("Index must be greater than zero to access the first entity in the chunk");
            }
            else if (index > chunk->count)
            {
                throw new ArgumentOutOfRangeException($"Index `{index}` is out of range for the chunk with count `{chunk->count}`");
            }
            else if (index < 0)
            {
                throw new ArgumentOutOfRangeException($"Index `{index}` is less than zero");
            }
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[1024];
            int length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this chunk and copies it to the <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            int length = 0;
            length += chunk->count.ToString(destination);
            if (chunk->definition != Definition.Default)
            {
                destination[length++] = ' ';
                length += chunk->definition.ToString(chunk->schema, destination.Slice(length));
            }

            return length;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return chunk->definition.GetHashCode();
        }

        /// <summary>
        /// Retrieves the byte offset of the <paramref name="componentType"/>
        /// </summary>
        public readonly int GetComponentOffset(int componentType)
        {
            MemoryAddress.ThrowIfDefault(chunk);

            return chunk->schema.GetComponentOffset(componentType);
        }

        /// <summary>
        /// Retrieves an enumerator for iterating through each component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ComponentEnumerator<T> GetComponents<T>(int componentType) where T : unmanaged
        {
            int componentOffset = chunk->schema.GetComponentOffset(componentType);
            return new(chunk->components, componentOffset);
        }

        /// <summary>
        /// Retrieves a reference to the component of type <paramref name="componentType"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(int index, int componentType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(chunk);
            ThrowIfIndexIsOutOfRange(index);
            ThrowIfComponentTypeIsMissing(componentType);

            int componentOffset = chunk->schema.GetComponentOffset(componentType);
            return ref chunk->components[index].Read<T>(componentOffset);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Chunk chunk && Equals(chunk);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Chunk other)
        {
            return (nint)chunk == (nint)other.chunk;
        }

        /// <inheritdoc/>
        public static bool operator ==(Chunk left, Chunk right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Chunk left, Chunk right)
        {
            return !(left == right);
        }

        /// <summary>
        /// A single row in the chunk, where each column is a component.
        /// </summary>
        public readonly struct Row
        {
            private readonly Schema schema;
            internal readonly MemoryAddress row;

            internal Row(Schema schema, MemoryAddress row)
            {
                this.schema = schema;
                this.row = row;
            }

            /// <summary>
            /// Retrieves the component with the given <paramref name="componentType"/>.
            /// </summary>
            public readonly MemoryAddress GetComponent(int componentType)
            {
                unchecked
                {
                    return new(row.Pointer + schema.schema->componentOffsets[(uint)componentType]);
                }
            }

            /// <summary>
            /// Retrieves the component of type <typeparamref name="T"/>.
            /// </summary>
            public readonly ref T GetComponent<T>(int componentType) where T : unmanaged
            {
                unchecked
                {
                    return ref *(T*)(row.Pointer + schema.schema->componentOffsets[(uint)componentType]);
                }
            }

            /// <summary>
            /// Assigns the component of type <typeparamref name="T"/> to <paramref name="value"/>.
            /// </summary>
            public readonly void SetComponent<T>(int componentType, T value) where T : unmanaged
            {
                unchecked
                {
                    *(T*)(row.Pointer + schema.schema->componentOffsets[(uint)componentType]) = value;
                }
            }

            /// <summary>
            /// Retrieves all bytes for the given <paramref name="componentType"/>.
            /// </summary>
            public readonly Span<byte> GetSpan(int componentType)
            {
                unchecked
                {
                    return new Span<byte>(row.Pointer + schema.schema->componentOffsets[(uint)componentType], schema.schema->sizes[(uint)componentType]);
                }
            }
        }

        /// <inheritdoc/>
        public readonly ref struct Entity<C1> where C1 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
#else
            private readonly void* c1;
            /// <inheritdoc/>
            public readonly ref C1 component1 => ref *(C1*)c1;
#endif

            /// <inheritdoc/>
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


        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2> where C1 : unmanaged where C2 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;

            /// <inheritdoc/>
            public readonly ref C2 component2;
#else
            private readonly void* c1;
            private readonly void* c2;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
#endif

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
#else
            private readonly void* c1;
            private readonly void* c2;
            private readonly void* c3;
            public readonly ref C1 component1 => ref *(C1*)c1;
            public readonly ref C2 component2 => ref *(C2*)c2;
            public readonly ref C3 component3 => ref *(C3*)c3;
#endif
            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
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
            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
            public readonly ref C5 component5;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
            public readonly ref C5 component5;
            /// <inheritdoc/>
            public readonly ref C6 component6;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
            public readonly ref C5 component5;
            /// <inheritdoc/>
            public readonly ref C6 component6;
            /// <inheritdoc/>
            public readonly ref C7 component7;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
            public readonly ref C5 component5;
            /// <inheritdoc/>
            public readonly ref C6 component6;
            /// <inheritdoc/>
            public readonly ref C7 component7;
            /// <inheritdoc/>
            public readonly ref C8 component8;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
            public readonly ref C5 component5;
            /// <inheritdoc/>
            public readonly ref C6 component6;
            /// <inheritdoc/>
            public readonly ref C7 component7;
            /// <inheritdoc/>
            public readonly ref C8 component8;
            /// <inheritdoc/>
            public readonly ref C9 component9;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
            public readonly ref C5 component5;
            /// <inheritdoc/>
            public readonly ref C6 component6;
            /// <inheritdoc/>
            public readonly ref C7 component7;
            /// <inheritdoc/>
            public readonly ref C8 component8;
            /// <inheritdoc/>
            public readonly ref C9 component9;
            /// <inheritdoc/>
            public readonly ref C10 component10;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
            public readonly ref C5 component5;
            /// <inheritdoc/>
            public readonly ref C6 component6;
            /// <inheritdoc/>
            public readonly ref C7 component7;
            /// <inheritdoc/>
            public readonly ref C8 component8;
            /// <inheritdoc/>
            public readonly ref C9 component9;
            /// <inheritdoc/>
            public readonly ref C10 component10;
            /// <inheritdoc/>
            public readonly ref C11 component11;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
            public readonly ref C5 component5;
            /// <inheritdoc/>
            public readonly ref C6 component6;
            /// <inheritdoc/>
            public readonly ref C7 component7;
            /// <inheritdoc/>
            public readonly ref C8 component8;
            /// <inheritdoc/>
            public readonly ref C9 component9;
            /// <inheritdoc/>
            public readonly ref C10 component10;
            /// <inheritdoc/>
            public readonly ref C11 component11;
            /// <inheritdoc/>
            public readonly ref C12 component12;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
            public readonly ref C5 component5;
            /// <inheritdoc/>
            public readonly ref C6 component6;
            /// <inheritdoc/>
            public readonly ref C7 component7;
            /// <inheritdoc/>
            public readonly ref C8 component8;
            /// <inheritdoc/>
            public readonly ref C9 component9;
            /// <inheritdoc/>
            public readonly ref C10 component10;
            /// <inheritdoc/>
            public readonly ref C11 component11;
            /// <inheritdoc/>
            public readonly ref C12 component12;
            /// <inheritdoc/>
            public readonly ref C13 component13;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
            public readonly ref C5 component5;
            /// <inheritdoc/>
            public readonly ref C6 component6;
            /// <inheritdoc/>
            public readonly ref C7 component7;
            /// <inheritdoc/>
            public readonly ref C8 component8;
            /// <inheritdoc/>
            public readonly ref C9 component9;
            /// <inheritdoc/>
            public readonly ref C10 component10;
            /// <inheritdoc/>
            public readonly ref C11 component11;
            /// <inheritdoc/>
            public readonly ref C12 component12;
            /// <inheritdoc/>
            public readonly ref C13 component13;
            /// <inheritdoc/>
            public readonly ref C14 component14;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly ref struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
        {
            /// <summary>
            /// The entity the components belong to.
            /// </summary>
            public readonly uint entity;
#if NET
            /// <inheritdoc/>
            public readonly ref C1 component1;
            /// <inheritdoc/>
            public readonly ref C2 component2;
            /// <inheritdoc/>
            public readonly ref C3 component3;
            /// <inheritdoc/>
            public readonly ref C4 component4;
            /// <inheritdoc/>
            public readonly ref C5 component5;
            /// <inheritdoc/>
            public readonly ref C6 component6;
            /// <inheritdoc/>
            public readonly ref C7 component7;
            /// <inheritdoc/>
            public readonly ref C8 component8;
            /// <inheritdoc/>
            public readonly ref C9 component9;
            /// <inheritdoc/>
            public readonly ref C10 component10;
            /// <inheritdoc/>
            public readonly ref C11 component11;
            /// <inheritdoc/>
            public readonly ref C12 component12;
            /// <inheritdoc/>
            public readonly ref C13 component13;
            /// <inheritdoc/>
            public readonly ref C14 component14;
            /// <inheritdoc/>
            public readonly ref C15 component15;
            /// <inheritdoc/>
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

            /// <inheritdoc/>
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