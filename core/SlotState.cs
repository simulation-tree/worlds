namespace Worlds
{
    /// <summary>
    /// All possible states of an entity.
    /// </summary>
    public enum SlotState : byte
    {
        /// <summary>
        /// Uninitialized.
        /// </summary>
        Unknown,

        /// <summary>
        /// The slot doesn't describe a valid entity.
        /// </summary>
        Free,

        /// <summary>
        /// The entity is enabled.
        /// </summary>
        Enabled,

        /// <summary>
        /// The entity is disabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// The entity is enabled on its own, but disabled due to ancestors.
        /// </summary>
        DisabledButLocallyEnabled
    }
}