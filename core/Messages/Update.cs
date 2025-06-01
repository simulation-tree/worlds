namespace Worlds.Messages
{
    /// <summary>
    /// Message type for a world update.
    /// </summary>
    public readonly struct Update
    {
        /// <summary>
        /// The delta time for the update.
        /// </summary>
        public readonly double deltaTime;

        /// <summary>
        /// The delta time for the update as a float.
        /// </summary>
        public readonly float DeltaTimeAsFloat => (float)deltaTime;

        /// <summary>
        /// Initializes the message.
        /// </summary>
        public Update(double deltaTime)
        {
            this.deltaTime = deltaTime;
        }
    }
}
