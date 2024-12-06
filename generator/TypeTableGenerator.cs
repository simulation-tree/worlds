using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Worlds.TypeTableGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class TypeTableGenerator : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();
        private const string TypeName = "TypeTable";
        private const string Namespace = "Worlds";
        private const string ComponentAttributeFullName = "Worlds.ComponentAttribute";
        private const string ArrayAttributeFullName = "Worlds.ArrayAttribute";
        private const string ComponentTypeName = "ComponentType";
        private const string ArrayTypeName = "ArrayType";

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
                source.AppendLine($"internal static partial class {TypeName}");
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
                foreach (ITypeSymbol type in walker.types)
                {
                    ImmutableArray<AttributeData> attributes = type.GetAttributes();
                    foreach (AttributeData attribute in attributes)
                    {
                        if (attribute.AttributeClass?.ToDisplayString() == ComponentAttributeFullName)
                        {
                            componentTypes.Add(type);
                        }
                        else if (attribute.AttributeClass?.ToDisplayString() == ArrayAttributeFullName)
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
                            ImmutableArray<AttributeData> attributes = typeSymbol.GetAttributes();
                            foreach (AttributeData attribute in attributes)
                            {
                                if (attribute.AttributeClass?.ToDisplayString() == ComponentAttributeFullName)
                                {
                                    componentTypes.Add(typeSymbol);
                                }
                                else if (attribute.AttributeClass?.ToDisplayString() == ArrayAttributeFullName)
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
            source.AppendLine($"{ComponentTypeName}.Register<{GetFullTypeName(componentType)}>();");
        }

        private static void AppendArrayTypeRegistration(ITypeSymbol arrayType)
        {
            source.AppendLine($"{ArrayTypeName}.Register<{GetFullTypeName(arrayType)}>();");
        }

        private static string GetFullTypeName(ITypeSymbol type)
        {
            return type.ToDisplayString();
        }
    }
}