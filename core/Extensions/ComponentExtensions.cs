namespace Worlds
{
    /// <summary>
    /// Extension methods for <see cref="World"/> to add, get and remove components.
    /// </summary>
    public unsafe static class ComponentExtensions
    {
        /// <summary>
        /// Adds two components.
        /// </summary>
        public static void AddComponentTypes<T1, T2>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2>());
        }

        /// <summary>
        /// Adds two components.
        /// </summary>
        public static void AddComponents<T1, T2>(this World world, uint entity, T1 component1, T2 component2) where T1 : unmanaged where T2 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2>(out int c1, out int c2);
            BitMask componentTypes = new(c1, c2);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
        }

        /// <summary>
        /// Adds three components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3>());
        }

        /// <summary>
        /// Adds three components.
        /// </summary>
        public static void AddComponents<T1, T2, T3>(this World world, uint entity, T1 component1, T2 component2, T3 component3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3>(out int c1, out int c2, out int c3);
            BitMask componentTypes = new(c1, c2, c3);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
        }

        /// <summary>
        /// Adds four components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4>());
        }

        /// <summary>
        /// Adds four components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4>(out int c1, out int c2, out int c3, out int c4);
            BitMask componentTypes = new(c1, c2, c3, c4);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
        }

        /// <summary>
        /// Adds five components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5>());
        }

        /// <summary>
        /// Adds five components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5>(out int c1, out int c2, out int c3, out int c4, out int c5);
            BitMask componentTypes = new(c1, c2, c3, c4, c5);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
        }

        /// <summary>
        /// Adds six components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5, T6>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6>());
        }

        /// <summary>
        /// Adds six components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5, T6>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6);
            BitMask componentTypes = new(c1, c2, c3, c4, c5, c6);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
            newRow.SetComponent(c6, component6);
        }

        /// <summary>
        /// Adds seven components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7>());
        }

        /// <summary>
        /// Adds seven components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5, T6, T7>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7);
            BitMask componentTypes = new(c1, c2, c3, c4, c5, c6, c7);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
            newRow.SetComponent(c6, component6);
            newRow.SetComponent(c7, component7);
        }

        /// <summary>
        /// Adds eight components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>());
        }

        /// <summary>
        /// Adds eight components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8);
            BitMask componentTypes = new(c1, c2, c3, c4, c5, c6, c7, c8);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
            newRow.SetComponent(c6, component6);
            newRow.SetComponent(c7, component7);
            newRow.SetComponent(c8, component8);
        }

        /// <summary>
        /// Adds nine components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>());
        }

        /// <summary>
        /// Adds nine components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9);
            BitMask componentTypes = new(c1, c2, c3, c4, c5, c6, c7, c8, c9);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
            newRow.SetComponent(c6, component6);
            newRow.SetComponent(c7, component7);
            newRow.SetComponent(c8, component8);
            newRow.SetComponent(c9, component9);
        }

        /// <summary>
        /// Adds ten components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>());
        }

        /// <summary>
        /// Adds ten components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10);
            BitMask componentTypes = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
            newRow.SetComponent(c6, component6);
            newRow.SetComponent(c7, component7);
            newRow.SetComponent(c8, component8);
            newRow.SetComponent(c9, component9);
            newRow.SetComponent(c10, component10);
        }

        /// <summary>
        /// Adds eleven components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>());
        }

        /// <summary>
        /// Adds eleven components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11);
            BitMask componentTypes = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
            newRow.SetComponent(c6, component6);
            newRow.SetComponent(c7, component7);
            newRow.SetComponent(c8, component8);
            newRow.SetComponent(c9, component9);
            newRow.SetComponent(c10, component10);
            newRow.SetComponent(c11, component11);
        }

        /// <summary>
        /// Adds twelve components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>());
        }

        /// <summary>
        /// Adds twelve components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12);
            BitMask componentTypes = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
            newRow.SetComponent(c6, component6);
            newRow.SetComponent(c7, component7);
            newRow.SetComponent(c8, component8);
            newRow.SetComponent(c9, component9);
            newRow.SetComponent(c10, component10);
            newRow.SetComponent(c11, component11);
            newRow.SetComponent(c12, component12);
        }

        /// <summary>
        /// Adds thirteen components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>());
        }

        /// <summary>
        /// Adds thirteen components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13);
            BitMask componentTypes = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
            newRow.SetComponent(c6, component6);
            newRow.SetComponent(c7, component7);
            newRow.SetComponent(c8, component8);
            newRow.SetComponent(c9, component9);
            newRow.SetComponent(c10, component10);
            newRow.SetComponent(c11, component11);
            newRow.SetComponent(c12, component12);
            newRow.SetComponent(c13, component13);
        }

        /// <summary>
        /// Adds fourteen components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>());
        }

        /// <summary>
        /// Adds fourteen components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13, out int c14);
            BitMask componentTypes = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
            newRow.SetComponent(c6, component6);
            newRow.SetComponent(c7, component7);
            newRow.SetComponent(c8, component8);
            newRow.SetComponent(c9, component9);
            newRow.SetComponent(c10, component10);
            newRow.SetComponent(c11, component11);
            newRow.SetComponent(c12, component12);
            newRow.SetComponent(c13, component13);
            newRow.SetComponent(c14, component14);
        }

        /// <summary>
        /// Adds fifthteen components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>());
        }

        /// <summary>
        /// Adds fifthteen components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13, out int c14, out int c15);
            BitMask componentTypes = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
            newRow.SetComponent(c6, component6);
            newRow.SetComponent(c7, component7);
            newRow.SetComponent(c8, component8);
            newRow.SetComponent(c9, component9);
            newRow.SetComponent(c10, component10);
            newRow.SetComponent(c11, component11);
            newRow.SetComponent(c12, component12);
            newRow.SetComponent(c13, component13);
            newRow.SetComponent(c14, component14);
            newRow.SetComponent(c15, component15);
        }

        /// <summary>
        /// Adds sixteen components.
        /// </summary>
        public static void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            world.AddComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>());
        }

        /// <summary>
        /// Adds sixteen components.
        /// </summary>
        public static void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this World world, uint entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15, T16 component16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13, out int c14, out int c15, out int c16);
            BitMask componentTypes = new(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c15);
            world.AddComponentTypes(entity, componentTypes, out Chunk.Row newRow);
            newRow.SetComponent(c1, component1);
            newRow.SetComponent(c2, component2);
            newRow.SetComponent(c3, component3);
            newRow.SetComponent(c4, component4);
            newRow.SetComponent(c5, component5);
            newRow.SetComponent(c6, component6);
            newRow.SetComponent(c7, component7);
            newRow.SetComponent(c8, component8);
            newRow.SetComponent(c9, component9);
            newRow.SetComponent(c10, component10);
            newRow.SetComponent(c11, component11);
            newRow.SetComponent(c12, component12);
            newRow.SetComponent(c13, component13);
            newRow.SetComponent(c14, component14);
            newRow.SetComponent(c15, component15);
            newRow.SetComponent(c16, component16);
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/> and <typeparamref name="T2"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, and <typeparamref name="T3"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, and <typeparamref name="T4"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, and <typeparamref name="T5"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, and <typeparamref name="T6"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5, T6>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, and <typeparamref name="T7"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, and <typeparamref name="T8"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, and <typeparamref name="T9"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, and <typeparamref name="T10"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>, and <typeparamref name="T11"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>, <typeparamref name="T11"/>, and <typeparamref name="T12"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>, <typeparamref name="T11"/>, <typeparamref name="T12"/>, and <typeparamref name="T13"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>, <typeparamref name="T11"/>, <typeparamref name="T12"/>, <typeparamref name="T13"/>, and <typeparamref name="T14"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>, <typeparamref name="T11"/>, <typeparamref name="T12"/>, <typeparamref name="T13"/>, <typeparamref name="T14"/>, and <typeparamref name="T15"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>());
        }

        /// <summary>
        /// Removes components of types <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>, <typeparamref name="T11"/>, <typeparamref name="T12"/>, <typeparamref name="T13"/>, <typeparamref name="T14"/>, <typeparamref name="T15"/>, and <typeparamref name="T16"/> from the entity.
        /// </summary>
        public static void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this World world, uint entity) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            world.RemoveComponentTypes(entity, world.world->schema.GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>());
        }
    }
}