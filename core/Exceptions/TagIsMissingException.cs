using System;
using Types;

namespace Worlds
{
    /// <summary>
    /// Exception indicating that a tag type is missing on an entity.
    /// </summary>
    public class TagIsMissingException : Exception
    {
        /// <inheritdoc/>
        public TagIsMissingException(World world, uint entity, int tagType) : base(GetMessage(world, entity, tagType))
        {
        }

        private unsafe static string GetMessage(World world, uint entity, int tagType)
        {
            TypeMetadata type = world.schema.GetTagLayout(tagType);
            return $"Entity `{entity}` is missing tag `{type}`";
        }
    }
}