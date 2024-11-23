using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Simulation.Generator
{
    public class SymbolsMap
    {
        private readonly Dictionary<SyntaxNode, HashSet<ISymbol>> map;
        private readonly Dictionary<string, HashSet<ITypeSymbol>> nameToType;

        public SymbolsMap()
        {
            map = new();
            nameToType = new();
        }

        public void Add(SyntaxNode node, ISymbol symbol)
        {
            if (!map.TryGetValue(node, out HashSet<ISymbol> symbols))
            {
                symbols = [];
                map.Add(node, symbols);
            }

            symbols.Add(symbol);

            if (symbol is ITypeSymbol typeSymbol)
            {
                foreach (SyntaxReference declarationReference in typeSymbol.DeclaringSyntaxReferences)
                {
                    SyntaxNode declaration = declarationReference.GetSyntax();

                }

                if (!nameToType.TryGetValue(typeSymbol.Name, out HashSet<ITypeSymbol> typeSymbols))
                {
                    typeSymbols = [];
                    nameToType.Add(typeSymbol.Name, typeSymbols);
                }

                typeSymbols.Add(typeSymbol);

                string fullName = typeSymbol.ToDisplayString();
                if (!nameToType.TryGetValue(fullName, out typeSymbols))
                {
                    typeSymbols = [];
                    nameToType.Add(fullName, typeSymbols);
                }

                typeSymbols.Add(typeSymbol);
            }
        }

        public bool TryGet(SyntaxNode node, out ICollection<ISymbol> typeSymbols)
        {
            if (map.TryGetValue(node, out HashSet<ISymbol> symbols))
            {
                typeSymbols = symbols;
                return true;
            }

            typeSymbols = [];
            return false;
        }

        public bool TryGetType(string name, out ICollection<ITypeSymbol> type)
        {
            if (nameToType.TryGetValue(name, out HashSet<ITypeSymbol> typeSymbols))
            {
                type = typeSymbols;
                return true;
            }

            type = [];
            return false;
        }
    }
}