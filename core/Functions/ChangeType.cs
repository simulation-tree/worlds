namespace Worlds.Functions
{
    /// <summary>
    /// Options for a kind of change.
    /// </summary>
    public enum ChangeType : byte
    {
        /// <summary>
        /// A positive change. Adding or creating.
        /// </summary>
        Positive,

        /// <summary>
        /// A negative change. Removing or destroying.
        /// </summary>
        Negative,
    }
}