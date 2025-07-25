using System;
using Types;

namespace Worlds
{
    /// <summary>
    /// An error when an array type is missing from an entity.
    /// </summary>
    public class ArrayIsMissingException : Exception
    {
        /// <inheritdoc/>
        public ArrayIsMissingException(World world, uint entity, int arrayType) : base(GetMessage(world, entity, arrayType))
        {
        }

        private unsafe static string GetMessage(World world, uint entity, int arrayType)
        {
            TypeMetadata type = world.world->schema.GetArrayLayout(arrayType);
            return $"Entity `{entity}` is missing array `{type}`";
        }
    }
}