namespace Game
{
    public interface IEntity
    {
        World World { get; }
        EntityID Value { get; }

        /// <summary>
        /// <c>true</c> when the entity doesn't exists in its <see cref="Game.World"/> anymore,
        /// or it has been disposed of.
        /// </summary>
        public bool IsDestroyed()
        {
            return !World.ContainsEntity(Value);
        }

        public bool ContainsComponent<T>() where T : unmanaged
        {
            return World.ContainsComponent<T>(Value);
        }

        public ref T GetComponentRef<T>() where T : unmanaged
        {
            return ref World.GetComponentRef<T>(Value);
        }

        public bool TryGetComponent<T>(out T component) where T : unmanaged
        {
            if (ContainsComponent<T>())
            {
                component = GetComponent<T>();
                return true;
            }
            else
            {
                component = default;
                return false;
            }
        }

        public T GetComponent<T>(T defaultValue) where T : unmanaged
        {
            return World.GetComponent(Value, defaultValue);
        }

        public T GetComponent<T>() where T : unmanaged
        {
            return World.GetComponent<T>(Value);
        }

        public void SetComponent<T>(T component) where T : unmanaged
        {
            GetComponentRef<T>() = component;
        }
    }
}