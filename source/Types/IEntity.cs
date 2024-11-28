using System;

namespace Simulation
{
    /// <summary>
    /// Describes a type of entity.
    /// </summary>
    public interface IEntity : IDisposable
    {
        /// <summary>
        /// The ID of the entity.
        /// </summary>
        uint Value { get; }

        /// <summary>
        /// The world that the entity belongs to.
        /// </summary>
        World World { get; }

        /// <summary>
        /// Describes the components and arrays of the entity.
        /// </summary>
        Definition Definition { get; }
    }
}