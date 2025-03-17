#pragma warning disable IL2090
#pragma warning disable IL2075
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Worlds
{
    /// <summary>
    /// Extensions for <see cref="IEntity"/> instances.
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Casts the entity to an <see cref="Entity"/> instance.
        /// </summary>
        public unsafe static Entity AsEntity<T>(this T entity) where T : unmanaged, IEntity
        {
            ThrowIfNotEntity<T>();

            return *(Entity*)&entity;
        }

        /// <summary>
        /// Casts the entity to an instance of <typeparamref name="T"/>.
        /// </summary>
        public unsafe static T As<T>(Entity entity) where T : unmanaged, IEntity
        {
            ThrowIfNotEntity<T>();

            return *(T*)&entity;
        }

        /// <summary>
        /// Retrieves the world of the entity.
        /// </summary>
        public unsafe static World GetWorld<T>(this T entity) where T : unmanaged, IEntity
        {
            ThrowIfNotEntity<T>();

            return *(World*)&entity;
        }

        /// <summary>
        /// Retrieves the entity position.
        /// </summary>
        public unsafe static uint GetEntityValue<T>(this T entity) where T : unmanaged, IEntity
        {
            ThrowIfNotEntity<T>();

            return *(uint*)((byte*)&entity + sizeof(World));
        }

#if DEBUG
        [Conditional("DEBUG")]
        internal unsafe static void ThrowIfNotEntity<T>() where T : unmanaged, IEntity
        {
            if (!EntityTypeCache<T>.compatible)
            {
                throw new InvalidCastException($"Cannot cast `{typeof(T)}` to Entity");
            }
        }

        private unsafe static class EntityTypeCache<T> where T : unmanaged, IEntity
        {
            public static readonly bool compatible;

            static EntityTypeCache()
            {
                uint actualSize = (uint)sizeof(T);
                uint expectedSize = (uint)sizeof(Entity);
                if (actualSize != expectedSize)
                {
                    compatible = false;
                    return;
                }

                FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fields.Length == 2)
                {
                    if (fields[0].FieldType == typeof(World) && fields[1].FieldType == typeof(uint))
                    {
                        compatible = true;
                    }
                    else
                    {
                        compatible = false;
                    }
                }
                else if (fields.Length == 1)
                {
                    Stack<Type> stack = new();
                    stack.Push(fields[0].FieldType);
                    while (stack.Count > 0)
                    {
                        Type current = stack.Pop();
                        if (current == typeof(Entity))
                        {
                            compatible = true;
                            break;
                        }
                        else
                        {
                            FieldInfo[] typeFields = current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (typeFields.Length == 2)
                            {
                                if (typeFields[0].FieldType == typeof(World) && typeFields[1].FieldType == typeof(uint))
                                {
                                    compatible = true;
                                    break;
                                }
                                else
                                {
                                    compatible = false;
                                    break;
                                }
                            }
                            else if (typeFields.Length == 1)
                            {
                                stack.Push(typeFields[0].FieldType);
                            }
                            else
                            {
                                compatible = false;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    compatible = false;
                }
            }
        }
#else
        /// <inheritdoc/>
        [Conditional("DEBUG")]
        public static void ThrowIfNotEntity<T>() where T : unmanaged, IEntity
        {
        }
#endif
    }
}