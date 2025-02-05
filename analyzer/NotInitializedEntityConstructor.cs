using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Worlds.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NotInitializedEntityConstructor : DiagnosticAnalyzer
    {
        private const string WorldFieldName = "world";
        private const string ValueFieldName = "value";
        private const string ID = "W0002";
        private const string Title = "Entity not initialized";
        private const string Format = "Entity constructor in type `{0}` doesn't initialize the `{1}` field";
        private const string Category = "Types";
        private const DiagnosticSeverity Severity = DiagnosticSeverity.Error;

        private static readonly DiagnosticDescriptor worldAssignmentRule;
        private static readonly DiagnosticDescriptor valueAssignmentRule;

        static NotInitializedEntityConstructor()
        {
            worldAssignmentRule = new(ID, Title, Format, Category, Severity, true);
            valueAssignmentRule = new(ID, Title, Format, Category, Severity, true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [worldAssignmentRule, valueAssignmentRule];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.ConstructorDeclaration);
        }

        private void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is ConstructorDeclarationSyntax constructor)
            {
                //ignore default constructors as theyre marked obsolete
                if (constructor.ParameterList.Parameters.Count == 0)
                {
                    return;
                }

                if (constructor.Parent is TypeDeclarationSyntax typeDeclaration)
                {
                    //must inherit Worlds.IEntity
                    if (!typeDeclaration.InheritsFrom("IEntity"))
                    {
                        return;
                    }

                    bool worldAssigned = false;
                    bool valueAssigned = false;
                    foreach (SyntaxNode descendant in constructor.DescendantNodes())
                    {
                        if (descendant is MemberAccessExpressionSyntax memberAccess)
                        {
                            if (memberAccess.Expression is ThisExpressionSyntax thisExpression)
                            {
                                if (memberAccess.Name.Identifier.Text == "world")
                                {
                                    worldAssigned = true;
                                }
                                else if (memberAccess.Name.Identifier.Text == "value")
                                {
                                    valueAssigned = true;
                                }
                            }
                        }
                    }

                    if (!worldAssigned)
                    {
                        Location location = constructor.GetLocation();
                        string typeName = typeDeclaration.Identifier.ToString();
                        Diagnostic diagnostic = Diagnostic.Create(worldAssignmentRule, location, typeName, WorldFieldName);
                        context.ReportDiagnostic(diagnostic);
                    }

                    if (!valueAssigned)
                    {
                        Location location = constructor.GetLocation();
                        string typeName = typeDeclaration.Identifier.ToString();
                        Diagnostic diagnostic = Diagnostic.Create(valueAssignmentRule, location, typeName, ValueFieldName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}