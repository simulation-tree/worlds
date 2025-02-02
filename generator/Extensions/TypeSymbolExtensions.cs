using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Worlds.Generator
{
    public static class TypeSymbolExtensions
    {
        public static IEnumerable<ITypeSymbol> GetInheritingTypes(this ITypeSymbol type)
        {
            Stack<INamedTypeSymbol> stack = new();
            foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
            {
                stack.Push(interfaceType);
                while (stack.Count > 0)
                {
                    INamedTypeSymbol current = stack.Pop();
                    if (current.GetFullTypeName().StartsWith("Worlds.IEntity<"))
                    {
                        ITypeSymbol genericType = current.TypeArguments[0];
                        foreach (INamedTypeSymbol interfaceTypeOfGeneric in genericType.AllInterfaces)
                        {
                            stack.Push(interfaceTypeOfGeneric);
                        }

                        yield return genericType;
                    }
                    else if (current.BaseType is INamedTypeSymbol currentBaseType)
                    {
                        stack.Push(currentBaseType);
                    }
                }
            }
        }
    }
}
