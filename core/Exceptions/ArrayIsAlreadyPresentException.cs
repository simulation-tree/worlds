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
        public ArrayIsAlreadyPresentException(World world, uint entity, int arrayType) : base(GetMessage(world, entity, arrayType))
        {
        }

        private unsafe static string GetMessage(World world, uint entity, int arrayType)
        {
            TypeMetadata type = world.world->schema.GetArrayLayout(arrayType);
            return $"Entity `{entity}` already has array `{type}`";
        }
    }
}