using System;

namespace Worlds
{
    /// <summary>
    /// Represents an error indicating that the expected entity is missing.
    /// </summary>
    public class EntityIsMissingException : Exception
    {
        /// <inheritdoc/>
        public EntityIsMissingException(World world, uint entity) : base(GetMessage(world, entity))
        {
        }

        private static string GetMessage(World world, uint entity)
        {
            return $"Entity `{entity}` is missing";
        }
    }
}