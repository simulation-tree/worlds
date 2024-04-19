namespace Game.Events
{
    /// <summary>
    /// Called when a <see cref="VirtualMachine"/> updates.
    /// </summary>
    public readonly struct Update
    {

    }

    public readonly struct SystemAdded
    {
        public readonly uint index;

        public SystemAdded(uint index)
        {
            this.index = index;
        }
    }

    public readonly struct SystemRemoved
    {
        public readonly uint index;

        public SystemRemoved(uint index)
        {
            this.index = index;
        }
    }
}
