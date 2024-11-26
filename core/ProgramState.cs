namespace Programs
{
    /// <summary>
    /// Describes the state of a program.
    /// </summary>
    public enum ProgramState : byte
    {
        /// <summary>
        /// A <see cref="Simulation.Simulator"/> has not initialized the program.
        /// </summary>
        Uninitialized,

        /// <summary>
        /// The program is currently running.
        /// </summary>
        Active,

        /// <summary>
        /// The program has finished running.
        /// </summary>
        Finished
    }
}