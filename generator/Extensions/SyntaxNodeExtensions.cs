using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Worlds
{
    public static class SyntaxNodeExtensions
    {
        public static bool InheritsFrom(this TypeDeclarationSyntax typeDeclaration, string baseTypeName)
        {
            if (typeDeclaration.BaseList is not null)
            {
                foreach (BaseTypeSyntax baseTypeSyntax in typeDeclaration.BaseList.Types)
                {
                    if (baseTypeSyntax.Type is IdentifierNameSyntax identifierName)
                    {
                        if (identifierName.Identifier.Text == baseTypeName)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}