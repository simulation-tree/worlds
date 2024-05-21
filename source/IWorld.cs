namespace Game
{
    public interface IWorld
    {
        World Value { get; }

        public EntityID CreateEntity()
        {
            return Value.CreateEntity();
        }

        public void DestroyEntity(EntityID entity)
        {
            Value.DestroyEntity(entity);
        }

        public void DestroyEntity(IEntity entity)
        {
            entity.World.DestroyEntity(entity.Value);
        }

        public bool ContainsEntity(EntityID entity)
        {
            return Value.ContainsEntity(entity);
        }

        public bool ContainsComponent<T>(EntityID entity) where T : unmanaged
        {
            return Value.ContainsComponent<T>(entity);
        }

        public bool ContainsComponent<T>(IEntity entity) where T : unmanaged
        {
            return entity.World.ContainsComponent<T>(entity.Value);
        }

        public ref T GetComponentRef<T>(IEntity entity) where T : unmanaged
        {
            return ref entity.World.GetComponentRef<T>(entity.Value);
        }

        public ref T GetComponentRef<T>(EntityID entity) where T : unmanaged
        {
            return ref Value.GetComponentRef<T>(entity);
        }

        public void AddComponent<T>(IEntity entity, T component) where T : unmanaged
        {
            entity.World.AddComponent(entity.Value, component);
        }

        public void AddComponent<T>(EntityID entity, T component) where T : unmanaged
        {
            Value.AddComponent(entity, component);
        }

        public bool TryGetComponent<T>(EntityID entity, out T component) where T : unmanaged
        {
            return Value.TryGetComponent(entity, out component);
        }

        public bool TryGetComponent<T>(IEntity entity, out T component) where T : unmanaged
        {
            return entity.World.TryGetComponent(entity.Value, out component);
        }

        public ref T TryGetComponentRef<T>(EntityID entity, out bool has) where T : unmanaged
        {
            return ref Value.TryGetComponentRef<T>(entity, out has);
        }

        public void SetComponent<T>(IEntity entity, T component) where T : unmanaged
        {
            entity.GetComponentRef<T>() = component;
        }

        public T GetComponent<T>(IEntity entity, T defaultValue) where T : unmanaged
        {
            return entity.GetComponent<T>(defaultValue);
        }

        public T GetComponent<T>(EntityID entity, T defaultValue) where T : unmanaged
        {
            return Value.GetComponent(entity, defaultValue);
        }

        public T GetComponent<T>(EntityID entity) where T : unmanaged
        {
            return Value.GetComponent<T>(entity);
        }

        public T GetComponent<T>(IEntity entity) where T : unmanaged
        {
            return entity.GetComponent<T>();
        }

        public void SetComponent<T>(EntityID entity, T component) where T : unmanaged
        {
            Value.SetComponent(entity, component);
        }

        public void RemoveComponent<T>(EntityID entity) where T : unmanaged
        {
            Value.RemoveComponent<T>(entity);
        }

        public bool TryGetFirst<T>(out T component) where T : unmanaged
        {
            return Value.TryGetFirst<T>(out component);
        }

        public bool TryGetFirst<T>(out EntityID entity, out T component) where T : unmanaged
        {
            return Value.TryGetFirst<T>(out entity, out component);
        }
    }
}