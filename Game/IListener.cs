namespace Game
{
    /// <summary>
    /// Objects added to <see cref="VirtualMachine"/>s that
    /// implement this will be registered as listeners for events
    /// of type <typeparamref name="T"/>.
    /// </summary>
    public interface IListener<T> where T : unmanaged
    {
        void Receive(World world, ref T e);
    }
}
