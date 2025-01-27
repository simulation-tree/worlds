﻿namespace Worlds
{
    public static class CreateExtensions
    {
        public static uint CreateEntity<T1>(this World world, T1 component1) where T1 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            return entity;
        }

        public static uint CreateEntity<T1, T2>(this World world, T1 component1, T2 component2) where T1 : unmanaged where T2 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3>(this World world, T1 component1, T2 component2, T3 component3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4>(this World world, T1 component1, T2 component2, T3 component3, T4 component4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            ComponentType componentType6 = schema.GetComponent<T6>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            chunk.GetComponent<T6>(index, componentType6) = component6;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            ComponentType componentType6 = schema.GetComponent<T6>();
            ComponentType componentType7 = schema.GetComponent<T7>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            chunk.GetComponent<T6>(index, componentType6) = component6;
            chunk.GetComponent<T7>(index, componentType7) = component7;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            ComponentType componentType6 = schema.GetComponent<T6>();
            ComponentType componentType7 = schema.GetComponent<T7>();
            ComponentType componentType8 = schema.GetComponent<T8>();
            BitMask componentTypesMask = default;
            componentTypesMask.Set(componentType1);
            componentTypesMask.Set(componentType2);
            componentTypesMask.Set(componentType3);
            componentTypesMask.Set(componentType4);
            componentTypesMask.Set(componentType5);
            componentTypesMask.Set(componentType6);
            componentTypesMask.Set(componentType7);
            componentTypesMask.Set(componentType8);
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            chunk.GetComponent<T6>(index, componentType6) = component6;
            chunk.GetComponent<T7>(index, componentType7) = component7;
            chunk.GetComponent<T8>(index, componentType8) = component8;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            ComponentType componentType6 = schema.GetComponent<T6>();
            ComponentType componentType7 = schema.GetComponent<T7>();
            ComponentType componentType8 = schema.GetComponent<T8>();
            ComponentType componentType9 = schema.GetComponent<T9>();
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
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            chunk.GetComponent<T6>(index, componentType6) = component6;
            chunk.GetComponent<T7>(index, componentType7) = component7;
            chunk.GetComponent<T8>(index, componentType8) = component8;
            chunk.GetComponent<T9>(index, componentType9) = component9;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            ComponentType componentType6 = schema.GetComponent<T6>();
            ComponentType componentType7 = schema.GetComponent<T7>();
            ComponentType componentType8 = schema.GetComponent<T8>();
            ComponentType componentType9 = schema.GetComponent<T9>();
            ComponentType componentType10 = schema.GetComponent<T10>();
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
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            chunk.GetComponent<T6>(index, componentType6) = component6;
            chunk.GetComponent<T7>(index, componentType7) = component7;
            chunk.GetComponent<T8>(index, componentType8) = component8;
            chunk.GetComponent<T9>(index, componentType9) = component9;
            chunk.GetComponent<T10>(index, componentType10) = component10;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            ComponentType componentType6 = schema.GetComponent<T6>();
            ComponentType componentType7 = schema.GetComponent<T7>();
            ComponentType componentType8 = schema.GetComponent<T8>();
            ComponentType componentType9 = schema.GetComponent<T9>();
            ComponentType componentType10 = schema.GetComponent<T10>();
            ComponentType componentType11 = schema.GetComponent<T11>();
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
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            chunk.GetComponent<T6>(index, componentType6) = component6;
            chunk.GetComponent<T7>(index, componentType7) = component7;
            chunk.GetComponent<T8>(index, componentType8) = component8;
            chunk.GetComponent<T9>(index, componentType9) = component9;
            chunk.GetComponent<T10>(index, componentType10) = component10;
            chunk.GetComponent<T11>(index, componentType11) = component11;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            ComponentType componentType6 = schema.GetComponent<T6>();
            ComponentType componentType7 = schema.GetComponent<T7>();
            ComponentType componentType8 = schema.GetComponent<T8>();
            ComponentType componentType9 = schema.GetComponent<T9>();
            ComponentType componentType10 = schema.GetComponent<T10>();
            ComponentType componentType11 = schema.GetComponent<T11>();
            ComponentType componentType12 = schema.GetComponent<T12>();
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
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            chunk.GetComponent<T6>(index, componentType6) = component6;
            chunk.GetComponent<T7>(index, componentType7) = component7;
            chunk.GetComponent<T8>(index, componentType8) = component8;
            chunk.GetComponent<T9>(index, componentType9) = component9;
            chunk.GetComponent<T10>(index, componentType10) = component10;
            chunk.GetComponent<T11>(index, componentType11) = component11;
            chunk.GetComponent<T12>(index, componentType12) = component12;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            ComponentType componentType6 = schema.GetComponent<T6>();
            ComponentType componentType7 = schema.GetComponent<T7>();
            ComponentType componentType8 = schema.GetComponent<T8>();
            ComponentType componentType9 = schema.GetComponent<T9>();
            ComponentType componentType10 = schema.GetComponent<T10>();
            ComponentType componentType11 = schema.GetComponent<T11>();
            ComponentType componentType12 = schema.GetComponent<T12>();
            ComponentType componentType13 = schema.GetComponent<T13>();
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
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            chunk.GetComponent<T6>(index, componentType6) = component6;
            chunk.GetComponent<T7>(index, componentType7) = component7;
            chunk.GetComponent<T8>(index, componentType8) = component8;
            chunk.GetComponent<T9>(index, componentType9) = component9;
            chunk.GetComponent<T10>(index, componentType10) = component10;
            chunk.GetComponent<T11>(index, componentType11) = component11;
            chunk.GetComponent<T12>(index, componentType12) = component12;
            chunk.GetComponent<T13>(index, componentType13) = component13;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            ComponentType componentType6 = schema.GetComponent<T6>();
            ComponentType componentType7 = schema.GetComponent<T7>();
            ComponentType componentType8 = schema.GetComponent<T8>();
            ComponentType componentType9 = schema.GetComponent<T9>();
            ComponentType componentType10 = schema.GetComponent<T10>();
            ComponentType componentType11 = schema.GetComponent<T11>();
            ComponentType componentType12 = schema.GetComponent<T12>();
            ComponentType componentType13 = schema.GetComponent<T13>();
            ComponentType componentType14 = schema.GetComponent<T14>();
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
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            chunk.GetComponent<T6>(index, componentType6) = component6;
            chunk.GetComponent<T7>(index, componentType7) = component7;
            chunk.GetComponent<T8>(index, componentType8) = component8;
            chunk.GetComponent<T9>(index, componentType9) = component9;
            chunk.GetComponent<T10>(index, componentType10) = component10;
            chunk.GetComponent<T11>(index, componentType11) = component11;
            chunk.GetComponent<T12>(index, componentType12) = component12;
            chunk.GetComponent<T13>(index, componentType13) = component13;
            chunk.GetComponent<T14>(index, componentType14) = component14;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            ComponentType componentType6 = schema.GetComponent<T6>();
            ComponentType componentType7 = schema.GetComponent<T7>();
            ComponentType componentType8 = schema.GetComponent<T8>();
            ComponentType componentType9 = schema.GetComponent<T9>();
            ComponentType componentType10 = schema.GetComponent<T10>();
            ComponentType componentType11 = schema.GetComponent<T11>();
            ComponentType componentType12 = schema.GetComponent<T12>();
            ComponentType componentType13 = schema.GetComponent<T13>();
            ComponentType componentType14 = schema.GetComponent<T14>();
            ComponentType componentType15 = schema.GetComponent<T15>();
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
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            chunk.GetComponent<T6>(index, componentType6) = component6;
            chunk.GetComponent<T7>(index, componentType7) = component7;
            chunk.GetComponent<T8>(index, componentType8) = component8;
            chunk.GetComponent<T9>(index, componentType9) = component9;
            chunk.GetComponent<T10>(index, componentType10) = component10;
            chunk.GetComponent<T11>(index, componentType11) = component11;
            chunk.GetComponent<T12>(index, componentType12) = component12;
            chunk.GetComponent<T13>(index, componentType13) = component13;
            chunk.GetComponent<T14>(index, componentType14) = component14;
            chunk.GetComponent<T15>(index, componentType15) = component15;
            return entity;
        }

        public static uint CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this World world, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15, T16 component16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            Schema schema = world.Schema;
            ComponentType componentType1 = schema.GetComponent<T1>();
            ComponentType componentType2 = schema.GetComponent<T2>();
            ComponentType componentType3 = schema.GetComponent<T3>();
            ComponentType componentType4 = schema.GetComponent<T4>();
            ComponentType componentType5 = schema.GetComponent<T5>();
            ComponentType componentType6 = schema.GetComponent<T6>();
            ComponentType componentType7 = schema.GetComponent<T7>();
            ComponentType componentType8 = schema.GetComponent<T8>();
            ComponentType componentType9 = schema.GetComponent<T9>();
            ComponentType componentType10 = schema.GetComponent<T10>();
            ComponentType componentType11 = schema.GetComponent<T11>();
            ComponentType componentType12 = schema.GetComponent<T12>();
            ComponentType componentType13 = schema.GetComponent<T13>();
            ComponentType componentType14 = schema.GetComponent<T14>();
            ComponentType componentType15 = schema.GetComponent<T15>();
            ComponentType componentType16 = schema.GetComponent<T16>();
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
            Definition definition = new(componentTypesMask, default, default);
            uint entity = world.CreateEntity(definition, out Chunk chunk, out uint index);
            chunk.GetComponent<T1>(index, componentType1) = component1;
            chunk.GetComponent<T2>(index, componentType2) = component2;
            chunk.GetComponent<T3>(index, componentType3) = component3;
            chunk.GetComponent<T4>(index, componentType4) = component4;
            chunk.GetComponent<T5>(index, componentType5) = component5;
            chunk.GetComponent<T6>(index, componentType6) = component6;
            chunk.GetComponent<T7>(index, componentType7) = component7;
            chunk.GetComponent<T8>(index, componentType8) = component8;
            chunk.GetComponent<T9>(index, componentType9) = component9;
            chunk.GetComponent<T10>(index, componentType10) = component10;
            chunk.GetComponent<T11>(index, componentType11) = component11;
            chunk.GetComponent<T12>(index, componentType12) = component12;
            chunk.GetComponent<T13>(index, componentType13) = component13;
            chunk.GetComponent<T14>(index, componentType14) = component14;
            chunk.GetComponent<T15>(index, componentType15) = component15;
            chunk.GetComponent<T16>(index, componentType16) = component16;
            return entity;
        }
    }
}