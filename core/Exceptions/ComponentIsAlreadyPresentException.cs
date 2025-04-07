using System;

namespace Worlds
{
    /// <summary>
    /// An error when a component type is already present on an entity.
    /// </summary>
    public class ComponentIsAlreadyPresentException : Exception
    {
        /// <inheritdoc/>
        public ComponentIsAlreadyPresentException(World world, uint entity, int componentType) : base(GetMessage(world, entity, componentType))
        {
        }
        private static string GetMessage(World world, uint entity, int componentType)
        {
            Types.Type type = world.Schema.GetComponentLayout(componentType);
            return $"Entity `{entity}` already has component `{type}`";
        }
    }
}