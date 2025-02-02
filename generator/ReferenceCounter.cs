using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Worlds.Generator
{
    public class ReferenceCounter : CSharpSyntaxWalker
    {
        public readonly StructDeclarationSyntax structDeclaration;
        public readonly ITypeSymbol typeSymbol;
        public readonly SemanticModel semanticModel;

        public ReferenceCounter(StructDeclarationSyntax structDeclaration, ITypeSymbol typeSymbol, SemanticModel semanticModel)
        {
            this.structDeclaration = structDeclaration;
            this.typeSymbol = typeSymbol;
            this.semanticModel = semanticModel;
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            base.VisitMemberAccessExpression(node);
        }
    }
}