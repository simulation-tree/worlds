using System;

namespace Worlds
{
    /// <summary>
    /// States that the decorated type is a <see cref="ComponentType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class ComponentAttribute : Attribute
    {
    }

    /// <summary>
    /// States that the decorated type is an <see cref="ArrayType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class ArrayAttribute : Attribute
    {
    }
}
