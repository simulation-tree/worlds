using Unmanaged;

namespace Game.ECS
{
    public readonly unsafe struct Listener
    {
        private readonly delegate* unmanaged<World, Container, void> callback;

        public Listener(delegate* unmanaged<World, Container, void> callback)
        {
            this.callback = callback;
        }

        public void Invoke(World sender, Container container)
        {
            callback(sender, container);
        }
    }
}
