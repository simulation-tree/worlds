using System;

namespace Worlds
{
    /// <summary>
    /// Extension methods for <see cref="World"/> to create entities with components
    /// already present.
    /// </summary>
    public unsafe static class CreateExtensions
    {
        /// <summary>
        /// Creates an entity with 1 component already present.
        /// </summary>
        public static uint CreateEntity<T1>(this World world, T1 component1) where T1 : unmanaged
        {
            Schema schema = world.world->schema;
            int c1 = schema.GetComponentType<T1>();
            uint entity = world.CreateEntity(new BitMask(c1), out Chunk.Row newRow);
            unchecked
            {
                *(T1*)(newRow.row.Pointer + schema.schema->componentOffsets[(uint)c1]) = component1;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 1 component already present.
        /// </summary>
        public static uint CreateEntity<T1>(this World world, int c1, T1 component1) where T1 : unmanaged
        {
            Schema schema = world.world->schema;
            uint entity = world.CreateEntity(new BitMask(c1), out Chunk.Row newRow);
            unchecked
            {
                *(T1*)(newRow.row.Pointer + schema.schema->componentOffsets[(uint)c1]) = component1;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 2 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2>(this World world, T1 component1, T2 component2) where T1 : unmanaged where T2 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2>(out int c1, out int c2);
            uint entity = world.CreateEntity(new BitMask(c1, c2), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 2 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2>(this World world, int c1, int c2, T1 component1, T2 component2) where T1 : unmanaged where T2 : unmanaged
        {
            Schema schema = world.world->schema;
            uint entity = world.CreateEntity(new BitMask(c1, c2), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 3 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3>(this World world, T1 component1, T2 component2, T3 component3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3>(out int c1, out int c2, out int c3);
            uint entity = world.CreateEntity(new BitMask(c1, c2, c3), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 3 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3>(this World world, int c1, int c2, int c3, T1 component1, T2 component2, T3 component3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            Schema schema = world.world->schema;
            uint entity = world.CreateEntity(new BitMask(c1, c2, c3), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 4 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4>(this World world, T1 component1, T2 component2, T3 component3, T4 component4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4>(out int c1, out int c2, out int c3, out int c4);
            uint entity = world.CreateEntity(new BitMask(c1, c2, c3, c4), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 4 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4>(this World world, int c1, int c2, int c3, int c4, T1 component1, T2 component2, T3 component3, T4 component4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            Schema schema = world.world->schema;
            uint entity = world.CreateEntity(new BitMask(c1, c2, c3, c4), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 5 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5>(out int c1, out int c2, out int c3, out int c4, out int c5);
            uint entity = world.CreateEntity(new BitMask(c1, c2, c3, c4, c5), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 5 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5>(this World world, int c1, int c2, int c3, int c4, int c5, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            Schema schema = world.world->schema;
            uint entity = world.CreateEntity(new BitMask(c1, c2, c3, c4, c5), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 6 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6);
            uint entity = world.CreateEntity(new BitMask(c1, c2, c3, c4, c5, c6), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 6 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6>(this World world, int c1, int c2, int c3, int c4, int c5, int c6, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            Schema schema = world.world->schema;
            uint entity = world.CreateEntity(new BitMask(c1, c2, c3, c4, c5, c6), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 7 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7);
            uint entity = world.CreateEntity(new BitMask(c1, c2, c3, c4, c5, c6, c7), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 7 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7>(this World world, int c1, int c2, int c3, int c4, int c5, int c6, int c7, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            Schema schema = world.world->schema;
            uint entity = world.CreateEntity(new BitMask(c1, c2, c3, c4, c5, c6, c7), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 8 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8);
            BitMask componentTypesMask = new(c1, c2, c3, c4, c5, c6, c7, c8);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
                *(T8*)(pointer + componentOffsets[c8]) = component8;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 8 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8>(this World world, int c1, int c2, int c3, int c4, int c5, int c6, int c7, int c8, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            Schema schema = world.world->schema;
            uint entity = world.CreateEntity(new BitMask(c1, c2, c3, c4, c5, c6, c7, c8), out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
                *(T8*)(pointer + componentOffsets[c8]) = component8;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 9 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9);
            BitMask componentTypesMask = new(c1, c2, c3, c4, c5, c6, c7, c8, c9);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
                *(T8*)(pointer + componentOffsets[c8]) = component8;
                *(T9*)(pointer + componentOffsets[c9]) = component9;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 10 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10);
            BitMask componentTypesMask = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
                *(T8*)(pointer + componentOffsets[c8]) = component8;
                *(T9*)(pointer + componentOffsets[c9]) = component9;
                *(T10*)(pointer + componentOffsets[c10]) = component10;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 11 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11);
            BitMask componentTypesMask = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
                *(T8*)(pointer + componentOffsets[c8]) = component8;
                *(T9*)(pointer + componentOffsets[c9]) = component9;
                *(T10*)(pointer + componentOffsets[c10]) = component10;
                *(T11*)(pointer + componentOffsets[c11]) = component11;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 12 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12);
            BitMask componentTypesMask = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
                *(T8*)(pointer + componentOffsets[c8]) = component8;
                *(T9*)(pointer + componentOffsets[c9]) = component9;
                *(T10*)(pointer + componentOffsets[c10]) = component10;
                *(T11*)(pointer + componentOffsets[c11]) = component11;
                *(T12*)(pointer + componentOffsets[c12]) = component12;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 13 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13);
            BitMask componentTypesMask = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
                *(T8*)(pointer + componentOffsets[c8]) = component8;
                *(T9*)(pointer + componentOffsets[c9]) = component9;
                *(T10*)(pointer + componentOffsets[c10]) = component10;
                *(T11*)(pointer + componentOffsets[c11]) = component11;
                *(T12*)(pointer + componentOffsets[c12]) = component12;
                *(T13*)(pointer + componentOffsets[c13]) = component13;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 14 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13, out int c14);
            BitMask componentTypesMask = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
                *(T8*)(pointer + componentOffsets[c8]) = component8;
                *(T9*)(pointer + componentOffsets[c9]) = component9;
                *(T10*)(pointer + componentOffsets[c10]) = component10;
                *(T11*)(pointer + componentOffsets[c11]) = component11;
                *(T12*)(pointer + componentOffsets[c12]) = component12;
                *(T13*)(pointer + componentOffsets[c13]) = component13;
                *(T14*)(pointer + componentOffsets[c14]) = component14;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 15 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13, out int c14, out int c15);
            BitMask componentTypesMask = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
                *(T8*)(pointer + componentOffsets[c8]) = component8;
                *(T9*)(pointer + componentOffsets[c9]) = component9;
                *(T10*)(pointer + componentOffsets[c10]) = component10;
                *(T11*)(pointer + componentOffsets[c11]) = component11;
                *(T12*)(pointer + componentOffsets[c12]) = component12;
                *(T13*)(pointer + componentOffsets[c13]) = component13;
                *(T14*)(pointer + componentOffsets[c14]) = component14;
                *(T15*)(pointer + componentOffsets[c15]) = component15;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity with 16 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15, T16 component16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13, out int c14, out int c15, out int c16);
            BitMask componentTypesMask = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row newRow);
            Span<int> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
            unchecked
            {
                byte* pointer = newRow.row.Pointer;
                *(T1*)(pointer + componentOffsets[c1]) = component1;
                *(T2*)(pointer + componentOffsets[c2]) = component2;
                *(T3*)(pointer + componentOffsets[c3]) = component3;
                *(T4*)(pointer + componentOffsets[c4]) = component4;
                *(T5*)(pointer + componentOffsets[c5]) = component5;
                *(T6*)(pointer + componentOffsets[c6]) = component6;
                *(T7*)(pointer + componentOffsets[c7]) = component7;
                *(T8*)(pointer + componentOffsets[c8]) = component8;
                *(T9*)(pointer + componentOffsets[c9]) = component9;
                *(T10*)(pointer + componentOffsets[c10]) = component10;
                *(T11*)(pointer + componentOffsets[c11]) = component11;
                *(T12*)(pointer + componentOffsets[c12]) = component12;
                *(T13*)(pointer + componentOffsets[c13]) = component13;
                *(T14*)(pointer + componentOffsets[c14]) = component14;
                *(T15*)(pointer + componentOffsets[c15]) = component15;
                *(T16*)(pointer + componentOffsets[c16]) = component16;
            }

            return entity;
        }
    }
}