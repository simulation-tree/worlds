using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Types;

namespace Worlds.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class InheritingEntityGenerator : IIncrementalGenerator
    {
        private static readonly SymbolEqualityComparer symbolComparer = SymbolEqualityComparer.Default;
        private const string InputVariableName = "input";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            var structTypes = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            var input = structTypes.Combine(context.CompilationProvider);
            context.RegisterSourceOutput(input, Generate);
        }

        private static void Generate(SourceProductionContext context, ((EntityType? input, Diagnostic? diagnostic) result, Compilation compilation) data)
        {
            if (data.result.input is not null)
            {
                Generate(context, data.result.input, data.compilation);
            }

            if (data.result.diagnostic is not null)
            {
                context.ReportDiagnostic(data.result.diagnostic);
            }
        }

        private static void Generate(SourceProductionContext context, EntityType type, Compilation compilation)
        {
            SourceBuilder builder = new();
            builder.AppendLine("using Worlds;");
            builder.AppendLine("using System;");
            builder.AppendLine("using Unmanaged;");
            builder.AppendLine();

            //inside a namespace if needed
            INamespaceSymbol? containedNamespace = type.typeSymbol.ContainingNamespace;
            if (containedNamespace is not null)
            {
                builder.Append("namespace ");
                builder.Append(containedNamespace.ToDisplayString());
                builder.AppendLine();
                builder.BeginGroup();
            }

            //inside a type if needed
            INamedTypeSymbol? containingType = type.typeSymbol.ContainingType;
            if (containingType is not null)
            {
                builder.Append("public ");
                if (containingType.IsReadOnly)
                {
                    builder.Append("readonly ");
                }

                builder.Append("partial ");
                if (containingType.IsReferenceType)
                {
                    builder.Append("class ");
                }
                else
                {
                    builder.Append("struct ");
                }

                builder.Append(containingType.Name);
                builder.AppendLine();
                builder.BeginGroup();
            }

            //write the source
            string source = EntityTemplate.Source;
            string accessors = "public";
            if (type.typeSymbol.IsReadOnly)
            {
                accessors += " readonly";
            }

            string typeName = type.typeSymbol.Name;
            source = source.Replace("{{Accessors}}", accessors);
            source = source.Replace("{{TypeName}}", typeName);
            string[] lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                ref string line = ref lines[i];
                if (line.Length > 0)
                {
                    line = "    " + line;
                }
            }

            source = string.Join("\n", lines);
            source = source.TrimStart();
            builder.Append(source);
            builder.AppendLine();

            if (containingType is not null)
            {
                builder.EndGroup();
            }

            if (containedNamespace is not null)
            {
                builder.EndGroup();
            }

            context.AddSource($"{type.typeSymbol.Name}.generated.cs", builder.ToString());
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.StructDeclaration);
        }

        private static (EntityType? result, Diagnostic? diagnostic) Transform(GeneratorSyntaxContext syntaxContext, CancellationToken token)
        {
            StructDeclarationSyntax node = (StructDeclarationSyntax)syntaxContext.Node;
            ITypeSymbol? typeSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(node);
            if (typeSymbol is null)
            {
                return default;
            }

            if (typeSymbol.HasInterface("Worlds.IEntity"))
            {
                if (!node.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    string message = $"The type {typeSymbol.Name} must be partial when inheriting another type";
                    string title = "Missing partial keyword";
                    Diagnostic diagnostic = Diagnostic.Create("T0002", "Inheritance", message, DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0, false, title, location: node.GetLocation());
                    return (null, diagnostic);
                }

                if (typeSymbol.ContainingType is not null)
                {
                    //todo: check if containing type is also partial, and 
                    //emit a diagnostic error if its not
                }

                string fullTypeName = typeSymbol.GetFullTypeName();
                return (new(node, typeSymbol, fullTypeName), null);
            }

            return default;
        }
    }
}