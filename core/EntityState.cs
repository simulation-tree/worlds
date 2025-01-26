namespace Worlds
{
    /// <summary>
    /// Enabled state of an entity.
    /// </summary>
    public enum EntityState : byte
    {
        /// <summary>
        /// The slot describes a destroyed entity, free for new ones.
        /// </summary>
        Free,

        /// <summary>
        /// Entity is enabled.
        /// </summary>
        Enabled,

        /// <summary>
        /// Entity is disabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// Entity is enabled while its parent or ancesstor isn't.
        /// </summary>
        DisabledButLocallyEnabled
    }
}