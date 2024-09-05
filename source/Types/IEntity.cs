namespace Simulation
{
    public interface IEntity
    {
        uint Value { get; }
        World World { get; }
        Definition Definition { get; }
    }
}