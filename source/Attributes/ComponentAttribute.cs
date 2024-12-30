﻿using System;

namespace Worlds
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class TypeAttribute : Attribute
    {
    }

    /// <summary>
    /// States that the decorated type is a <see cref="ComponentType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class ComponentAttribute : TypeAttribute
    {
    }

    /// <summary>
    /// States that the decorated type is an <see cref="ArrayElementType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class ArrayElementAttribute : TypeAttribute
    {
    }

    /// <summary>
    /// States that the decorated type is a <see cref="TagType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class TagAttribute : TypeAttribute
    {
    }
}
