namespace Worlds
{
    /// <summary>
    /// Describes a slot where an entity may or may not be stored.
    /// </summary>
    public struct EntitySlot
    {
        /// <summary>
        /// Entity ID.
        /// </summary>
        public uint entity;

        /// <summary>
        /// The chunk that this entity slot belongs to.
        /// </summary>
        public Chunk chunk;

        /// <summary>
        /// State of the entity.
        /// </summary>
        public EntitySlotState state;
    }
}