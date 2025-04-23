using System;
using Types;

namespace Worlds
{
    /// <summary>
    /// An error when a component type is missing from an entity.
    /// </summary>
    public class ComponentIsMissingException : Exception
    {
        /// <inheritdoc/>
        public ComponentIsMissingException(World world, uint entity, int componentType) : base(GetMessage(world, entity, componentType))
        {
        }

        private unsafe static string GetMessage(World world, uint entity, int componentType)
        {
            TypeMetadata type = world.world->schema.GetComponentLayout(componentType);
            return $"Entity `{entity}` is missing component `{type}`";
        }
    }
}