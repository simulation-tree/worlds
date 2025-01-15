using System;
using Types;

namespace Worlds
{
    /// <summary>
    /// States that the decorated type is a <see cref="TagType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class TagAttribute : TypeAttribute
    {
    }
}