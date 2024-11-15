using System;

namespace Simulation
{
    public interface IEntity : IDisposable
    {
        uint Value { get; }
        World World { get; }
        Definition Definition { get; }
    }
}