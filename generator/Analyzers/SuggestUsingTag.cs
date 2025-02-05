using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Types;

namespace Worlds.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SuggestUsingTag : DiagnosticAnalyzer
    {
        private const string ID = "W0001";
        private const string Title = "Empty component type can be a tag";
        private const string MessageFormat = "Component type `{0}` is empty, and can instead be a tag to reduce the amount of components registed with a schema";
        private const string Category = "Types";
        private const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        private static readonly DiagnosticDescriptor rule;

        static SuggestUsingTag()
        {
            rule = new(ID, Title, MessageFormat, Category, Severity, true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [rule];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
            if (!type.HasAttribute("Worlds.ComponentAttribute"))
            {
                return;
            }

            int fields = 0;
            foreach (ISymbol member in type.GetMembers())
            {
                if (member.Kind == SymbolKind.Field)
                {
                    fields++;
                }
            }

            if (fields == 0)
            {
                if (!type.DeclaringSyntaxReferences.IsEmpty)
                {
                    SyntaxNode node = type.DeclaringSyntaxReferences[0].GetSyntax();
                    if (node is not StructDeclarationSyntax structNode)
                    {
                        return;
                    }

                    if (!structNode.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        try
                        {
                            Location location = structNode.Identifier.GetLocation();
                            context.ReportDiagnostic(Diagnostic.Create(rule, location, type.Name));
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
    }
}