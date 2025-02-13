using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Types;

namespace Worlds.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class SchemaBankGenerator : IIncrementalGenerator
    {
        public const string TypeNameFormat = "{0}SchemaBank";
        public const string InterfaceName = "Worlds.ISchemaBank";

        private static readonly List<InvocationMatch> componentInvocations;
        private static readonly List<InvocationMatch> arrayElementInvocations;
        private static readonly List<InvocationMatch> tagInvocations;

        static SchemaBankGenerator()
        {
            componentInvocations = new();
            arrayElementInvocations = new();
            tagInvocations = new();

            componentInvocations.Add(new("CreateExtensions", "CreateEntity"));

            componentInvocations.Add(new("FetchExtensions", "GetAllContaining"));

            componentInvocations.Add(new("World", "ContainsComponent"));
            componentInvocations.Add(new("World", "AddComponent"));
            componentInvocations.Add(new("World", "RemoveComponent"));
            componentInvocations.Add(new("World", "GetComponent"));
            componentInvocations.Add(new("World", "SetComponent"));
            componentInvocations.Add(new("World", "TryGetComponent"));
            componentInvocations.Add(new("World", "ContainsAnyComponent"));
            arrayElementInvocations.Add(new("World", "CreateArray"));
            arrayElementInvocations.Add(new("World", "DestroyArray"));
            arrayElementInvocations.Add(new("World", "ResizeArray"));
            arrayElementInvocations.Add(new("World", "ContainsArray"));
            arrayElementInvocations.Add(new("World", "GetArray"));
            arrayElementInvocations.Add(new("World", "GetArrayLength"));
            tagInvocations.Add(new("World", "AddTag"));
            tagInvocations.Add(new("World", "ContainsTag"));
            tagInvocations.Add(new("World", "RemoveTag"));

            componentInvocations.Add(new("Entity", "ContainsComponent"));
            componentInvocations.Add(new("Entity", "AddComponent"));
            componentInvocations.Add(new("Entity", "RemoveComponent"));
            componentInvocations.Add(new("Entity", "GetComponent"));
            componentInvocations.Add(new("Entity", "SetComponent"));
            componentInvocations.Add(new("Entity", "TryGetComponent"));
            arrayElementInvocations.Add(new("Entity", "CreateArray"));
            arrayElementInvocations.Add(new("Entity", "DestroyArray"));
            arrayElementInvocations.Add(new("Entity", "ResizeArray"));
            arrayElementInvocations.Add(new("Entity", "ContainsArray"));
            arrayElementInvocations.Add(new("Entity", "GetArray"));
            arrayElementInvocations.Add(new("Entity", "GetArrayLength"));
            tagInvocations.Add(new("Entity", "AddTag"));
            tagInvocations.Add(new("Entity", "ContainsTag"));
            tagInvocations.Add(new("Entity", "RemoveTag"));

            componentInvocations.Add(new("Chunk", "GetComponents"));
            componentInvocations.Add(new("Chunk", "GetComponent"));

            componentInvocations.Add(new("Schema", "GetComponent"));
            componentInvocations.Add(new("Schema", "GetComponents"));
            componentInvocations.Add(new("Schema", "GetComponentDataType"));
            componentInvocations.Add(new("Schema", "GetComponentSize"));
            arrayElementInvocations.Add(new("Schema", "GetArrayElement"));
            arrayElementInvocations.Add(new("Schema", "GetArrayElements"));
            arrayElementInvocations.Add(new("Schema", "GetArrayElementDataType"));
            arrayElementInvocations.Add(new("Schema", "GetArrayElementSize"));
            tagInvocations.Add(new("Schema", "GetTag"));
            tagInvocations.Add(new("Schema", "GetTagDataType"));

            componentInvocations.Add(new("ComponentQuery", "RequireComponent"));
            componentInvocations.Add(new("ComponentQuery", "ExcludeComponent"));
            arrayElementInvocations.Add(new("ComponentQuery", "RequireArrayElement"));
            arrayElementInvocations.Add(new("ComponentQuery", "ExcludeArrayElement"));
            tagInvocations.Add(new("ComponentQuery", "RequireTag"));
            tagInvocations.Add(new("ComponentQuery", "ExcludeTag"));

            componentInvocations.Add(new("Operation", "AddComponent"));
            componentInvocations.Add(new("Operation", "RemoveComponent"));
            componentInvocations.Add(new("Operation", "SetComponent"));
            componentInvocations.Add(new("Operation", "AddOrSetComponent"));
            arrayElementInvocations.Add(new("Operation", "CreateArray"));
            arrayElementInvocations.Add(new("Operation", "ResizeArray"));
            arrayElementInvocations.Add(new("Operation", "SetArrayElements"));
            arrayElementInvocations.Add(new("Operation", "SetArrayElement"));
            arrayElementInvocations.Add(new("Operation", "SetArray"));
            arrayElementInvocations.Add(new("Operation", "CreateOrSetArray"));
            arrayElementInvocations.Add(new("Operation", "DestroyArray"));
            tagInvocations.Add(new("Operation", "AddTag"));
            tagInvocations.Add(new("Operation", "RemoveTag"));

            componentInvocations.Add(new("Definition", "AddComponentTypes"));
            componentInvocations.Add(new("Definition", "AddComponentType"));
            componentInvocations.Add(new("Definition", "GetComponentSize"));
            arrayElementInvocations.Add(new("Definition", "AddArrayType"));
            arrayElementInvocations.Add(new("Definition", "AddArrayTypes"));
            arrayElementInvocations.Add(new("Definition", "GetArrayElementSize"));
            tagInvocations.Add(new("Definition", "AddTagTypes"));
            tagInvocations.Add(new("Definition", "AddTagType"));

            componentInvocations.Add(new("Archetype", "AddComponentTypes"));
            componentInvocations.Add(new("Archetype", "AddComponentType"));
            componentInvocations.Add(new("Archetype", "GetComponentSize"));
            arrayElementInvocations.Add(new("Archetype", "AddArrayType"));
            arrayElementInvocations.Add(new("Archetype", "AddArrayTypes"));
            arrayElementInvocations.Add(new("Archetype", "GetArrayElementSize"));
            tagInvocations.Add(new("Archetype", "AddTagTypes"));
            tagInvocations.Add(new("Archetype", "AddTagType"));
        }

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<Input?> inputs = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            context.RegisterSourceOutput(inputs.Collect(), Generate);
        }

        private void Generate(SourceProductionContext context, ImmutableArray<Input?> inputs)
        {
            List<Input> inputsList = new();
            foreach (Input? input in inputs)
            {
                if (input is not null)
                {
                    inputsList.Add(input);
                }
            }

            if (inputsList.Count > 0)
            {
                if (TryGenerate(inputsList, out string typeName, out string sourceCode))
                {
                    context.AddSource($"{typeName}.generated.cs", sourceCode);
                }
            }
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node is CompilationUnitSyntax;
        }

        private static Input? Transform(GeneratorSyntaxContext context, CancellationToken token)
        {
            if (context.Node is CompilationUnitSyntax compilationUnit)
            {
                SemanticModel semanticModel = context.SemanticModel;
                return new Input(compilationUnit, semanticModel);
            }

            return null;
        }

        private static string? GetMethodName(InvocationExpressionSyntax invocationExpression)
        {
            if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Name.Identifier.Text;
            }
            else if (invocationExpression.Expression is IdentifierNameSyntax identifierName)
            {
                return identifierName.Identifier.Text;
            }
            else if (invocationExpression.Expression is GenericNameSyntax genericName)
            {
                return genericName.Identifier.Text;
            }

            return null;
        }

        private static bool IsComponentMethod(string methodName)
        {
            foreach (InvocationMatch componentInvocation in componentInvocations)
            {
                if (componentInvocation.methodName == methodName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsComponentMethod(ITypeSymbol declaringType, string methodName)
        {
            string declaringTypeName = declaringType.Name;
            bool isEntity = declaringType.HasInterface("Worlds.IEntity");
            foreach (InvocationMatch componentInvocation in componentInvocations)
            {
                if (componentInvocation.declaringTypeName == declaringTypeName || (componentInvocation.declaringTypeName == "Entity" && isEntity))
                {
                    if (componentInvocation.methodName == methodName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsArrayElementMethod(string methodName)
        {
            foreach (InvocationMatch arrayElementInvocation in arrayElementInvocations)
            {
                if (arrayElementInvocation.methodName == methodName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsArrayElementMethod(ITypeSymbol declaringType, string methodName)
        {
            string declaringTypeName = declaringType.Name;
            bool isEntity = declaringType.HasInterface("Worlds.IEntity");
            foreach (InvocationMatch arrayElementInvocation in arrayElementInvocations)
            {
                if (arrayElementInvocation.declaringTypeName == declaringTypeName || (arrayElementInvocation.declaringTypeName == "Entity" && isEntity))
                {
                    if (arrayElementInvocation.methodName == methodName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsTagMethod(string methodName)
        {
            foreach (InvocationMatch tagInvocation in tagInvocations)
            {
                if (tagInvocation.methodName == methodName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTagMethod(ITypeSymbol declaringType, string methodName)
        {
            string declaringTypeName = declaringType.Name;
            bool isEntity = declaringType.HasInterface("Worlds.IEntity");
            foreach (InvocationMatch tagInvocation in tagInvocations)
            {
                if (tagInvocation.declaringTypeName == declaringTypeName || (tagInvocation.declaringTypeName == "Entity" && isEntity))
                {
                    if (tagInvocation.methodName == methodName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static IReadOnlyCollection<FoundDataType> GetMentionedDataTypes(SourceBuilder source, IReadOnlyList<Input> inputs)
        {
            HashSet<FoundDataType> all = new();
            HashSet<(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)> types = new();
            HashSet<(SyntaxNode node, SemanticModel semanticModel)> remaining = new();
            foreach (Input input in inputs)
            {
                SyntaxNode rootNode = input.rootNode;
                SemanticModel semanticModel = input.semanticModel;
                foreach (SyntaxNode descendant in rootNode.DescendantNodes())
                {
                    if (descendant is TypeDeclarationSyntax typeDeclaration)
                    {
                        types.Add((typeDeclaration, semanticModel));
                    }
                    else
                    {
                        if (descendant is InvocationExpressionSyntax invocationExpression)
                        {
                            Handle(source, all, null, semanticModel, false, invocationExpression);
                        }
                        else if (descendant is VariableDeclarationSyntax variableDeclaration)
                        {
                            TypeSyntax variableType = variableDeclaration.Type;
                            if (variableType is GenericNameSyntax genericName && genericName.Identifier.Text == "ComponentQuery")
                            {
                                foreach (TypeSyntax typeSyntax in genericName.TypeArgumentList.Arguments)
                                {
                                    if (typeSyntax is IdentifierNameSyntax identifierName)
                                    {
                                        if (semanticModel.GetTypeInfo(identifierName).Type is ITypeSymbol genericType)
                                        {
                                            if (genericType is ITypeParameterSymbol)
                                            {
                                                continue;
                                            }

                                            if (all.Add(new FoundDataType(DataKind.Component, genericType.GetFullTypeName())))
                                            {
                                                source.AppendLine($"// {variableDeclaration.Type}: {genericType.GetFullTypeName()};");
                                            }
                                        }
                                        else
                                        {
                                            source.AppendLine($"// not found: {variableDeclaration.Type}: {identifierName.Identifier};");
                                        }
                                    }
                                }
                            }
                        }
                        else if (descendant is ExpressionStatementSyntax expressionStatement)
                        {
                            if (expressionStatement.Expression is InvocationExpressionSyntax wrappedInvocationExpression)
                            {
                                Handle(source, all, null, semanticModel, false, wrappedInvocationExpression);
                            }

                            //source.AppendLine($"// expression: {input.typeDeclaration.Identifier} {expressionStatement.Expression} {expressionStatement.Expression.GetType()}");
                        }
                        else if (descendant is LocalDeclarationStatementSyntax localDeclarationStatement)
                        {
                            //check if its ComponentQuery<T1, T2, ...>
                            TypeSyntax variableType = localDeclarationStatement.Declaration.Type;
                            //source.AppendLine($"// local: {variableType} {variableType.GetType()} {localDeclarationStatement.Declaration.Variables.Count}");
                            if (semanticModel.GetTypeInfo(variableType).Type is ITypeSymbol genericType)
                            {
                                if (genericType is ITypeParameterSymbol)
                                {
                                    continue;
                                }

                                if (genericType.GetFullTypeName().StartsWith("Worlds.ComponentQuery<"))
                                {
                                    foreach (TypeSyntax typeSyntax in ((GenericNameSyntax)variableType).TypeArgumentList.Arguments)
                                    {
                                        if (semanticModel.GetTypeInfo(typeSyntax).Type is ITypeSymbol componentType)
                                        {
                                            if (componentType is ITypeParameterSymbol)
                                            {
                                                continue;
                                            }

                                            if (all.Add(new FoundDataType(DataKind.Component, componentType.GetFullTypeName())))
                                            {
                                                source.AppendLine($"// local: {variableType} {componentType.GetFullTypeName()}");
                                            }
                                        }
                                        else
                                        {
                                            source.AppendLine($"// not found: {variableType} {typeSyntax}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //source.AppendLine($"// not found: {variableType} {variableType.GetType()} {localDeclarationStatement.Declaration.Variables.Count}");
                            }
                        }
                        else
                        {
                            //source.AppendLine($"// not invocation: {descendant.GetType()} {descendant}");
                        }
                    }
                }
            }

            foreach ((TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel) in types)
            {
                bool isEntity = false;
                if (semanticModel.GetDeclaredSymbol(typeDeclaration) is INamedTypeSymbol typeSymbol)
                {
                    isEntity = typeSymbol.HasInterface("Worlds.IEntity");
                }

                //source.AppendLine($"// {typeDeclaration.Identifier}: {isEntity};");

                foreach (SyntaxNode descendant in typeDeclaration.DescendantNodes())
                {
                    if (descendant is InvocationExpressionSyntax invocationExpression)
                    {
                        Handle(source, all, typeDeclaration, semanticModel, isEntity, invocationExpression);
                    }
                    else if (descendant is VariableDeclarationSyntax variableDeclaration)
                    {
                        TypeSyntax variableType = variableDeclaration.Type;
                        if (variableType is GenericNameSyntax genericName && genericName.Identifier.Text == "ComponentQuery")
                        {
                            foreach (TypeSyntax typeSyntax in genericName.TypeArgumentList.Arguments)
                            {
                                if (typeSyntax is IdentifierNameSyntax identifierName)
                                {
                                    if (semanticModel.GetTypeInfo(identifierName).Type is ITypeSymbol genericType)
                                    {
                                        if (genericType is ITypeParameterSymbol)
                                        {
                                            continue;
                                        }

                                        if (all.Add(new FoundDataType(DataKind.Component, genericType.GetFullTypeName())))
                                        {
                                            source.AppendLine($"// {variableDeclaration.Type}: {genericType.GetFullTypeName()};");
                                        }
                                    }
                                    else
                                    {
                                        source.AppendLine($"// not found: {variableDeclaration.Type}: {identifierName.Identifier};");
                                    }
                                }
                            }
                        }
                    }
                    else if (descendant is ExpressionStatementSyntax expressionStatement)
                    {
                        if (expressionStatement.Expression is InvocationExpressionSyntax wrappedInvocationExpression)
                        {
                            Handle(source, all, typeDeclaration, semanticModel, isEntity, wrappedInvocationExpression);
                        }

                        //source.AppendLine($"// expression: {input.typeDeclaration.Identifier} {expressionStatement.Expression} {expressionStatement.Expression.GetType()}");
                    }
                    else
                    {
                        //source.AppendLine($"// not invocation: {input.typeDeclaration.Identifier} {descendant} {descendant.GetType()}");
                    }
                }
            }

            //remove type names that are ?
            List<FoundDataType> valid = new();
            foreach (FoundDataType type in all)
            {
                if (type.fullTypeName != "?")
                {
                    valid.Add(type);
                }
            }

            return valid;
        }

        private static void Handle(SourceBuilder source, HashSet<FoundDataType> all, TypeDeclarationSyntax? typeDeclaration, SemanticModel semanticModel, bool isEntity, InvocationExpressionSyntax invocationExpression)
        {
            //source.AppendLine($"// 1111 {input.typeDeclaration.Identifier}: {invocationExpression.GetType()} + {invocationExpression.Expression.GetType()} + {invocationExpression};");
            if (semanticModel.GetSymbolInfo(invocationExpression).Symbol is IMethodSymbol methodSymbol)
            {
                //source.AppendLine($"// {input.typeDeclaration.Identifier}: generic: {methodSymbol.IsGenericMethod} {invocationExpression.Expression.GetType()}; + {invocationExpression}");
                if (methodSymbol.IsGenericMethod)
                {
                    ITypeSymbol declaringTypeSymbol = methodSymbol.ContainingType;
                    //source.AppendLine($"//     {declaringTypeSymbol.Name}.{methodSymbol.Name}");
                    if (IsComponentMethod(declaringTypeSymbol, methodSymbol.Name))
                    {
                        //source.AppendLine($"// {declaringTypeSymbol.Name}.{methodSymbol.Name} is component with {methodSymbol.TypeArguments.Length} type args");
                        foreach (ITypeSymbol genericType in methodSymbol.TypeArguments)
                        {
                            //source.AppendLine($"//     {declaringTypeSymbol.Name}.{methodSymbol.Name}<{genericType.GetFullTypeName()}>();");
                            if (genericType is ITypeParameterSymbol)
                            {
                                continue;
                            }

                            if (all.Add(new FoundDataType(DataKind.Component, genericType.GetFullTypeName())))
                            {
                                source.AppendLine($"// {declaringTypeSymbol.Name}.{methodSymbol.Name}<{genericType.GetFullTypeName()}>();");
                            }
                        }
                    }
                    else
                    {
                        source.AppendLine($"//     {declaringTypeSymbol.Name}.{methodSymbol.Name} is not component");
                    }

                    if (IsArrayElementMethod(declaringTypeSymbol, methodSymbol.Name))
                    {
                        foreach (ITypeSymbol genericType in methodSymbol.TypeArguments)
                        {
                            if (genericType is ITypeParameterSymbol)
                            {
                                continue;
                            }

                            if (all.Add(new FoundDataType(DataKind.ArrayElement, genericType.GetFullTypeName())))
                            {
                                source.AppendLine($"// {declaringTypeSymbol.Name}.{methodSymbol.Name}<{genericType.GetFullTypeName()}>();");
                            }
                        }
                    }

                    if (IsTagMethod(declaringTypeSymbol, methodSymbol.Name))
                    {
                        foreach (ITypeSymbol genericType in methodSymbol.TypeArguments)
                        {
                            if (genericType is ITypeParameterSymbol)
                            {
                                continue;
                            }

                            if (all.Add(new FoundDataType(DataKind.Tag, genericType.GetFullTypeName())))
                            {
                                source.AppendLine($"// {declaringTypeSymbol.Name}.{methodSymbol.Name}<{genericType.GetFullTypeName()}>();");
                            }
                        }
                    }
                }
            }
            else
            {
                if (typeDeclaration is not null)
                {
                    //source.AppendLine($"// {input.typeDeclaration.Identifier}: {invocationExpression.GetType()} + {invocationExpression.Expression.GetType()} + {invocationExpression}");
                    if (invocationExpression.Expression is GenericNameSyntax || (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name is GenericNameSyntax))
                    {
                        if (isEntity)
                        {
                            if (GetMethodName(invocationExpression) is string methodName)
                            {
                                source.AppendLine($"// entity {typeDeclaration.Identifier} with method {methodName}: {invocationExpression.Expression}");
                                if (IsComponentMethod(methodName))
                                {
                                    if (invocationExpression.Expression is GenericNameSyntax genericName)
                                    {
                                        //print all generic types
                                        foreach (TypeSyntax typeSyntax in genericName.TypeArgumentList.Arguments)
                                        {
                                            if (semanticModel.GetTypeInfo(typeSyntax).Type is ITypeSymbol genericType)
                                            {
                                                if (genericType is ITypeParameterSymbol)
                                                {
                                                    continue;
                                                }

                                                if (all.Add(new FoundDataType(DataKind.Component, genericType.GetFullTypeName())))
                                                {
                                                    source.AppendLine($"// entity: {typeDeclaration.Identifier}: {methodName}<{genericType.GetFullTypeName()}>();");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (ArgumentSyntax argumentSyntax in invocationExpression.ArgumentList.Arguments)
                                        {
                                            if (semanticModel.GetTypeInfo(argumentSyntax.Expression).Type is ITypeSymbol genericType)
                                            {
                                                if (genericType is ITypeParameterSymbol)
                                                {
                                                    continue;
                                                }

                                                if (all.Add(new FoundDataType(DataKind.Component, genericType.GetFullTypeName())))
                                                {
                                                    source.AppendLine($"// entity: {typeDeclaration.Identifier}: {methodName}<{genericType.GetFullTypeName()}>(); + {invocationExpression.Expression} {invocationExpression.Expression.GetType()}");
                                                }
                                            }
                                        }
                                    }
                                }

                                if (IsArrayElementMethod(methodName))
                                {
                                    if (invocationExpression.Expression is GenericNameSyntax genericName)
                                    {
                                        foreach (TypeSyntax typeSyntax in genericName.TypeArgumentList.Arguments)
                                        {
                                            if (semanticModel.GetTypeInfo(typeSyntax).Type is ITypeSymbol genericType)
                                            {
                                                if (genericType is ITypeParameterSymbol)
                                                {
                                                    continue;
                                                }

                                                if (all.Add(new FoundDataType(DataKind.ArrayElement, genericType.GetFullTypeName())))
                                                {
                                                    source.AppendLine($"// entity: {typeDeclaration.Identifier}: {methodName}<{genericType.GetFullTypeName()}>();");
                                                }
                                            }
                                        }
                                    }

                                    //source.AppendLine($"// array: {input.typeDeclaration.Identifier}: {methodName} {invocationExpression.ArgumentList} {invocationExpression.Expression.GetType()}");
                                    foreach (ArgumentSyntax argumentSyntax in invocationExpression.ArgumentList.Arguments)
                                    {
                                        if (semanticModel.GetTypeInfo(argumentSyntax.Expression).Type is ITypeSymbol genericType)
                                        {
                                            if (genericType is ITypeParameterSymbol)
                                            {
                                                continue;
                                            }

                                            string fullTypeName = genericType.GetFullTypeName();
                                            if (fullTypeName.StartsWith("Unmanaged.USpan<"))
                                            {
                                                fullTypeName = fullTypeName.Substring(14, fullTypeName.Length - 15);
                                            }

                                            if (all.Add(new FoundDataType(DataKind.ArrayElement, fullTypeName)))
                                            {
                                                source.AppendLine($"// entity: {typeDeclaration.Identifier}: {methodName}<{fullTypeName}>();");
                                            }
                                        }
                                    }
                                }

                                if (IsTagMethod(methodName))
                                {
                                    foreach (ArgumentSyntax argumentSyntax in invocationExpression.ArgumentList.Arguments)
                                    {
                                        if (semanticModel.GetTypeInfo(argumentSyntax.Expression).Type is ITypeSymbol genericType)
                                        {
                                            if (genericType is ITypeParameterSymbol)
                                            {
                                                continue;
                                            }

                                            if (all.Add(new FoundDataType(DataKind.Tag, genericType.GetFullTypeName())))
                                            {
                                                source.AppendLine($"// entity: {typeDeclaration.Identifier}: {methodName}<{genericType.GetFullTypeName()}>();");
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                source.AppendLine($"// entity: {typeDeclaration.Identifier}: not known method {invocationExpression.Expression}+{invocationExpression.Expression.GetType()};");
                            }
                        }
                        else
                        {
                            source.AppendLine($"// not entity: {typeDeclaration.Identifier} {invocationExpression} {invocationExpression.GetType()}");
                        }
                    }
                }
            }
        }

        public static bool TryGenerate(IReadOnlyList<Input> inputs, out string typeName, out string sourceCode)
        {
            SourceBuilder source = new();
            string? assemblyName = null;
            IReadOnlyCollection<FoundDataType> results = GetMentionedDataTypes(source, inputs);

            if (results.Count == 0)
            {
                typeName = string.Empty;
                sourceCode = string.Empty;
                return false;
            }

            foreach (Input input in inputs)
            {
                if (assemblyName is null)
                {
                    SyntaxTree syntaxTree = input.rootNode.SyntaxTree;
                    SemanticModel semanticModel = input.semanticModel;
                    foreach (SyntaxNode descendant in syntaxTree.GetRoot().DescendantNodes())
                    {
                        if (descendant is TypeDeclarationSyntax typeDeclaration)
                        {
                            if (semanticModel.GetDeclaredSymbol(typeDeclaration) is INamedTypeSymbol typeSymbol)
                            {
                                assemblyName = typeSymbol.ContainingAssembly.Name;
                                break;
                            }
                        }
                    }
                }
            }

            if (assemblyName is not null && assemblyName.EndsWith(".Core"))
            {
                assemblyName = assemblyName.Substring(0, assemblyName.Length - 5);
            }

            source.AppendLine("using Types;");
            source.AppendLine("using Unmanaged;");
            source.AppendLine("using Worlds;");
            source.AppendLine();

            if (assemblyName is not null)
            {
                source.Append("namespace ");
                source.AppendLine(assemblyName);
                source.BeginGroup();
            }

            source.AppendLine("/// <summary>");
            source.AppendLine("/// Contains all component, array and tag");
            source.AppendLine("/// types mentioned by this project.");
            source.AppendLine("/// </summary>");

            typeName = TypeNameFormat.Replace("{0}", assemblyName ?? "");
            typeName = typeName.Replace(".", "");
            source.Append("public readonly struct ");
            source.Append(typeName);
            source.Append(" : ");
            source.Append(InterfaceName);
            source.AppendLine();

            source.BeginGroup();
            {
                source.Append("readonly void ");
                source.Append(InterfaceName);
                source.Append(".Load(Schema schema)");
                source.AppendLine();

                source.BeginGroup();
                {
                    foreach (FoundDataType result in results)
                    {
                        source.Append("schema.Register");
                        if (result.kind == DataKind.Component)
                        {
                            source.Append("Component<");
                        }
                        else if (result.kind == DataKind.ArrayElement)
                        {
                            source.Append("ArrayElement<");
                        }
                        else if (result.kind == DataKind.Tag)
                        {
                            source.Append("Tag<");
                        }

                        source.Append(result.fullTypeName);
                        source.Append(">();");
                        source.AppendLine();
                    }
                }
                source.EndGroup();
            }
            source.EndGroup();

            if (assemblyName is not null)
            {
                source.EndGroup();
                typeName = $"{assemblyName}.{typeName}";
            }

            sourceCode = source.ToString();
            return true;
        }

        public class Input
        {
            public readonly SyntaxNode rootNode;
            public readonly SemanticModel semanticModel;

            public Input(SyntaxNode rootNode, SemanticModel semanticModel)
            {
                this.rootNode = rootNode;
                this.semanticModel = semanticModel;
            }
        }

        public readonly struct FoundDataType : IEquatable<FoundDataType>
        {
            public readonly DataKind kind;
            public readonly string fullTypeName;

            public FoundDataType(DataKind kind, string fullTypeName)
            {
                this.kind = kind;
                this.fullTypeName = fullTypeName;
            }

            public readonly override bool Equals(object? obj)
            {
                return obj is FoundDataType type && Equals(type);
            }

            public readonly bool Equals(FoundDataType other)
            {
                return kind == other.kind && fullTypeName == other.fullTypeName;
            }

            public readonly override int GetHashCode()
            {
                int hashCode = -1702990006;
                hashCode = hashCode * -1521134295 + kind.GetHashCode();
                for (int i = 0; i < fullTypeName.Length; i++)
                {
                    hashCode = hashCode * -1521134295 + fullTypeName[i];
                }

                return hashCode;
            }

            public static bool operator ==(FoundDataType left, FoundDataType right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(FoundDataType left, FoundDataType right)
            {
                return !(left == right);
            }
        }

        public enum DataKind
        {
            Unknown,
            Component,
            ArrayElement,
            Tag
        }

        public struct InvocationMatch
        {
            public readonly string declaringTypeName;
            public readonly string methodName;

            public InvocationMatch(string declaringTypeName, string methodName)
            {
                this.declaringTypeName = declaringTypeName;
                this.methodName = methodName;
            }
        }
    }
}