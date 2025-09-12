using System;

namespace Worlds
{
    /// <summary>
    /// Different properties that an entity can have.
    /// </summary>
    [Flags]
    public enum SlotFlags : byte
    {
        /// <summary>
        /// No settings.
        /// </summary>
        None = 0,

        /// <summary>
        /// Entity contains arrays.
        /// </summary>
        ContainsArrays = 1,

        /// <summary>
        /// Entity contains children.
        /// </summary>
        ContainsChildren = 2,

        /// <summary>
        /// The arrays on this entity are outdated.
        /// </summary>
        ArraysOutdated = 8,

        /// <summary>
        /// The children on this entity are outdated.
        /// </summary>
        ChildrenOutdated = 16,

        /// <summary>
        /// The entity is outdated and needs to be refreshed back to initial state.
        /// </summary>
        Outdated = ArraysOutdated | ChildrenOutdated
    }
}