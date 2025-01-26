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
        /// Number of children.
        /// </summary>
        public ushort childCount;

        /// <summary>
        /// Children of the entity.
        /// </summary>
        public List<uint> children;

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

        public readonly bool TryGetChildren(out USpan<uint> children)
        {
            bool contains = childCount > 0;
            if (contains)
            {
                children = this.children.AsSpan(0, childCount);
            }
            else
            {
                children = default;
            }

            return contains;
        }
    }
}