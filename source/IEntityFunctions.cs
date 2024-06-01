using Unmanaged.Collections;

namespace Game
{
    public static class IEntityFunctions
    {
        /// <summary>
        /// Destroys the entity and returns <c>true</c> if successful.
        /// </summary>
        public static bool Destroy<T>(this T entity) where T : IEntity
        {
            if (entity.World.ContainsEntity(entity.Value))
            {
                entity.World.DestroyEntity(entity.Value);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// <c>true</c> when the entity doesn't exists in its <see cref="Game.World"/> anymore,
        /// or it has been disposed of.
        /// </summary>
        public static bool IsDestroyed<T>(this T entity) where T : IEntity
        {
            return !entity.World.ContainsEntity(entity.Value);
        }

        public static bool ContainsComponent<T>(this IEntity entity) where T : unmanaged
        {
            return entity.World.ContainsComponent<T>(entity.Value);
        }

        public static ref T GetComponentRef<T>(this IEntity entity) where T : unmanaged
        {
            return ref entity.World.GetComponentRef<T>(entity.Value);
        }

        public static bool TryGetComponent<T>(this IEntity entity, out T component) where T : unmanaged
        {
            if (entity.ContainsComponent<T>())
            {
                component = entity.GetComponent<T>();
                return true;
            }
            else
            {
                component = default;
                return false;
            }
        }

        public static T GetComponent<T>(this IEntity entity, T defaultValue) where T : unmanaged
        {
            return entity.World.GetComponent(entity.Value, defaultValue);
        }

        public static T GetComponent<T>(this IEntity world, EntityID entity) where T : unmanaged
        {
            return world.World.GetComponent<T>(entity);
        }

        public static T GetComponent<T>(this IEntity entity) where T : unmanaged
        {
            return entity.World.GetComponent<T>(entity.Value);
        }

        public static void SetComponent<T>(this IEntity entity, T component) where T : unmanaged
        {
            entity.GetComponentRef<T>() = component;
        }

        public static void AddComponent<T>(this IEntity entity, T component) where T : unmanaged
        {
            entity.World.AddComponent(entity.Value, component);
        }

        public static void RemoveComponent<T>(this IEntity entity) where T : unmanaged
        {
            entity.World.RemoveComponent<T>(entity.Value);
        }

        public static UnmanagedList<T> GetCollection<T>(this IEntity entity) where T : unmanaged
        {
            return entity.World.GetCollection<T>(entity.Value);
        }

        public static UnmanagedList<T> GetCollection<T>(this IEntity world, EntityID entity) where T : unmanaged
        {
            return world.World.GetCollection<T>(entity);
        }

        public static bool ContainsCollection<T>(this IEntity entity) where T : unmanaged
        {
            return entity.World.ContainsCollection<T>(entity.Value);
        }

        public static void DestroyCollection<T>(this IEntity entity) where T : unmanaged
        {
            entity.World.DestroyCollection<T>(entity.Value);
        }

        public static UnmanagedList<T> CreateCollection<T>(this IEntity entity) where T : unmanaged
        {
            return entity.World.CreateCollection<T>(entity.Value);
        }

        public static UnmanagedList<T> CreateCollection<T>(this IEntity entity, uint initialCapacity) where T : unmanaged
        {
            return entity.World.CreateCollection<T>(entity.Value, initialCapacity);
        }
    }
}