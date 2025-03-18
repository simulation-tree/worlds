using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Worlds
{
    public static class CompilationExtensions
    {
        /// <summary>
        /// Iterates through all types found in every syntax trees.
        /// </summary>
        public static IReadOnlyCollection<ITypeSymbol> GetAllTypes(this Compilation compilation, bool includingReferencedProjects = true)
        {
            Stack<ISymbol> symbolStack = new();
            HashSet<ITypeSymbol> types = [];
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(tree);
                TypeDeclarationsWalker walker = new(semanticModel);
                walker.Visit(tree.GetRoot());
                foreach (ITypeSymbol type in walker.types)
                {
                    types.Add(type);
                }
            }

            if (includingReferencedProjects)
            {
                foreach (MetadataReference assemblyReference in compilation.References)
                {
                    if (compilation.GetAssemblyOrModuleSymbol(assemblyReference) is IAssemblySymbol assemblySymbol)
                    {
                        symbolStack.Push(assemblySymbol.GlobalNamespace);
                        while (symbolStack.Count > 0)
                        {
                            ISymbol current = symbolStack.Pop();
                            if (current is INamespaceSymbol namespaceSymbol)
                            {
                                foreach (ISymbol member in namespaceSymbol.GetNamespaceMembers())
                                {
                                    symbolStack.Push(member);
                                }

                                foreach (ISymbol member in namespaceSymbol.GetTypeMembers())
                                {
                                    symbolStack.Push(member);
                                }
                            }
                            else if (current is ITypeSymbol type)
                            {
                                types.Add(type);
                                foreach (ISymbol member in type.GetMembers())
                                {
                                    symbolStack.Push(member);
                                }
                            }
                            else if (current is IFieldSymbol field)
                            {
                                types.Add(field.Type);
                            }
                            else if (current is IMethodSymbol method)
                            {
                                types.Add(method.ReturnType);
                                foreach (IParameterSymbol parameter in method.Parameters)
                                {
                                    types.Add(parameter.Type);
                                }
                            }
                            else if (current is IPropertySymbol property)
                            {
                                types.Add(property.Type);
                            }
                        }
                    }
                }
            }

            return types;
        }
    }
}