using Collections;
using Unmanaged;

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
        /// All arrays stored.
        /// </summary>
        public Array<Allocation> arrays;

        /// <summary>
        /// Lengths of arrays.
        /// </summary>
        public Array<uint> arrayLengths;

        /// <summary>
        /// State of the entity.
        /// </summary>
        public EntitySlotState state;
    }
}