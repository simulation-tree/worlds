using System;

namespace Worlds
{
    /// <summary>
    /// Exception indicating that a tag type is already present on an entity.
    /// </summary>
    public class TagIsAlreadyPresentException : Exception
    {
        /// <inheritdoc/>
        public TagIsAlreadyPresentException(World world, uint entity, int tagType) : base(GetMessage(world, entity, tagType))
        {
        }

        private static string GetMessage(World world, uint entity, int tagType)
        {
            Types.Type type = world.Schema.GetTagLayout(tagType);
            return $"Entity `{entity}` already has tag `{type}`";
        }
    }
}