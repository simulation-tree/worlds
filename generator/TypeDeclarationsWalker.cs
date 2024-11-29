using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Worlds.Generator
{
    public class TypeDeclarationsWalker : CSharpSyntaxWalker
    {
        public readonly HashSet<ITypeSymbol> types = [];
        public readonly SemanticModel semanticModel;

        public TypeDeclarationsWalker(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            ITypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(node);
            if (typeSymbol is not null)
            {
                types.Add(typeSymbol);
            }

            base.VisitStructDeclaration(node);
        }
    }
}