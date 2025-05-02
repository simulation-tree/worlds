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
            int componentType1 = schema.GetComponentType<T1>();
            BitMask componentTypesMask = new(componentType1);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 2 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2>(this World world, T1 component1, T2 component2) where T1 : unmanaged where T2 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2>(out int componentType1, out int componentType2);
            BitMask componentTypesMask = new(componentType1, componentType2);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 3 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3>(this World world, T1 component1, T2 component2, T3 component3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3>(out int componentType1, out int componentType2, out int componentType3);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 4 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4>(this World world, T1 component1, T2 component2, T3 component3, T4 component4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4>(out int componentType1, out int componentType2, out int componentType3, out int componentType4);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 5 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            Schema schema = world.Schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 6 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5, out int componentType6);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5, componentType6);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            row.SetComponent(componentType6, component6);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 7 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5, out int componentType6, out int componentType7);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5, componentType6, componentType7);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            row.SetComponent(componentType6, component6);
            row.SetComponent(componentType7, component7);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 8 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5, out int componentType6, out int componentType7, out int componentType8);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5, componentType6, componentType7, componentType8);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            row.SetComponent(componentType6, component6);
            row.SetComponent(componentType7, component7);
            row.SetComponent(componentType8, component8);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 9 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5, out int componentType6, out int componentType7, out int componentType8, out int componentType9);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5, componentType6, componentType7, componentType8, componentType9);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            row.SetComponent(componentType6, component6);
            row.SetComponent(componentType7, component7);
            row.SetComponent(componentType8, component8);
            row.SetComponent(componentType9, component9);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 10 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5, out int componentType6, out int componentType7, out int componentType8, out int componentType9, out int componentType10);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5, componentType6, componentType7, componentType8, componentType9, componentType10);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            row.SetComponent(componentType6, component6);
            row.SetComponent(componentType7, component7);
            row.SetComponent(componentType8, component8);
            row.SetComponent(componentType9, component9);
            row.SetComponent(componentType10, component10);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 11 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5, out int componentType6, out int componentType7, out int componentType8, out int componentType9, out int componentType10, out int componentType11);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5, componentType6, componentType7, componentType8, componentType9, componentType10, componentType11);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            row.SetComponent(componentType6, component6);
            row.SetComponent(componentType7, component7);
            row.SetComponent(componentType8, component8);
            row.SetComponent(componentType9, component9);
            row.SetComponent(componentType10, component10);
            row.SetComponent(componentType11, component11);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 12 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5, out int componentType6, out int componentType7, out int componentType8, out int componentType9, out int componentType10, out int componentType11, out int componentType12);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5, componentType6, componentType7, componentType8, componentType9, componentType10, componentType11, componentType12);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            row.SetComponent(componentType6, component6);
            row.SetComponent(componentType7, component7);
            row.SetComponent(componentType8, component8);
            row.SetComponent(componentType9, component9);
            row.SetComponent(componentType10, component10);
            row.SetComponent(componentType11, component11);
            row.SetComponent(componentType12, component12);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 13 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5, out int componentType6, out int componentType7, out int componentType8, out int componentType9, out int componentType10, out int componentType11, out int componentType12, out int componentType13);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5, componentType6, componentType7, componentType8, componentType9, componentType10, componentType11, componentType12, componentType13);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            row.SetComponent(componentType6, component6);
            row.SetComponent(componentType7, component7);
            row.SetComponent(componentType8, component8);
            row.SetComponent(componentType9, component9);
            row.SetComponent(componentType10, component10);
            row.SetComponent(componentType11, component11);
            row.SetComponent(componentType12, component12);
            row.SetComponent(componentType13, component13);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 14 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5, out int componentType6, out int componentType7, out int componentType8, out int componentType9, out int componentType10, out int componentType11, out int componentType12, out int componentType13, out int componentType14);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5, componentType6, componentType7, componentType8, componentType9, componentType10, componentType11, componentType12, componentType13, componentType14);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            row.SetComponent(componentType6, component6);
            row.SetComponent(componentType7, component7);
            row.SetComponent(componentType8, component8);
            row.SetComponent(componentType9, component9);
            row.SetComponent(componentType10, component10);
            row.SetComponent(componentType11, component11);
            row.SetComponent(componentType12, component12);
            row.SetComponent(componentType13, component13);
            row.SetComponent(componentType14, component14);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 15 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5, out int componentType6, out int componentType7, out int componentType8, out int componentType9, out int componentType10, out int componentType11, out int componentType12, out int componentType13, out int componentType14, out int componentType15);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5, componentType6, componentType7, componentType8, componentType9, componentType10, componentType11, componentType12, componentType13, componentType14, componentType15);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            row.SetComponent(componentType6, component6);
            row.SetComponent(componentType7, component7);
            row.SetComponent(componentType8, component8);
            row.SetComponent(componentType9, component9);
            row.SetComponent(componentType10, component10);
            row.SetComponent(componentType11, component11);
            row.SetComponent(componentType12, component12);
            row.SetComponent(componentType13, component13);
            row.SetComponent(componentType14, component14);
            row.SetComponent(componentType15, component15);
            return entity;
        }

        /// <summary>
        /// Creates an entity with 16 components already present.
        /// </summary>
        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15, T16 component16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            Schema schema = world.world->schema;
            schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(out int componentType1, out int componentType2, out int componentType3, out int componentType4, out int componentType5, out int componentType6, out int componentType7, out int componentType8, out int componentType9, out int componentType10, out int componentType11, out int componentType12, out int componentType13, out int componentType14, out int componentType15, out int componentType16);
            BitMask componentTypesMask = new(componentType1, componentType2, componentType3, componentType4, componentType5, componentType6, componentType7, componentType8, componentType9, componentType10, componentType11, componentType12, componentType13, componentType14, componentType15, componentType16);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk.Row row);
            row.SetComponent(componentType1, component1);
            row.SetComponent(componentType2, component2);
            row.SetComponent(componentType3, component3);
            row.SetComponent(componentType4, component4);
            row.SetComponent(componentType5, component5);
            row.SetComponent(componentType6, component6);
            row.SetComponent(componentType7, component7);
            row.SetComponent(componentType8, component8);
            row.SetComponent(componentType9, component9);
            row.SetComponent(componentType10, component10);
            row.SetComponent(componentType11, component11);
            row.SetComponent(componentType12, component12);
            row.SetComponent(componentType13, component13);
            row.SetComponent(componentType14, component14);
            row.SetComponent(componentType15, component15);
            row.SetComponent(componentType16, component16);
            return entity;
        }
    }
}