using Programs.Functions;

namespace Programs
{
    public interface IProgram
    {
        /// <summary>
        /// Called when this program has started.
        /// </summary>
        StartFunction Start { get; }

        /// <summary>
        /// Called when the simulator iterates over this program.
        /// </summary>
        UpdateFunction Update { get; }

        /// <summary>
        /// Called when this program is finished, and just before it's disposed.
        /// </summary>
        FinishFunction Finish { get; }
    }
}