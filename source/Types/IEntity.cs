namespace Simulation
{
    public interface IEntity
    {
        eint Value { get; }
        World World { get; }

        /// <summary>
        /// Creates a new query that will find entities that are this type.
        /// </summary>
        Query GetQuery(World world);
    }
}