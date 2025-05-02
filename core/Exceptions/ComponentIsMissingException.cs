using System;
using Types;
using Unmanaged;

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

        /// <inheritdoc/>
        public ComponentIsMissingException(World world, uint entity, BitMask componentTypes) : base(GetMessage(world, entity, componentTypes))
        {
        }

        private unsafe static string GetMessage(World world, uint entity, int componentType)
        {
            TypeMetadata type = world.world->schema.GetComponentLayout(componentType);
            return $"Entity `{entity}` is missing component `{type}`";
        }

        private unsafe static string GetMessage(World world, uint entity, BitMask componentTypes)
        {
            using Text text = new($"Entity {entity} already has the following components: ");
            for (int c = 0; c < BitMask.Capacity; c++)
            {
                if (componentTypes.Contains(c))
                {
                    TypeMetadata type = world.world->schema.GetComponentLayout(c);
                    text.Append(type.ToString());
                    text.Append('\n');
                }
            }

            return text.ToString();
        }
    }
}