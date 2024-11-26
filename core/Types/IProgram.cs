namespace Programs
{
    /// <summary>
    /// Describes a program.
    /// </summary>
    public interface IProgram
    {
        /// <summary>
        /// Called when this program has started.
        /// </summary>
        StartProgramFunction Start { get; }

        /// <summary>
        /// Called when the simulator iterates over this program.
        /// </summary>
        UpdateProgramFunction Update { get; }

        /// <summary>
        /// Called when this program is finished, and just before it's disposed.
        /// </summary>
        FinishProgramFunction Finish { get; }
    }
}