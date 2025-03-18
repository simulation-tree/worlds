using Microsoft.CodeAnalysis;

namespace Worlds
{
    public class EntityType
    {
        public readonly SyntaxNode syntaxNode;
        public readonly ITypeSymbol typeSymbol;
        public readonly string fullTypeName;

        public EntityType(SyntaxNode syntaxNode, ITypeSymbol symbol, string fullTypeName)
        {
            this.syntaxNode = syntaxNode;
            this.typeSymbol = symbol;
            this.fullTypeName = fullTypeName;
        }
    }
}