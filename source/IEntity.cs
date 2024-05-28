namespace Game
{
    public interface IEntity : INode
    {
        World World { get; }
        EntityID Value { get; }
    }
}