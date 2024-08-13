using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unmanaged;

namespace Simulation
{
    public struct Entity : IEntity, IDisposable
    {
        public eint value;
        public World world;

        World IEntity.World => world;
        eint IEntity.Value => value;

        public Entity(World world, eint existingEntity)
        {
            this.value = existingEntity;
            this.world = world;
        }

        public Entity(World world)
        {
            this.world = world;
            value = world.CreateEntity();
        }

        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[32];
            int length = ToString(buffer);
            return new string(buffer[..length]);
        }

        public readonly int ToString(Span<char> buffer)
        {
            int length = 0;
            if (this.IsDestroyed())
            {
                buffer[length++] = 'D';
                buffer[length++] = 'e';
                buffer[length++] = 's';
                buffer[length++] = 't';
                buffer[length++] = 'r';
                buffer[length++] = 'o';
                buffer[length++] = 'y';
                buffer[length++] = 'e';
                buffer[length++] = 'd';
                buffer[length++] = ' ';
            }

            value.TryFormat(buffer, out int valueLength);
            return length + valueLength;
        }

        /// <summary>
        /// Destroys the entity.
        /// </summary>
        public readonly void Dispose()
        {
            //todo: should this exist? should Dispose be removed so that its only Destroy? ditch it?
            this.ThrowIfDestroyed();
            world.DestroyEntity(value);
        }

        public static bool TryFindFirst<T>(World world, out T entity) where T : unmanaged, IEntity
        {
            using Query query = new T().GetQuery(world);
            query.Update();

            if (query.Count > 0)
            {
                eint firstEntity = query[0];
                ref byte firstByte = ref System.Runtime.CompilerServices.Unsafe.As<eint, byte>(ref firstEntity);
                entity = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<T>(ref firstByte);
                return true;
            }
            else
            {
                entity = default;
                return false;
            }
        }

        public static T GetFirst<T>(World world) where T : unmanaged, IEntity
        {
            using Query query = new T().GetQuery(world);
            query.Update();

            if (query.Count > 0)
            {
                eint firstEntity = query[0];
                ref byte firstByte = ref System.Runtime.CompilerServices.Unsafe.As<eint, byte>(ref firstEntity);
                T entity = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<T>(ref firstByte);
                return entity;
            }

            throw new NullReferenceException($"Component of type {typeof(T)} not found in world.");
        }

        Query IEntity.GetQuery(World world)
        {
            return new Query(world);
        }

        /// <summary>
        /// Returns <c>true</c> if the entity complies with the given type.
        /// </summary>
        public static bool Is<T>(World world, eint entity) where T : unmanaged, IEntity
        {
            using Query query = default(T).GetQuery(world);
            ReadOnlySpan<RuntimeType> entityComponentTypes = world.GetComponentTypes(entity);
            foreach (RuntimeType type in query.Types)
            {
                if (!entityComponentTypes.Contains(type))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Retrieves the given entity as a <typeparamref name="T"/>,
        /// assuming it is that type.
        /// </summary>
        public unsafe static T As<T>(World world, eint entity) where T : unmanaged, IEntity
        {
            ThrowIfTypeLayoutMismatches(typeof(T));
            Entity e = new(world, entity);
            return *(T*)&e;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfTypeLayoutMismatches(Type type)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Stack<Type> checkStack = new();
            checkStack.Push(type);
            while (checkStack.Count > 0)
            {
                Type checkingType = checkStack.Pop();
                if (checkingType == typeof(Entity))
                {
                    return;
                }
                else if (typeof(IEntity).IsAssignableFrom(checkingType))
                {
#pragma warning disable IL2075
                    FieldInfo[] checkingFields = checkingType.GetFields(flags);
#pragma warning restore IL2075
                    if (checkingFields.Length == 1)
                    {
                        checkStack.Push(checkingFields[0].FieldType);
                    }
                    else if (checkingFields.Length == 2)
                    {
                        Type first = checkingFields[0].FieldType;
                        Type second = checkingFields[1].FieldType;
                        if (first == typeof(eint) && second == typeof(World))
                        {
                            return;
                        }
                        else
                        {
                            throw new Exception($"Unexpected entity layout in `{checkingType}`. Was expecting `{nameof(eint)}`, then `{nameof(World)}`");
                        }
                    }
                }
            }

            throw new Exception($"The type `{type}` does not align with the `{nameof(Entity)}` type");
        }
    }
}