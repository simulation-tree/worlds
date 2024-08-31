namespace Simulation
{
    public interface IEntity
    {
        uint Value { get; }
        World World { get; }

        /// <summary>
        /// Creates a new query that will find entities that are this type.
        /// </summary>
        Query GetQuery(World world);
    }
}