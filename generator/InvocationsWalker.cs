using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Simulation.Generator
{
    public class InvocationsWalker : CSharpSyntaxWalker
    {
        public readonly SemanticModel semanticModel;
        public readonly Dictionary<MethodDeclarationSyntax, List<InvocationExpressionSyntax>> invocations = [];

        public InvocationsWalker(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(node);
            IMethodSymbol[] symbols;
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                symbols = [methodSymbol];
            }
            else
            {
                symbols = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().ToArray();
            }

            foreach (ISymbol candidate in symbols)
            {
                if (candidate is IMethodSymbol candidateMethodSymbol)
                {
                    foreach (SyntaxReference declaration in candidateMethodSymbol.DeclaringSyntaxReferences)
                    {
                        if (declaration.GetSyntax() is MethodDeclarationSyntax methodDeclaration)
                        {
                            if (!invocations.TryGetValue(methodDeclaration, out List<InvocationExpressionSyntax> list))
                            {
                                list = new();
                                invocations.Add(methodDeclaration, list);
                            }

                            list.Add(node);
                        }
                    }
                }
            }

            base.VisitInvocationExpression(node);
        }
    }
}