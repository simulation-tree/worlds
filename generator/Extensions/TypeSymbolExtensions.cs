using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Worlds
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
                    if (current.ToDisplayString().StartsWith("Worlds.IEntity<"))
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

        public static IEnumerable<IMethodSymbol> GetMethods(this ITypeSymbol type)
        {
            foreach (ISymbol typeMember in type.GetMembers())
            {
                if (typeMember is IMethodSymbol method)
                {
                    if (method.MethodKind == MethodKind.Constructor)
                    {
                        continue;
                    }

                    yield return method;
                }
            }
        }
        
        public static string GetFullTypeName(this ITypeSymbol symbol)
        {
            SpecialType special = symbol.SpecialType;
            if (special == SpecialType.System_Boolean)
            {
                return "System.Boolean";
            }
            else if (special == SpecialType.System_Byte)
            {
                return "System.Byte";
            }
            else if (special == SpecialType.System_SByte)
            {
                return "System.SByte";
            }
            else if (special == SpecialType.System_Int16)
            {
                return "System.Int16";
            }
            else if (special == SpecialType.System_UInt16)
            {
                return "System.UInt16";
            }
            else if (special == SpecialType.System_Int32)
            {
                return "System.Int32";
            }
            else if (special == SpecialType.System_UInt32)
            {
                return "System.UInt32";
            }
            else if (special == SpecialType.System_Int64)
            {
                return "System.Int64";
            }
            else if (special == SpecialType.System_UInt64)
            {
                return "System.UInt64";
            }
            else if (special == SpecialType.System_Single)
            {
                return "System.Single";
            }
            else if (special == SpecialType.System_Double)
            {
                return "System.Double";
            }
            else if (special == SpecialType.System_Decimal)
            {
                return "System.Decimal";
            }
            else if (special == SpecialType.System_Char)
            {
                return "System.Char";
            }
            else if (special == SpecialType.System_IntPtr)
            {
                return "System.IntPtr";
            }
            else if (special == SpecialType.System_UIntPtr)
            {
                return "System.UIntPtr";
            }
            else
            {
                return symbol.ToDisplayString();
            }
        }

        /// <summary>
        /// Checks if the type implements the interface with the given <paramref name="fullInterfaceName"/>.
        /// </summary>
        public static bool HasInterface(this ITypeSymbol type, string fullInterfaceName)
        {
            Stack<ITypeSymbol> stack = new();
            foreach (ITypeSymbol interfaceType in type.AllInterfaces)
            {
                stack.Push(interfaceType);
            }

            while (stack.Count > 0)
            {
                ITypeSymbol current = stack.Pop();
                if (current.ToDisplayString() == fullInterfaceName)
                {
                    return true;
                }
                else
                {
                    foreach (ITypeSymbol interfaceType in current.AllInterfaces)
                    {
                        stack.Push(interfaceType);
                    }

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
