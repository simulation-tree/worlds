namespace Simulation
{
    /// <summary>
    /// Describes a query.
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// Native address of the results array.
        /// </summary>
        nint Results { get; }

        /// <summary>
        /// Size of each result.
        /// </summary>
        uint ResultSize { get; }

        /// <summary>
        /// Amount of results.
        /// </summary>
        uint Count { get; }
    }
}
