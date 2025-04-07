using System;

namespace Worlds
{
    /// <summary>
    /// Represents an error indicating that the expected entity is missing.
    /// </summary>
    public class ReferenceToEntityIsMissingException : Exception
    {
        /// <inheritdoc/>
        public ReferenceToEntityIsMissingException(World world, uint entity, rint reference) : base(GetMessage(world, entity, reference))
        {
        }

        /// <inheritdoc/>
        public ReferenceToEntityIsMissingException(World world, uint entity, uint referencedEntity) : base(GetMessage(world, entity, referencedEntity))
        {
        }

        private static string GetMessage(World world, uint entity, rint reference)
        {
            return $"Entity `{entity}` is missing referenced entity at index `{reference}`";
        }

        private static string GetMessage(World world, uint entity, uint referencedEntity)
        {
            return $"Entity `{entity}` does not reference other entity `{referencedEntity}`";
        }
    }
}