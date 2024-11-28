using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Worlds.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class TypeGenerator : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();
        private static readonly SourceBuilder console = new();
        private const string TypeName = "TypeTable";
        private const string Namespace = "Worlds";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, Generate);
        }

        private static void Generate(SourceProductionContext context, Compilation compilation)
        {
            context.AddSource($"{TypeName}.generated.cs", Generate(compilation));
        }

        public static string Generate(Compilation compilation)
        {
            compilation = AppendReferencedSyntaxTrees(compilation);

            source.Clear();
            source.AppendLine($"namespace {Namespace}");
            source.BeginGroup();
            {
                source.AppendLine($"public static partial class {TypeName}");
                source.BeginGroup();
                {
                    HashSet<string> componentTypeNames = [];
                    HashSet<string> arrayTypeNames = [];
                    source.AppendLine("/*");
                    try
                    {
                        SymbolsMap symbols = CollectSymbols(compilation);
                        foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
                        {
                            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
                            SyntaxNode root = syntaxTree.GetRoot();

                            InvocationsWalker invocationsWalker = new(semanticModel);
                            invocationsWalker.Visit(root);

                            TypeUsagesWalker walker = new(semanticModel, invocationsWalker.invocations, symbols, source);
                            walker.Visit(root);

                            componentTypeNames.UnionWith(walker.componentTypeNames);
                            arrayTypeNames.UnionWith(walker.arrayTypeNames);
                        }
                    }
                    catch (Exception e)
                    {
                        source.AppendLine(e.Message);
                        source.AppendLine(e.StackTrace);
                    }
                    source.AppendLine("*/");
                    source.AppendLine($"static {TypeName}()");
                    source.BeginGroup();
                    {
                        //foreach (string line in console.Lines)
                        //{
                        //    string sanitizedLine = line.Replace('\\', '/');
                        //    source.AppendLine($"System.Console.WriteLine(\"{sanitizedLine}\");");
                        //}

                        foreach (string componentTypeName in componentTypeNames)
                        {
                            AppendComponentTypeRegistration(componentTypeName);
                        }

                        foreach (string arrayTypeName in arrayTypeNames)
                        {
                            AppendArrayTypeRegistration(arrayTypeName);
                        }
                    }
                    source.EndGroup();
                }
                source.EndGroup();
            }
            source.EndGroup();
            return source.ToString();
        }

        private static Compilation AppendReferencedSyntaxTrees(Compilation compilation)
        {
            HashSet<SyntaxTree> added = [];
            foreach (MetadataReference assemblyReference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(assemblyReference) is IAssemblySymbol assemblySymbol)
                {
                    console.AppendLine($"checking reference {assemblyReference.Display}");
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
                        else if (current is ITypeSymbol typeSymbol)
                        {
                            foreach (ISymbol member in typeSymbol.GetMembers())
                            {
                                stack.Push(member);
                            }

                            foreach (SyntaxReference declarationReference in typeSymbol.DeclaringSyntaxReferences)
                            {
                                SyntaxNode declaration = declarationReference.GetSyntax();
                                SyntaxTree tree = declaration.SyntaxTree;
                                if (added.Add(tree))
                                {
                                    console.AppendLine($"{tree.FilePath}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    console.AppendLine($"assembly {assemblyReference.Display} not found");
                }
            }

            console.AppendLine($"{added.Count} trees added");
            return compilation.AddSyntaxTrees(added);
        }

        private static SymbolsMap CollectSymbols(Compilation compilation)
        {
            SymbolsMap symbols = new();
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
                        else if (current is ITypeSymbol typeSymbol)
                        {
                            foreach (ISymbol member in typeSymbol.GetMembers())
                            {
                                stack.Push(member);
                            }

                            foreach (SyntaxReference declarationReference in typeSymbol.DeclaringSyntaxReferences)
                            {
                                SyntaxNode declaration = declarationReference.GetSyntax();
                                symbols.Add(declaration, typeSymbol);
                            }
                        }
                        else if (current is IMethodSymbol methodSymbol)
                        {
                            foreach (SyntaxReference declaration in methodSymbol.DeclaringSyntaxReferences)
                            {
                                SyntaxNode node = declaration.GetSyntax();
                                symbols.Add(node, methodSymbol);
                            }
                        }
                        else if (current is IFieldSymbol fieldSymbol)
                        {
                            foreach (SyntaxReference declaration in fieldSymbol.DeclaringSyntaxReferences)
                            {
                                SyntaxNode node = declaration.GetSyntax();
                                symbols.Add(node, fieldSymbol);
                            }
                        }
                        else if (current is IPropertySymbol propertySymbol)
                        {
                            foreach (SyntaxReference declaration in propertySymbol.DeclaringSyntaxReferences)
                            {
                                SyntaxNode node = declaration.GetSyntax();
                                symbols.Add(node, propertySymbol);
                            }
                        }
                    }
                }
            }

            return symbols;
        }

        private static void AppendComponentTypeRegistration(string fullTypeName)
        {
            source.AppendLine($"ComponentType.Register<{fullTypeName}>();");
        }

        private static void AppendArrayTypeRegistration(string fullTypeName)
        {
            source.AppendLine($"ArrayType.Register<{fullTypeName}>();");
        }
    }
}