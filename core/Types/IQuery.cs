namespace Simulation
{
    public interface IQuery
    {
        nint Results { get; }
        uint ResultSize { get; }
        uint Count { get; }
    }
}
