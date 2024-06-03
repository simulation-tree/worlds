namespace Game
{
    public interface IEntity
    {
        World World { get; }
        EntityID Value { get; }
    }
}