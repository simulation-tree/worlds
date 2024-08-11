using System;

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
    }
}