using System;

namespace Worlds
{
    /// <summary>
    /// States that the decorated type is an <see cref="ArrayElementType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class ArrayElementAttribute : Attribute
    {
    }
}