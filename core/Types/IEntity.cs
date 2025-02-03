using System;

namespace Worlds
{
    /// <summary>
    /// Denotes that the implementing type is an <see cref="Entity"/>.
    /// </summary>
    public interface IEntity : IDisposable
    {
        /// <summary>
        /// Describes the <see cref="ComponentType"/>s, <see cref="ArrayElementType"/>s
        /// and <see cref="TagType"/>s that should be present on entities of this type.
        /// </summary>
        void Describe(ref Archetype archetype);
    }
}