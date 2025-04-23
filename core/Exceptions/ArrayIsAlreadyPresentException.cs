using System;
using Types;

namespace Worlds
{
    /// <summary>
    /// An error when an array type is already present on an entity.
    /// </summary>
    public class ArrayIsAlreadyPresentException : Exception
    {
        /// <inheritdoc/>
        public ArrayIsAlreadyPresentException(World world, uint entity, int componentType) : base(GetMessage(world, entity, componentType))
        {
        }

        private unsafe static string GetMessage(World world, uint entity, int componentType)
        {
            TypeMetadata type = world.world->schema.GetComponentLayout(componentType);
            return $"Entity `{entity}` already has array `{type}`";
        }
    }
}