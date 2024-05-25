using Unmanaged.Collections;

namespace Game
{
    public static class ExtensionFunctions
    {
        public static EntityID CreateEntity(this IWorld world)
        {
            return world.CreateEntity();
        }

        public static void DestroyEntity(this IWorld world, EntityID entity)
        {
            world.DestroyEntity(entity);
        }

        public static void DestroyEntity(this IEntity entity)
        {
            entity.World.DestroyEntity(entity.Value);
        }

        public static bool ContainsEntity(this IWorld world, EntityID entity)
        {
            return world.ContainsEntity(entity);
        }

        public static bool ContainsCollection<T>(this IEntity entity) where T : unmanaged
        {
            return entity.World.ContainsCollection<T>(entity.Value);
        }

        public static bool ContainsCollection<T>(this IWorld world, EntityID entity) where T : unmanaged
        {
            return world.ContainsCollection<T>(entity);
        }

        public static UnmanagedList<T> CreateCollection<T>(this IEntity entity) where T : unmanaged
        {
            return entity.World.CreateCollection<T>(entity.Value);
        }

        public static UnmanagedList<T> CreateCollection<T>(this IWorld world, EntityID entity) where T : unmanaged
        {
            return world.Value.CreateCollection<T>(entity);
        }

        public static void DestroyCollection<T>(this IEntity entity) where T : unmanaged
        {
            entity.World.DestroyCollection<T>(entity.Value);
        }

        public static void DestroyCollection<T>(this IWorld world, EntityID entity) where T : unmanaged
        {
            world.Value.DestroyCollection<T>(entity);
        }

        public static UnmanagedList<T> GetCollection<T>(this IEntity entity) where T : unmanaged
        {
            return entity.World.GetCollection<T>(entity.Value);
        }

        public static UnmanagedList<T> GetCollection<T>(this IWorld world, EntityID entity) where T : unmanaged
        {
            return world.GetCollection<T>(entity);
        }

        public static bool ContainsComponent<T>(this IWorld world, EntityID entity) where T : unmanaged
        {
            return world.ContainsComponent<T>(entity);
        }

        public static bool ContainsComponent<T>(this IEntity entity) where T : unmanaged
        {
            return entity.World.ContainsComponent<T>(entity.Value);
        }

        public static ref T GetComponentRef<T>(this IEntity entity) where T : unmanaged
        {
            return ref entity.World.GetComponentRef<T>(entity.Value);
        }

        public static ref T GetComponentRef<T>(this IWorld world, EntityID entity) where T : unmanaged
        {
            return ref world.GetComponentRef<T>(entity);
        }

        public static void AddComponent<T>(this IEntity entity, T component) where T : unmanaged
        {
            entity.World.AddComponent(entity.Value, component);
        }

        public static void AddComponent<T>(this IWorld world, EntityID entity, T component) where T : unmanaged
        {
            world.AddComponent(entity, component);
        }

        public static bool TryGetComponent<T>(this IWorld world, EntityID entity, out T component) where T : unmanaged
        {
            return world.TryGetComponent(entity, out component);
        }

        public static bool TryGetComponent<T>(this IEntity entity, out T component) where T : unmanaged
        {
            return entity.World.TryGetComponent(entity.Value, out component);
        }

        public static ref T TryGetComponentRef<T>(this IWorld world, EntityID entity, out bool has) where T : unmanaged
        {
            return ref world.TryGetComponentRef<T>(entity, out has);
        }

        public static T GetComponent<T>(this IWorld world, EntityID entity, T defaultValue) where T : unmanaged
        {
            return world.GetComponent(entity, defaultValue);
        }

        public static T GetComponent<T>(this IEntity entity, T defaultValue) where T : unmanaged
        {
            return entity.World.GetComponent(entity.Value, defaultValue);
        }

        public static T GetComponent<T>(this IEntity entity) where T : unmanaged
        {
            return entity.World.GetComponent<T>(entity.Value);
        }

        public static T GetComponent<T>(this IWorld world, EntityID entity) where T : unmanaged
        {
            return world.GetComponent<T>(entity);
        }

        public static void SetComponent<T>(this IWorld world, EntityID entity, T component) where T : unmanaged
        {
            world.SetComponent(entity, component);
        }

        public static void SetComponent<T>(this IEntity entity, T component) where T : unmanaged
        {
            entity.World.SetComponent(entity.Value, component);
        }

        public static void RemoveComponent<T>(this IWorld world, EntityID entity) where T : unmanaged
        {
            world.RemoveComponent<T>(entity);
        }

        public static void RemoveComponent<T>(this IEntity entity) where T : unmanaged
        {
            entity.World.RemoveComponent<T>(entity.Value);
        }

        public static bool TryGetFirst<T>(this IWorld world, out EntityID entity) where T : unmanaged
        {
            return world.TryGetFirst(out entity);
        }

        public static bool TryGetFirst<T>(this IWorld world, out EntityID entity, out T component) where T : unmanaged
        {
            return world.TryGetFirst(out entity, out component);
        }
    }
}