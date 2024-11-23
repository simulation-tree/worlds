using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Simulation.Generator
{
    public class TypeUsagesWalker : CSharpSyntaxWalker
    {
        public readonly HashSet<string> componentTypeNames = [];
        public readonly HashSet<string> arrayTypeNames = [];

        private readonly SourceBuilder source;
        private readonly Dictionary<MethodDeclarationSyntax, List<InvocationExpressionSyntax>> invocations = [];
        private readonly SemanticModel semanticModel;
        private readonly SymbolsMap symbolsMap;
        private readonly HashSet<ITypeSymbol> visited = [];

        public TypeUsagesWalker(SemanticModel semanticModel, Dictionary<MethodDeclarationSyntax, List<InvocationExpressionSyntax>> invocations, SymbolsMap symbolsMap, SourceBuilder log)
        {
            this.semanticModel = semanticModel;
            this.invocations = invocations;
            this.symbolsMap = symbolsMap;
            this.source = log;
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            VisitObjectCreationExpression(node);
            base.VisitObjectCreationExpression(node);
        }

        public override void VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
        {
            VisitObjectCreationExpression(node);
            base.VisitImplicitObjectCreationExpression(node);
        }

        private void VisitObjectCreationExpression(BaseObjectCreationExpressionSyntax node)
        {
            if (node is ObjectCreationExpressionSyntax objectCreation)
            {
                foreach (SyntaxNode descendant in objectCreation.DescendantNodes())
                {
                    if (descendant is InvocationExpressionSyntax invocationExpression)
                    {
                        VisitInvocationExpression(invocationExpression);
                    }
                }
            }
            else if (node is ImplicitObjectCreationExpressionSyntax implicitCreation)
            {
                foreach (SyntaxNode childNode in implicitCreation.DescendantNodes())
                {
                    if (childNode is InvocationExpressionSyntax invocationExpression)
                    {
                        VisitInvocationExpression(invocationExpression);
                    }
                }
            }
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            foreach (ITypeSymbol symbol in GetSymbols<ITypeSymbol>(node.Declaration.Type))
            {
                VisitVariableDeclaration(node, symbol);
            }

            base.VisitLocalDeclarationStatement(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (ITypeSymbol symbol in GetSymbols<ITypeSymbol>(node.Declaration.Type))
            {
                VisitVariableDeclaration(node, symbol);
            }

            base.VisitFieldDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            foreach (ITypeSymbol symbol in GetSymbols<ITypeSymbol>(node.Type))
            {
                VisitVariableDeclaration(node, symbol);
            }

            base.VisitPropertyDeclaration(node);
        }

        private void VisitVariableDeclaration(SyntaxNode variableNode, ITypeSymbol variableType)
        {
            if (variableType.ContainingNamespace?.ToString() == "Simulation" && variableType.Name.ToString() == "ComponentQuery")
            {
                //source.AppendLine("Is component query");
                foreach (SyntaxNode childNode in variableNode.DescendantNodes())
                {
                    if (childNode is TypeArgumentListSyntax typeList)
                    {
                        foreach (TypeSyntax childTypeNode in typeList.Arguments)
                        {
                            ITypeSymbol[] childTypeSymbols = GetSymbols<ITypeSymbol>(childTypeNode);
                            if (childTypeSymbols.Length > 0)
                            {
                                ITypeSymbol childTypeSymbol = childTypeSymbols[0];
                                if (childTypeSymbol.TypeKind != TypeKind.TypeParameter)
                                {
                                    componentTypeNames.Add(childTypeSymbol.ToDisplayString());
                                }
                            }
                        }
                    }
                }
            }
            else if (variableType.ContainingNamespace?.ToString() == "Simulation" && variableType.Name.ToString() == "Definition")
            {
                //source.AppendLine("Is definition");
                foreach (SyntaxNode childNode in variableNode.DescendantNodes())
                {
                    if (childNode is InvocationExpressionSyntax invocationExpression)
                    {
                        foreach (SyntaxNode grandChild in childNode.DescendantNodes())
                        {
                            if (grandChild is GenericNameSyntax genericName)
                            {
                                string genericNameString = genericName.Identifier.ToString();
                                bool isComponentType = genericNameString.StartsWith("AddComponentType");
                                if (isComponentType || genericNameString.StartsWith("AddArrayType"))
                                {
                                    foreach (SyntaxNode greatGrandChild in grandChild.DescendantNodes())
                                    {
                                        if (greatGrandChild is TypeArgumentListSyntax typeArgumentList)
                                        {
                                            foreach (TypeSyntax type in typeArgumentList.Arguments)
                                            {
                                                if (type is IdentifierNameSyntax typeNameNode)
                                                {
                                                    string typeIdentifier = typeNameNode.Identifier.ToString();
                                                    if (symbolsMap.TryGetType(typeIdentifier, out ICollection<ITypeSymbol>? canditateTypes))
                                                    {
                                                        if (canditateTypes.Count == 1)
                                                        {
                                                            if (isComponentType)
                                                            {
                                                                FoundComponentType(canditateTypes.First());
                                                            }
                                                            else
                                                            {
                                                                FoundArrayType(canditateTypes.First());
                                                            }
                                                        }
                                                        else
                                                        {
                                                            source.AppendLine($"{canditateTypes.Count} possible canditates for {typeIdentifier}");
                                                            source.BeginGroup();
                                                            {
                                                                foreach (ITypeSymbol canditateType in canditateTypes)
                                                                {
                                                                    source.AppendLine($"{canditateType.ToDisplayString()}");
                                                                }
                                                            }
                                                            source.EndGroup();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //source.AppendLine($"??? {typeIdentifier}");
                                                    }
                                                }
                                                else
                                                {
                                                    //source.AppendLine($"??? {type.GetType()} = {type.GetText()}");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //source.AppendLine($"{greatGrandChild.GetType()} = {greatGrandChild.GetText()}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //source.AppendLine($"{grandChild.GetType()} = {grandChild.GetText()}");
                            }
                        }
                    }
                    else
                    {
                        //source.AppendLine($"{childNode.GetType()} = {childNode.GetText()}");
                    }
                }
            }
            else
            {
                if (visited.Add(variableType))
                {
                    //VisitType(variableType);
                }
            }
        }

        private void VisitType(ITypeSymbol typeSymbol)
        {
            source.AppendLine($"Type {typeSymbol.ToDisplayString()}");
            source.BeginGroup();
            {
                foreach (ISymbol typeMember in typeSymbol.GetMembers())
                {
                    if (typeMember is IMethodSymbol methodSymbol)
                    {
                        source.AppendLine($"Method {typeMember} in {typeSymbol}");
                        source.BeginGroup();
                        {
                            foreach (SyntaxReference declarationReference in methodSymbol.DeclaringSyntaxReferences)
                            {
                                SyntaxNode declaration = declarationReference.GetSyntax();
                                foreach (SyntaxNode descendant in declaration.DescendantNodes())
                                {
                                    if (descendant is InvocationExpressionSyntax invocationExpression)
                                    {
                                        VisitInvocationExpression(invocationExpression);
                                    }
                                    else if (descendant is ObjectCreationExpressionSyntax objectCreation)
                                    {
                                        VisitObjectCreationExpression(objectCreation);
                                    }
                                    else if (descendant is ImplicitObjectCreationExpressionSyntax implicitObjectCreation)
                                    {
                                        VisitObjectCreationExpression(implicitObjectCreation);
                                    }
                                    else if (descendant is LocalDeclarationStatementSyntax localDeclaration)
                                    {
                                        TypeSyntax type = localDeclaration.Declaration.Type;
                                        if (symbolsMap.TryGet(type, out ICollection<ISymbol>? typeSymbols))
                                        {
                                            if (typeSymbols.First() is ITypeSymbol declaringType)
                                            {
                                                source.AppendLine($"Local declaration {localDeclaration} in {declaration.GetType()}");
                                                VisitVariableDeclaration(localDeclaration, declaringType);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        source.EndGroup();
                    }
                    else if (typeMember is IFieldSymbol fieldSymbol)
                    {
                        foreach (SyntaxReference declarationReference in fieldSymbol.DeclaringSyntaxReferences)
                        {
                            SyntaxNode declaration = declarationReference.GetSyntax();
                            source.AppendLine($"Field {fieldSymbol} in {declaration.GetType()}");
                            VisitVariableDeclaration(declaration, fieldSymbol.Type);
                        }
                    }
                    else if (typeMember is IPropertySymbol propertySymbol)
                    {
                        foreach (SyntaxReference declarationReference in propertySymbol.DeclaringSyntaxReferences)
                        {
                            SyntaxNode declaration = declarationReference.GetSyntax();
                            source.AppendLine($"Property {propertySymbol} in {declaration.GetType()}");
                            VisitVariableDeclaration(declaration, propertySymbol.Type);
                        }
                    }
                }
            }
            source.EndGroup();
        }

        private T[] GetSymbols<T>(SyntaxNode node) where T : ISymbol
        {
            try
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(node);
                if (symbolInfo.Symbol is T symbol)
                {
                    return [symbol];
                }
                else
                {
                    List<T> list = [];
                    foreach (ISymbol candidateSymbol in symbolInfo.CandidateSymbols)
                    {
                        if (candidateSymbol is T t)
                        {
                            list.Add(t);
                        }
                    }

                    return [.. list];
                }
            }
            catch (Exception ex)
            {
                source.AppendLine($"Error getting symbol for {node.GetType()}: {ex.ToString()}");
                return [];
            }
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            IMethodSymbol[] symbols = GetSymbols<IMethodSymbol>(node);
            if (symbols.Length > 0)
            {
                foreach (IMethodSymbol symbol in symbols)
                {
                    if (!symbol.IsPartialDefinition)
                    {
                        VisitInvocationExpression(node, symbol);
                        break;
                    }
                }
            }
        }

        public void VisitInvocationExpression(InvocationExpressionSyntax node, IMethodSymbol symbol)
        {
            string methodName = symbol.Name;
            string typeName = symbol.ContainingType.ToDisplayString();
            if (typeName == "Simulation.Definition")
            {
                int desiredArgumentCount = node.ArgumentList.Arguments.Count;
                if (methodName == "AddComponentType" || methodName == "AddComponentTypes")
                {
                    if (symbol.Arity > 0 && symbol.Parameters.Length == desiredArgumentCount)
                    {
                        foreach (ITypeSymbol genericType in symbol.TypeArguments)
                        {
                            if (genericType.TypeKind != TypeKind.TypeParameter)
                            {
                                FoundComponentType(genericType);
                            }
                            else
                            {
                                //source.AppendLine($"definition api ??? {node}");
                                TraceGenericUsages(node);
                            }
                        }
                    }
                }
                else if (methodName == "AddArrayType" || methodName == "AddArrayTypes")
                {
                    if (symbol.Arity > 0 && symbol.Parameters.Length == desiredArgumentCount)
                    {
                        foreach (ITypeSymbol genericType in symbol.TypeArguments)
                        {
                            if (genericType.TypeKind != TypeKind.TypeParameter)
                            {
                                FoundArrayType(genericType);
                            }
                            else
                            {
                                //source.AppendLine($"definition api ??? {node}");
                                TraceGenericUsages(node);
                            }
                        }
                    }
                }
            }
            else if (typeName == "Simulation.World" || typeName == "Simulation.Entity" || typeName == "Simulation.Operation" || typeName == "Simulation.Instruction")
            {
                int desiredArgumentCount = node.ArgumentList.Arguments.Count;
                if (methodName == "AddComponent" || methodName == "RemoveComponent" || methodName == "SetComponent" || methodName == "GetComponent" || methodName == "GetComponentRef" || methodName == "ContainsComponent")
                {
                    if (symbol.Arity > 0 && symbol.Parameters.Length == desiredArgumentCount)
                    {
                        foreach (ITypeSymbol genericType in symbol.TypeArguments)
                        {
                            if (genericType.TypeKind != TypeKind.TypeParameter)
                            {
                                FoundComponentType(genericType);
                            }
                            else
                            {
                                //source.AppendLine($"world/entity api ??? {node}");
                                TraceGenericUsages(node);
                            }
                        }
                    }
                }
                else if (methodName == "AddArray" || methodName == "RemoveArray" || methodName == "GetArray" || methodName == "CreateArray" || methodName == "DestroyArray" || methodName == "ContainsArray" || methodName == "SetArrayElement" || methodName == "SetArrayElements" || methodName == "GetArrayLength" || methodName == "ResizeArray")
                {
                    if (symbol.Arity > 0 && symbol.Parameters.Length == desiredArgumentCount)
                    {
                        foreach (ITypeSymbol genericType in symbol.TypeArguments)
                        {
                            if (genericType.TypeKind != TypeKind.TypeParameter)
                            {
                                FoundArrayType(genericType);
                            }
                            else
                            {
                                //source.AppendLine($"world/entity api ??? {node}");
                                TraceGenericUsages(node);
                            }
                        }
                    }
                }
            }
            else if (typeName == "Simulation.ComponentQuery")
            {
                int desiredArgumentCount = node.ArgumentList.Arguments.Count;
                if (methodName == "Create")
                {
                    if (symbol.Arity > 0 && symbol.Parameters.Length == desiredArgumentCount)
                    {
                        foreach (ITypeSymbol genericType in symbol.TypeArguments)
                        {
                            if (genericType.TypeKind != TypeKind.TypeParameter)
                            {
                                FoundComponentType(genericType);
                            }
                            else
                            {
                                //source.AppendLine($"query api ??? {node}");
                                TraceGenericUsages(node);
                            }
                        }
                    }
                }
            }
            else if (typeName == "Simulation.ComponentType")
            {
                if (methodName == "Get")
                {
                    ITypeSymbol genericType = symbol.TypeArguments[0];
                    if (genericType.TypeKind != TypeKind.TypeParameter)
                    {
                        FoundComponentType(genericType);
                    }
                    else
                    {
                        //source.AppendLine($"component type ??? {node}");
                        TraceGenericUsages(node);
                    }
                }
            }
            else if (typeName == "Simulation.ArrayType")
            {
                if (methodName == "Get")
                {
                    ITypeSymbol genericType = symbol.TypeArguments[0];
                    if (genericType.TypeKind != TypeKind.TypeParameter)
                    {
                        FoundArrayType(genericType);
                    }
                    else
                    {
                        //source.AppendLine($"array type ??? {node}");
                        TraceGenericUsages(node);
                    }
                }
            }
            else
            {
                /*
                source.AppendLine($"Possibly invocations of {symbol.GetType()}");
                source.BeginGroup();
                {
                    foreach (SyntaxNode child in node.DescendantNodes())
                    {
                        source.AppendLine(child.GetType());
                    }
                }
                source.EndGroup();
                */
            }
        }

        private void TraceGenericUsages(InvocationExpressionSyntax invocation)
        {
            MethodDeclarationSyntax methodDeclaration = GetMethodDeclaration(invocation);
            if (invocations.TryGetValue(methodDeclaration, out List<InvocationExpressionSyntax>? list))
            {
                foreach (InvocationExpressionSyntax otherInvocation in list)
                {
                    IMethodSymbol[] otherMethodSymbols = GetSymbols<IMethodSymbol>(otherInvocation);
                    if (otherMethodSymbols.Length > 0)
                    {
                        foreach (IMethodSymbol possibleCandidate in otherMethodSymbols)
                        {
                            if (possibleCandidate.Arity > 0)
                            {
                                foreach (ITypeSymbol type in possibleCandidate.TypeArguments)
                                {
                                    if (type.TypeKind != TypeKind.TypeParameter)
                                    {
                                        FoundComponentType(type);
                                    }
                                    else
                                    {
                                        TraceGenericUsages(otherInvocation);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void FoundComponentType(ITypeSymbol typeSymbol)
        {
            componentTypeNames.Add(typeSymbol.ToDisplayString());
        }

        private void FoundArrayType(ITypeSymbol typeSymbol)
        {
            arrayTypeNames.Add(typeSymbol.ToDisplayString());
        }

        private MethodDeclarationSyntax GetMethodDeclaration(SyntaxNode node)
        {
            Stack<SyntaxNode> stack = new();
            stack.Push(node);
            while (stack.Count > 0)
            {
                SyntaxNode current = stack.Pop();
                if (current is MethodDeclarationSyntax methodDeclaration)
                {
                    return methodDeclaration;
                }
                else if (current.Parent is not null)
                {
                    stack.Push(current.Parent);
                }
            }

            throw new InvalidOperationException("Method declaration not found");
        }
    }
}