﻿using System;

namespace Worlds
{
    /// <summary>
    /// An error when an array type is missing from an entity.
    /// </summary>
    public class ArrayIsMissingException : Exception
    {
        /// <inheritdoc/>
        public ArrayIsMissingException(World world, uint entity, int componentType) : base(GetMessage(world, entity, componentType))
        {
        }

        private static string GetMessage(World world, uint entity, int componentType)
        {
            Types.Type type = world.Schema.GetComponentLayout(componentType);
            return $"Entity `{entity}` is missing array `{type}`";
        }
    }
}