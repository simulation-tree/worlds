using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Worlds.Analyzer
{
    public static class TypeSymbolExtensions
    {
        /// <summary>
        /// Checks if the type contains an attribute with the given name.
        /// </summary>
        public static bool HasAttribute(this ITypeSymbol type, string attributeName)
        {
            Stack<ITypeSymbol> stack = new();

            ImmutableArray<AttributeData> attributes = type.GetAttributes();
            foreach (AttributeData attribute in attributes)
            {
                if (attribute.AttributeClass is INamedTypeSymbol attributeType)
                {
                    stack.Push(attributeType);
                }
            }

            while (stack.Count > 0)
            {
                ITypeSymbol current = stack.Pop();
                string attributeTypeName = current.ToDisplayString();
                if (attributeName == attributeTypeName)
                {
                    return true;
                }
                else
                {
                    if (current.BaseType is INamedTypeSymbol currentBaseType)
                    {
                        stack.Push(currentBaseType);
                    }
                }
            }

            return false;
        }
    }
}