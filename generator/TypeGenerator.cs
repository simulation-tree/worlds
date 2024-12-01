using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Worlds.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class TypeGenerator : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();
        private static readonly SourceBuilder debug = new();
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
            source.Clear();
            source.AppendLine($"namespace {Namespace}");
            source.BeginGroup();
            {
                source.AppendLine($"public static partial class {TypeName}");
                source.BeginGroup();
                {
                    source.AppendLine($"static {TypeName}()");
                    source.BeginGroup();
                    {
                        HashSet<ITypeSymbol> componentTypes = [];
                        HashSet<ITypeSymbol> arrayTypes = [];
                        CollectTypeSymbols(compilation, componentTypes, arrayTypes);

                        //foreach (string line in debug.Lines)
                        //{
                        //    source.AppendLine($"//{line}");
                        //}

                        foreach (ITypeSymbol componentType in componentTypes)
                        {
                            AppendComponentTypeRegistration(componentType);
                        }

                        foreach (ITypeSymbol arrayType in arrayTypes)
                        {
                            AppendArrayTypeRegistration(arrayType);
                        }
                    }
                    source.EndGroup();
                }
                source.EndGroup();
            }
            source.EndGroup();
            return source.ToString();
        }

        private static void CollectTypeSymbols(Compilation compilation, HashSet<ITypeSymbol> componentTypes, HashSet<ITypeSymbol> arrayTypes)
        {
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(tree);
                TypeDeclarationsWalker walker = new(semanticModel);
                walker.Visit(tree.GetRoot());
                debug.AppendLine($"SyntaxTree: {tree.FilePath}");
                foreach (ITypeSymbol type in walker.types)
                {
                    debug.AppendLine($"Type: {type.ToDisplayString()}");
                    ImmutableArray<AttributeData> attributes = type.GetAttributes();
                    foreach (AttributeData attribute in attributes)
                    {
                        if (attribute.AttributeClass?.ToDisplayString() == "Worlds.ComponentAttribute")
                        {
                            componentTypes.Add(type);
                        }
                        else if (attribute.AttributeClass?.ToDisplayString() == "Worlds.ArrayAttribute")
                        {
                            arrayTypes.Add(type);
                        }
                    }
                }
            }

            foreach (MetadataReference assemblyReference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(assemblyReference) is IAssemblySymbol assemblySymbol)
                {
                    debug.AppendLine($"Assembly: {assemblySymbol.Name}");
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
                            debug.AppendLine($"Type: {typeSymbol.ToDisplayString()}");
                            ImmutableArray<AttributeData> attributes = typeSymbol.GetAttributes();
                            foreach (AttributeData attribute in attributes)
                            {
                                if (attribute.AttributeClass?.ToDisplayString() == "Worlds.ComponentAttribute")
                                {
                                    componentTypes.Add(typeSymbol);
                                }
                                else if (attribute.AttributeClass?.ToDisplayString() == "Worlds.ArrayAttribute")
                                {
                                    arrayTypes.Add(typeSymbol);
                                }
                            }

                            foreach (ISymbol member in typeSymbol.GetMembers())
                            {
                                stack.Push(member);
                            }
                        }
                    }
                }
            }
        }

        private static void AppendComponentTypeRegistration(ITypeSymbol componentType)
        {
            source.AppendLine($"ComponentType.Register<{GetFullTypeName(componentType)}>();");
        }

        private static void AppendArrayTypeRegistration(ITypeSymbol arrayType)
        {
            source.AppendLine($"ArrayType.Register<{GetFullTypeName(arrayType)}>();");
        }

        private static string GetFullTypeName(ITypeSymbol type)
        {
            return type.ToDisplayString();
        }
    }
}