using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Threading;
using Types;

namespace Worlds
{
    [Generator(LanguageNames.CSharp)]
    public class SchemaBankGenerator : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();
        public const string TypeName = "SchemaBank";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ITypeSymbol?> types = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            IncrementalValueProvider<Compilation> compilation = context.CompilationProvider;
            context.RegisterSourceOutput(types.Collect().Combine(compilation), Generate);
        }

        private void Generate(SourceProductionContext context, (ImmutableArray<ITypeSymbol?> types, Compilation compilation) input)
        {
            if (input.types.Length > 0)
            {
                context.AddSource($"{TypeName}.generated.cs", Generate(input.types, input.compilation));
            }
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.StructDeclaration);
        }

        private static ITypeSymbol? Transform(GeneratorSyntaxContext context, CancellationToken token)
        {
            StructDeclarationSyntax node = (StructDeclarationSyntax)context.Node;
            SemanticModel semanticModel = context.SemanticModel;
            ITypeSymbol? type = semanticModel.GetDeclaredSymbol(node);
            if (type is null)
            {
                return null;
            }

            if (type is INamedTypeSymbol namedType)
            {
                if (namedType.IsGenericType)
                {
                    return null;
                }
            }

            if (type.IsRefLikeType)
            {
                return null;
            }

            if (type.DeclaredAccessibility != Accessibility.Public && type.DeclaredAccessibility != Accessibility.Internal)
            {
                return null;
            }

            if (type.IsUnmanaged())
            {
                if (type.HasAttribute("Worlds.ComponentAttribute"))
                {
                    return type;
                }
                else if (type.HasAttribute("Worlds.ArrayElementAttribute"))
                {
                    return type;
                }
                else if (type.HasAttribute("Worlds.TagAttribute"))
                {
                    return type;
                }
            }

            return null;
        }

        public static string Generate(ImmutableArray<ITypeSymbol?> types, Compilation compilation)
        {
            string? assemblyName = compilation.AssemblyName;
            source.Clear();
            source.AppendLine("using Types;");
            source.AppendLine("using Unmanaged;");
            source.AppendLine("using Worlds;");
            source.AppendLine("using Worlds.Functions;");
            source.AppendLine();

            if (assemblyName is not null)
            {
                source.Append("namespace ");
                source.AppendLine(assemblyName);
                source.BeginGroup();
            }

            source.Append("public readonly struct ");
            source.Append(TypeName);
            source.Append(" : ISchemaBank");
            source.AppendLine();

            source.BeginGroup();
            {
                source.AppendLine("void ISchemaBank.Load(RegisterDataType function)");
                source.BeginGroup();
                {
                    foreach (ITypeSymbol? type in types)
                    {
                        if (type is not null)
                        {
                            AppendRegister(type);
                        }
                    }
                }
                source.EndGroup();
            }
            source.EndGroup();

            if (assemblyName is not null)
            {
                source.EndGroup();
            }

            return source.ToString();
        }

        private static void AppendRegister(ITypeSymbol type)
        {
            if (type.HasAttribute("Worlds.ComponentAttribute"))
            {
                source.Append("function.Invoke(TypeRegistry.Get<");
                source.Append(type.GetFullTypeName());
                source.Append(">(), DataType.Kind.Component);");
                source.AppendLine();
            }

            if (type.HasAttribute("Worlds.ArrayElementAttribute"))
            {
                source.Append("function.Invoke(TypeRegistry.Get<");
                source.Append(type.GetFullTypeName());
                source.Append(">(), DataType.Kind.ArrayElement);");
                source.AppendLine();
            }

            if (type.HasAttribute("Worlds.TagAttribute"))
            {
                source.Append("function.Invoke(TypeRegistry.Get<");
                source.Append(type.GetFullTypeName());
                source.Append(">(), DataType.Kind.Tag);");
                source.AppendLine();
            }
        }
    }
}