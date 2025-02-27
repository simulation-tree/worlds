namespace Worlds
{
    public static class CreateExtensions
    {
        public static uint CreateEntity<T1>(this World world, T1 component1) where T1 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            return entity;
        }

        public static uint CreateEntity<T1, T2>(this World world, T1 component1, T2 component2) where T1 : unmanaged where T2 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3>(this World world, T1 component1, T2 component2, T3 component3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4>(this World world, T1 component1, T2 component2, T3 component3, T4 component4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            uint componentType6 = schema.GetComponentTypeIndex<T6>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            chunk.SetComponent(index, componentType6, component6);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            uint componentType6 = schema.GetComponentTypeIndex<T6>();
            uint componentType7 = schema.GetComponentTypeIndex<T7>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            chunk.SetComponent(index, componentType6, component6);
            chunk.SetComponent(index, componentType7, component7);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            uint componentType6 = schema.GetComponentTypeIndex<T6>();
            uint componentType7 = schema.GetComponentTypeIndex<T7>();
            uint componentType8 = schema.GetComponentTypeIndex<T8>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            componentTypesMask.Set(componentType8);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            chunk.SetComponent(index, componentType6, component6);
            chunk.SetComponent(index, componentType7, component7);
            chunk.SetComponent(index, componentType8, component8);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            uint componentType6 = schema.GetComponentTypeIndex<T6>();
            uint componentType7 = schema.GetComponentTypeIndex<T7>();
            uint componentType8 = schema.GetComponentTypeIndex<T8>();
            uint componentType9 = schema.GetComponentTypeIndex<T9>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            componentTypesMask.Set(componentType8);
            componentTypesMask.Set(componentType9);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            chunk.SetComponent(index, componentType6, component6);
            chunk.SetComponent(index, componentType7, component7);
            chunk.SetComponent(index, componentType8, component8);
            chunk.SetComponent(index, componentType9, component9);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            uint componentType6 = schema.GetComponentTypeIndex<T6>();
            uint componentType7 = schema.GetComponentTypeIndex<T7>();
            uint componentType8 = schema.GetComponentTypeIndex<T8>();
            uint componentType9 = schema.GetComponentTypeIndex<T9>();
            uint componentType10 = schema.GetComponentTypeIndex<T10>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            componentTypesMask.Set(componentType8);
            componentTypesMask.Set(componentType9);
            componentTypesMask.Set(componentType10);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            chunk.SetComponent(index, componentType6, component6);
            chunk.SetComponent(index, componentType7, component7);
            chunk.SetComponent(index, componentType8, component8);
            chunk.SetComponent(index, componentType9, component9);
            chunk.SetComponent(index, componentType10, component10);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            uint componentType6 = schema.GetComponentTypeIndex<T6>();
            uint componentType7 = schema.GetComponentTypeIndex<T7>();
            uint componentType8 = schema.GetComponentTypeIndex<T8>();
            uint componentType9 = schema.GetComponentTypeIndex<T9>();
            uint componentType10 = schema.GetComponentTypeIndex<T10>();
            uint componentType11 = schema.GetComponentTypeIndex<T11>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            componentTypesMask.Set(componentType8);
            componentTypesMask.Set(componentType9);
            componentTypesMask.Set(componentType10);
            componentTypesMask.Set(componentType11);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            chunk.SetComponent(index, componentType6, component6);
            chunk.SetComponent(index, componentType7, component7);
            chunk.SetComponent(index, componentType8, component8);
            chunk.SetComponent(index, componentType9, component9);
            chunk.SetComponent(index, componentType10, component10);
            chunk.SetComponent(index, componentType11, component11);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            uint componentType6 = schema.GetComponentTypeIndex<T6>();
            uint componentType7 = schema.GetComponentTypeIndex<T7>();
            uint componentType8 = schema.GetComponentTypeIndex<T8>();
            uint componentType9 = schema.GetComponentTypeIndex<T9>();
            uint componentType10 = schema.GetComponentTypeIndex<T10>();
            uint componentType11 = schema.GetComponentTypeIndex<T11>();
            uint componentType12 = schema.GetComponentTypeIndex<T12>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            componentTypesMask.Set(componentType8);
            componentTypesMask.Set(componentType9);
            componentTypesMask.Set(componentType10);
            componentTypesMask.Set(componentType11);
            componentTypesMask.Set(componentType12);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            chunk.SetComponent(index, componentType6, component6);
            chunk.SetComponent(index, componentType7, component7);
            chunk.SetComponent(index, componentType8, component8);
            chunk.SetComponent(index, componentType9, component9);
            chunk.SetComponent(index, componentType10, component10);
            chunk.SetComponent(index, componentType11, component11);
            chunk.SetComponent(index, componentType12, component12);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            uint componentType6 = schema.GetComponentTypeIndex<T6>();
            uint componentType7 = schema.GetComponentTypeIndex<T7>();
            uint componentType8 = schema.GetComponentTypeIndex<T8>();
            uint componentType9 = schema.GetComponentTypeIndex<T9>();
            uint componentType10 = schema.GetComponentTypeIndex<T10>();
            uint componentType11 = schema.GetComponentTypeIndex<T11>();
            uint componentType12 = schema.GetComponentTypeIndex<T12>();
            uint componentType13 = schema.GetComponentTypeIndex<T13>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            componentTypesMask.Set(componentType8);
            componentTypesMask.Set(componentType9);
            componentTypesMask.Set(componentType10);
            componentTypesMask.Set(componentType11);
            componentTypesMask.Set(componentType12);
            componentTypesMask.Set(componentType13);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            chunk.SetComponent(index, componentType6, component6);
            chunk.SetComponent(index, componentType7, component7);
            chunk.SetComponent(index, componentType8, component8);
            chunk.SetComponent(index, componentType9, component9);
            chunk.SetComponent(index, componentType10, component10);
            chunk.SetComponent(index, componentType11, component11);
            chunk.SetComponent(index, componentType12, component12);
            chunk.SetComponent(index, componentType13, component13);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            uint componentType6 = schema.GetComponentTypeIndex<T6>();
            uint componentType7 = schema.GetComponentTypeIndex<T7>();
            uint componentType8 = schema.GetComponentTypeIndex<T8>();
            uint componentType9 = schema.GetComponentTypeIndex<T9>();
            uint componentType10 = schema.GetComponentTypeIndex<T10>();
            uint componentType11 = schema.GetComponentTypeIndex<T11>();
            uint componentType12 = schema.GetComponentTypeIndex<T12>();
            uint componentType13 = schema.GetComponentTypeIndex<T13>();
            uint componentType14 = schema.GetComponentTypeIndex<T14>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            componentTypesMask.Set(componentType8);
            componentTypesMask.Set(componentType9);
            componentTypesMask.Set(componentType10);
            componentTypesMask.Set(componentType11);
            componentTypesMask.Set(componentType12);
            componentTypesMask.Set(componentType13);
            componentTypesMask.Set(componentType14);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            chunk.SetComponent(index, componentType6, component6);
            chunk.SetComponent(index, componentType7, component7);
            chunk.SetComponent(index, componentType8, component8);
            chunk.SetComponent(index, componentType9, component9);
            chunk.SetComponent(index, componentType10, component10);
            chunk.SetComponent(index, componentType11, component11);
            chunk.SetComponent(index, componentType12, component12);
            chunk.SetComponent(index, componentType13, component13);
            chunk.SetComponent(index, componentType14, component14);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            uint componentType6 = schema.GetComponentTypeIndex<T6>();
            uint componentType7 = schema.GetComponentTypeIndex<T7>();
            uint componentType8 = schema.GetComponentTypeIndex<T8>();
            uint componentType9 = schema.GetComponentTypeIndex<T9>();
            uint componentType10 = schema.GetComponentTypeIndex<T10>();
            uint componentType11 = schema.GetComponentTypeIndex<T11>();
            uint componentType12 = schema.GetComponentTypeIndex<T12>();
            uint componentType13 = schema.GetComponentTypeIndex<T13>();
            uint componentType14 = schema.GetComponentTypeIndex<T14>();
            uint componentType15 = schema.GetComponentTypeIndex<T15>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            componentTypesMask.Set(componentType8);
            componentTypesMask.Set(componentType9);
            componentTypesMask.Set(componentType10);
            componentTypesMask.Set(componentType11);
            componentTypesMask.Set(componentType12);
            componentTypesMask.Set(componentType13);
            componentTypesMask.Set(componentType14);
            componentTypesMask.Set(componentType15);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            chunk.SetComponent(index, componentType6, component6);
            chunk.SetComponent(index, componentType7, component7);
            chunk.SetComponent(index, componentType8, component8);
            chunk.SetComponent(index, componentType9, component9);
            chunk.SetComponent(index, componentType10, component10);
            chunk.SetComponent(index, componentType11, component11);
            chunk.SetComponent(index, componentType12, component12);
            chunk.SetComponent(index, componentType13, component13);
            chunk.SetComponent(index, componentType14, component14);
            chunk.SetComponent(index, componentType15, component15);
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15, T16 component16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            Schema schema = world.Schema;
            uint componentType1 = schema.GetComponentTypeIndex<T1>();
            uint componentType2 = schema.GetComponentTypeIndex<T2>();
            uint componentType3 = schema.GetComponentTypeIndex<T3>();
            uint componentType4 = schema.GetComponentTypeIndex<T4>();
            uint componentType5 = schema.GetComponentTypeIndex<T5>();
            uint componentType6 = schema.GetComponentTypeIndex<T6>();
            uint componentType7 = schema.GetComponentTypeIndex<T7>();
            uint componentType8 = schema.GetComponentTypeIndex<T8>();
            uint componentType9 = schema.GetComponentTypeIndex<T9>();
            uint componentType10 = schema.GetComponentTypeIndex<T10>();
            uint componentType11 = schema.GetComponentTypeIndex<T11>();
            uint componentType12 = schema.GetComponentTypeIndex<T12>();
            uint componentType13 = schema.GetComponentTypeIndex<T13>();
            uint componentType14 = schema.GetComponentTypeIndex<T14>();
            uint componentType15 = schema.GetComponentTypeIndex<T15>();
            uint componentType16 = schema.GetComponentTypeIndex<T16>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            componentTypesMask.Set(componentType8);
            componentTypesMask.Set(componentType9);
            componentTypesMask.Set(componentType10);
            componentTypesMask.Set(componentType11);
            componentTypesMask.Set(componentType12);
            componentTypesMask.Set(componentType13);
            componentTypesMask.Set(componentType14);
            componentTypesMask.Set(componentType15);
            componentTypesMask.Set(componentType16);
            uint entity = world.CreateEntity(componentTypesMask, out Chunk chunk, out uint index);
            chunk.SetComponent(index, componentType1, component1);
            chunk.SetComponent(index, componentType2, component2);
            chunk.SetComponent(index, componentType3, component3);
            chunk.SetComponent(index, componentType4, component4);
            chunk.SetComponent(index, componentType5, component5);
            chunk.SetComponent(index, componentType6, component6);
            chunk.SetComponent(index, componentType7, component7);
            chunk.SetComponent(index, componentType8, component8);
            chunk.SetComponent(index, componentType9, component9);
            chunk.SetComponent(index, componentType10, component10);
            chunk.SetComponent(index, componentType11, component11);
            chunk.SetComponent(index, componentType12, component12);
            chunk.SetComponent(index, componentType13, component13);
            chunk.SetComponent(index, componentType14, component14);
            chunk.SetComponent(index, componentType15, component15);
            chunk.SetComponent(index, componentType16, component16);
            return entity;
        }
    }
}