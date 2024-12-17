using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

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
        private const string TypeAttributeFullName = "Worlds.TypeAttribute";
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
                        HashSet<ITypeSymbol> types = [];
                        CollectTypeSymbols(compilation, componentTypes, arrayTypes, types);

                        foreach (ITypeSymbol componentType in componentTypes)
                        {
                            AppendComponentTypeRegistration(componentType);
                        }

                        foreach (ITypeSymbol arrayType in arrayTypes)
                        {
                            AppendArrayTypeRegistration(arrayType);
                        }

                        foreach (ITypeSymbol type in types)
                        {
                            AppendLayoutRegistration(type);
                        }
                    }
                    source.EndGroup();
                    source.AppendLine();
                    source.AppendLine("private static void RegisterComponentType<T>() where T : unmanaged");
                    source.BeginGroup();
                    {
                        source.AppendLine($"{ComponentTypeName}.Register<T>();");
                    }
                    source.EndGroup();
                    source.AppendLine();
                    source.AppendLine("private static void RegisterArrayType<T>() where T : unmanaged");
                    source.BeginGroup();
                    {
                        source.AppendLine($"{ArrayTypeName}.Register<T>();");
                    }
                    source.EndGroup();
                }
                source.EndGroup();
            }
            source.EndGroup();
            return source.ToString();
        }

        private static void CollectTypeSymbols(Compilation compilation, HashSet<ITypeSymbol> componentTypes, HashSet<ITypeSymbol> arrayTypes, HashSet<ITypeSymbol> types)
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
                            types.Add(type);
                        }
                        else if (attribute.AttributeClass?.ToDisplayString() == ArrayAttributeFullName)
                        {
                            arrayTypes.Add(type);
                            types.Add(type);
                        }
                        else if (attribute.AttributeClass?.ToDisplayString() == TypeAttributeFullName)
                        {
                            types.Add(type);
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
                                    types.Add(typeSymbol);
                                }
                                else if (attribute.AttributeClass?.ToDisplayString() == ArrayAttributeFullName)
                                {
                                    arrayTypes.Add(typeSymbol);
                                    types.Add(typeSymbol);
                                }
                                else if (attribute.AttributeClass?.ToDisplayString() == TypeAttributeFullName)
                                {
                                    types.Add(typeSymbol);
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
            source.AppendLine($"RegisterComponentType<{GetFullTypeName(componentType)}>();");
        }

        private static void AppendArrayTypeRegistration(ITypeSymbol arrayType)
        {
            source.AppendLine($"RegisterArrayType<{GetFullTypeName(arrayType)}>();");
        }

        private static void AppendLayoutRegistration(ITypeSymbol type)
        {
            StringBuilder variables = new();
            foreach (ISymbol typeMember in type.GetMembers())
            {
                if (typeMember is IFieldSymbol field)
                {
                    variables.Append("new(\"");
                    variables.Append(field.Name);
                    variables.Append("\", (ushort)Unmanaged.TypeInfo<");
                    variables.Append(GetFullTypeName(field.Type));
                    variables.Append(">.size, typeof(");
                    variables.Append(GetFullTypeName(field.Type));
                    variables.Append(")), ");
                }
            }

            if (variables.Length > 0)
            {
                variables.Length -= 2;
            }

            source.AppendLine($"TypeLayout.Register<{GetFullTypeName(type)}>(\"{type.Name}\", [{variables}]);");
        }

        private static string GetFullTypeName(ITypeSymbol type)
        {
            return type.ToDisplayString();
        }
    }
}