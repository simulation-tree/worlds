using System;
using System.Diagnostics;
using System.Reflection;

namespace Worlds
{
    public static class EntityExtensions
    {
        public unsafe static Entity AsEntity<T>(this T entity) where T : unmanaged, IEntity
        {
            ThrowIfNotEntity<T>();

            return *(Entity*)&entity;
        }

        public unsafe static T As<T>(Entity entity) where T : unmanaged, IEntity
        {
            ThrowIfNotEntity<T>();

            return *(T*)&entity;
        }

        public static World GetWorld<T>(this T entity) where T : unmanaged, IEntity
        {
            return AsEntity(entity).world;
        }

        public static uint GetEntityValue<T>(this T entity) where T : unmanaged, IEntity
        {
            return AsEntity(entity).value;
        }

#if DEBUG
        [Conditional("DEBUG")]
        public unsafe static void ThrowIfNotEntity<T>() where T : unmanaged, IEntity
        {
            uint actualSize = (uint)sizeof(T);
            uint expectedSize = (uint)sizeof(Entity);
            if (actualSize != expectedSize)
            {
                throw new InvalidCastException($"Cannot cast {typeof(T)} to Entity");
            }

            FieldInfo[] fields = FieldCache<T>.fields;
            if (fields.Length == 2)
            {
                if (fields[0].FieldType != typeof(World) || fields[1].FieldType != typeof(uint))
                {
                    throw new InvalidCastException($"Cannot cast {typeof(T)} to Entity");
                }
            }
            else
            {
                Type type = typeof(T);
                throw new InvalidCastException($"Cannot cast {type} to Entity");
            }
        }

        private static class FieldCache<T> where T : unmanaged, IEntity
        {
#pragma warning disable IL2090
            public static readonly FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
#pragma warning restore IL2090
        }
#else
        [Conditional("DEBUG")]
        public static void ThrowIfNotEntity<T>() where T : unmanaged, IEntity
        {
        }
#endif
    }
}