using System;
using Types;

namespace Worlds
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
        /// Describes the components, arrays and tags that compose this entity.
        /// </summary>
        void Describe(ref Archetype archetype);
    }

    public interface IEntityNew : IInherits<EntityNew>
    {

    }
}