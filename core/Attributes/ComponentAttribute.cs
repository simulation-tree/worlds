using System;
using Types;

namespace Worlds
{
    /// <summary>
    /// States that the decorated type is a <see cref="ComponentType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class ComponentAttribute : TypeAttribute
    {
    }
}