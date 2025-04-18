﻿using System;

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

        private static string GetMessage(World world, uint entity, int componentType)
        {
            Types.Type type = world.Schema.GetComponentLayout(componentType);
            return $"Entity `{entity}` is missing component `{type}`";
        }
    }
}