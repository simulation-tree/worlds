using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Worlds.TypeTableGenerator
{
    public static class SharedFunctions
    {
        public const string Namespace = "Worlds";
        public const string ComponentAttribute = "Worlds.ComponentAttribute";
        public const string ArrayElementAttribute = "Worlds.ArrayElementAttribute";
        public const string TagAttribute = "Worlds.TagAttribute";
        public const string ComponentType = "Worlds.ComponentType";
        public const string ArrayElementType = "Worlds.ArrayElementType";
        public const string TagType = "Worlds.TagType";
        public const string TypeLayout = "Worlds.TypeLayout";

        public static void CollectTypeSymbols(this Compilation compilation, HashSet<ITypeSymbol> componentTypes, HashSet<ITypeSymbol> arrayElementTypes, HashSet<ITypeSymbol> tagTypes)
        {
            CollectTypeSymbols(compilation, componentTypes, arrayElementTypes, tagTypes, []);
        }

        public static void CollectTypeSymbols(this Compilation compilation, HashSet<ITypeSymbol> types)
        {
            CollectTypeSymbols(compilation, [], [], [], types);
        }

        public static void CollectTypeSymbols(this Compilation compilation, HashSet<ITypeSymbol> componentTypes, HashSet<ITypeSymbol> arrayElementTypes, HashSet<ITypeSymbol> tagTypes, HashSet<ITypeSymbol> types)
        {
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(tree);
                TypeDeclarationsWalker walker = new(semanticModel);
                walker.Visit(tree.GetRoot());
                foreach (ITypeSymbol type in walker.types)
                {
                    CheckType(type);
                }
            }

            foreach (MetadataReference assemblyReference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(assemblyReference) is IAssemblySymbol assemblySymbol)
                {
                    Stack<ISymbol> stack = new();
                    stack.Push(assemblySymbol.GlobalNamespace);
                    while (stack.Count > 0)
                    {
                        ISymbol current = stack.Pop();
                        if (current is INamespaceSymbol namespaceSymbol)
                        {
                            foreach (ISymbol member in namespaceSymbol.GetNamespaceMembers())
                            {
                                stack.Push(member);
                            }

                            foreach (ISymbol member in namespaceSymbol.GetTypeMembers())
                            {
                                stack.Push(member);
                            }
                        }
                        else if (current is ITypeSymbol type)
                        {
                            if (type.DeclaredAccessibility != Accessibility.Internal)
                            {
                                CheckType(type);

                                foreach (ISymbol member in type.GetMembers())
                                {
                                    stack.Push(member);
                                }
                            }
                        }
                    }
                }
            }

            void CheckType(ITypeSymbol type)
            {
                if (type.IsRefLikeType)
                {
                    return;
                }

                if (type.DeclaredAccessibility == Accessibility.Private || type.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                {
                    return;
                }

                bool isGeneric = type is INamedTypeSymbol namedType && namedType.IsGenericType;
                if (isGeneric)
                {
                    return;
                }

                if (IsUnmanaged(type))
                {
                    ImmutableArray<AttributeData> attributes = type.GetAttributes();
                    foreach (AttributeData attribute in attributes)
                    {
                        if (attribute.AttributeClass?.ToDisplayString() == ComponentAttribute)
                        {
                            componentTypes.Add(type);
                        }
                        else if (attribute.AttributeClass?.ToDisplayString() == ArrayElementAttribute)
                        {
                            arrayElementTypes.Add(type);
                        }
                        else if (attribute.AttributeClass?.ToDisplayString() == TagAttribute)
                        {
                            tagTypes.Add(type);
                        }

                        types.Add(type);
                    }
                }
            }
        }

        public static bool IsUnmanaged(this ITypeSymbol type)
        {
            //check if the entire type is a true value type and doesnt contain references
            Stack<ITypeSymbol> stack = new();
            stack.Push(type);

            while (stack.Count > 0)
            {
                ITypeSymbol current = stack.Pop();
                if (current.IsReferenceType)
                {
                    return false;
                }

                foreach (IFieldSymbol field in GetFields(current))
                {
                    stack.Push(field.Type);
                }
            }

            return true;
        }

        public static IEnumerable<IFieldSymbol> GetFields(this ITypeSymbol type)
        {
            foreach (ISymbol typeMember in type.GetMembers())
            {
                if (typeMember is IFieldSymbol field)
                {
                    if (field.HasConstantValue || field.IsStatic)
                    {
                        continue;
                    }

                    yield return field;
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
            else
            {
                return symbol.ToDisplayString();
            }
        }
    }
}