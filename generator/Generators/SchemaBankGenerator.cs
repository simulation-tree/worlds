using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Types;

namespace Worlds
{
    [Generator(LanguageNames.CSharp)]
    public class SchemaBankGenerator : IIncrementalGenerator
    {
        public const string TypeNameFormat = "{0}SchemaBank";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ITypeSymbol?> types = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            context.RegisterSourceOutput(types.Collect(), Generate);
        }

        private void Generate(SourceProductionContext context, ImmutableArray<ITypeSymbol?> typesArray)
        {
            List<ITypeSymbol> types = new();
            foreach (ITypeSymbol? type in typesArray)
            {
                if (type is not null)
                {
                    types.Add(type);
                }
            }

            if (types.Count > 0)
            {
                string source = Generate(types, out string typeName);
                context.AddSource($"{typeName}.generated.cs", source);
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

        public static string Generate(IReadOnlyList<ITypeSymbol> types, out string typeName)
        {
            string? assemblyName = types[0].ContainingAssembly?.Name;
            if (assemblyName is not null && assemblyName.EndsWith(".Core"))
            {
                assemblyName = assemblyName.Substring(0, assemblyName.Length - 5);
            }

            SourceBuilder source = new();
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

            typeName = TypeNameFormat.Replace("{0}", assemblyName ?? "");
            typeName = typeName.Replace(".", "");
            source.Append("public readonly struct ");
            source.Append(typeName);
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