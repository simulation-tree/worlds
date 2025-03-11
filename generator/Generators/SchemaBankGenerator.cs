using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Unmanaged;

namespace Worlds.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class SchemaBankGenerator : IIncrementalGenerator
    {
        public const string TypeNameFormat = "{0}SchemaBank";
        public const string SchemaBankInterfaceName = "Worlds.ISchemaBank";
        public const string EntityInterfaceName = "Worlds.IEntity";

        private static readonly List<InvocationMatch> componentInvocations;
        private static readonly List<InvocationMatch> arrayInvocations;
        private static readonly List<InvocationMatch> tagInvocations;

        static SchemaBankGenerator()
        {
            componentInvocations = new();
            arrayInvocations = new();
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
            arrayInvocations.Add(new("World", "CreateArray"));
            arrayInvocations.Add(new("World", "DestroyArray"));
            arrayInvocations.Add(new("World", "ContainsArray"));
            arrayInvocations.Add(new("World", "GetArray"));
            arrayInvocations.Add(new("World", "GetArrayLength"));
            tagInvocations.Add(new("World", "AddTag"));
            tagInvocations.Add(new("World", "ContainsTag"));
            tagInvocations.Add(new("World", "RemoveTag"));

            componentInvocations.Add(new("Entity", "ContainsComponent"));
            componentInvocations.Add(new("Entity", "AddComponent"));
            componentInvocations.Add(new("Entity", "RemoveComponent"));
            componentInvocations.Add(new("Entity", "GetComponent"));
            componentInvocations.Add(new("Entity", "SetComponent"));
            componentInvocations.Add(new("Entity", "TryGetComponent"));
            arrayInvocations.Add(new("Entity", "CreateArray"));
            arrayInvocations.Add(new("Entity", "DestroyArray"));
            arrayInvocations.Add(new("Entity", "ContainsArray"));
            arrayInvocations.Add(new("Entity", "GetArray"));
            arrayInvocations.Add(new("Entity", "GetArrayElement"));
            arrayInvocations.Add(new("Entity", "GetArrayLength"));
            tagInvocations.Add(new("Entity", "AddTag"));
            tagInvocations.Add(new("Entity", "ContainsTag"));
            tagInvocations.Add(new("Entity", "RemoveTag"));

            componentInvocations.Add(new("Chunk", "GetComponents"));
            componentInvocations.Add(new("Chunk", "GetComponent"));
            componentInvocations.Add(new("Chunk", "SetComponent"));

            componentInvocations.Add(new("Schema", "GetComponentType"));
            componentInvocations.Add(new("Schema", "GetComponents"));
            componentInvocations.Add(new("Schema", "GetComponentDataType"));
            arrayInvocations.Add(new("Schema", "GetArrayType"));
            arrayInvocations.Add(new("Schema", "GetArrayDataType"));
            tagInvocations.Add(new("Schema", "GetTagType"));
            tagInvocations.Add(new("Schema", "GetTagDataType"));

            componentInvocations.Add(new("ComponentQuery", "RequireComponent"));
            componentInvocations.Add(new("ComponentQuery", "RequireComponents"));
            componentInvocations.Add(new("ComponentQuery", "ExcludeComponent"));
            componentInvocations.Add(new("ComponentQuery", "ExcludeComponents"));
            arrayInvocations.Add(new("ComponentQuery", "RequireArray"));
            arrayInvocations.Add(new("ComponentQuery", "RequireArrays"));
            arrayInvocations.Add(new("ComponentQuery", "ExcludeArray"));
            arrayInvocations.Add(new("ComponentQuery", "ExcludeArrays"));
            tagInvocations.Add(new("ComponentQuery", "RequireTag"));
            tagInvocations.Add(new("ComponentQuery", "ExcludeTag"));

            componentInvocations.Add(new("Operation", "AddComponent"));
            componentInvocations.Add(new("Operation", "RemoveComponent"));
            componentInvocations.Add(new("Operation", "SetComponent"));
            componentInvocations.Add(new("Operation", "AddOrSetComponent"));
            arrayInvocations.Add(new("Operation", "CreateArray"));
            arrayInvocations.Add(new("Operation", "SetArrayElements"));
            arrayInvocations.Add(new("Operation", "SetArrayElement"));
            arrayInvocations.Add(new("Operation", "SetArray"));
            arrayInvocations.Add(new("Operation", "CreateOrSetArray"));
            arrayInvocations.Add(new("Operation", "DestroyArray"));
            tagInvocations.Add(new("Operation", "AddTag"));
            tagInvocations.Add(new("Operation", "RemoveTag"));

            componentInvocations.Add(new("Definition", "AddComponentTypes"));
            componentInvocations.Add(new("Definition", "AddComponentType"));
            arrayInvocations.Add(new("Definition", "AddArrayType"));
            arrayInvocations.Add(new("Definition", "AddArrayTypes"));
            tagInvocations.Add(new("Definition", "AddTagTypes"));
            tagInvocations.Add(new("Definition", "AddTagType"));

            componentInvocations.Add(new("Archetype", "AddComponentTypes"));
            componentInvocations.Add(new("Archetype", "AddComponentType"));
            arrayInvocations.Add(new("Archetype", "AddArrayType"));
            arrayInvocations.Add(new("Archetype", "AddArrayTypes"));
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
            bool isEntity = declaringType.HasInterface(EntityInterfaceName);
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

        private static bool IsArrayMethod(string methodName)
        {
            foreach (InvocationMatch arrayInvocation in arrayInvocations)
            {
                if (arrayInvocation.methodName == methodName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsArrayMethod(ITypeSymbol declaringType, string methodName)
        {
            string declaringTypeName = declaringType.Name;
            bool isEntity = declaringType.HasInterface(EntityInterfaceName);
            foreach (InvocationMatch arrayInvocation in arrayInvocations)
            {
                if (arrayInvocation.declaringTypeName == declaringTypeName || (arrayInvocation.declaringTypeName == "Entity" && isEntity))
                {
                    if (arrayInvocation.methodName == methodName)
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
            bool isEntity = declaringType.HasInterface(EntityInterfaceName);
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

        public static IReadOnlyList<DataType> GetMentionedDataTypes(SourceBuilder source, IReadOnlyList<Input> inputs)
        {
            DataTypeCollection collection = new();
            HashSet<(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)> types = new();
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
                            Handle(source, collection, semanticModel, false, invocationExpression);
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

                                            if (collection.TryAdd(DataKind.Component, genericType.GetFullTypeName()))
                                            {
                                                source.AppendLine($"// component {descendant} = {genericType.GetFullTypeName()}");
                                            }
                                        }
                                        else
                                        {
                                            //source.AppendLine($"// not found: {variableDeclaration.Type}: {identifierName.Identifier};");
                                        }
                                    }
                                }
                            }
                        }
                        else if (descendant is ExpressionStatementSyntax expressionStatement)
                        {
                            if (expressionStatement.Expression is InvocationExpressionSyntax wrappedInvocationExpression)
                            {
                                Handle(source, collection, semanticModel, false, wrappedInvocationExpression);
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

                                            if (collection.TryAdd(DataKind.Component, componentType.GetFullTypeName()))
                                            {
                                                source.AppendLine($"// component {descendant} = {genericType.GetFullTypeName()}");
                                            }
                                        }
                                        else
                                        {
                                            //source.AppendLine($"// not found: {variableType} {typeSyntax}");
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
                    isEntity = typeSymbol.HasInterface(EntityInterfaceName);
                }

                //source.AppendLine($"// {typeDeclaration.Identifier}: {isEntity};");

                foreach (SyntaxNode descendant in typeDeclaration.DescendantNodes())
                {
                    if (descendant is InvocationExpressionSyntax invocationExpression)
                    {
                        Handle(source, collection, semanticModel, isEntity, invocationExpression);
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

                                        if (collection.TryAdd(DataKind.Component, genericType.GetFullTypeName()))
                                        {
                                            source.AppendLine($"// component {descendant} = {genericType.GetFullTypeName()}");
                                        }
                                    }
                                    else
                                    {
                                        //source.AppendLine($"// not found: {variableDeclaration.Type}: {identifierName.Identifier};");
                                    }
                                }
                            }
                        }
                    }
                    else if (descendant is ExpressionStatementSyntax expressionStatement)
                    {
                        if (expressionStatement.Expression is InvocationExpressionSyntax wrappedInvocationExpression)
                        {
                            Handle(source, collection, semanticModel, isEntity, wrappedInvocationExpression);
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
            List<DataType> valid = new();
            foreach (DataType type in collection)
            {
                if (type.fullTypeName != "?")
                {
                    valid.Add(type);
                }
            }

            return valid;
        }

        private static void Handle(SourceBuilder source, DataTypeCollection collection, SemanticModel semanticModel, bool isEntity, InvocationExpressionSyntax invocationExpression)
        {
            //source.AppendLine($"// 1111 {typeDeclaration?.Identifier}: {invocationExpression.GetType()} + {invocationExpression.Expression.GetType()} + {invocationExpression};");
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

                            if (collection.TryAdd(DataKind.Component, genericType.GetFullTypeName()))
                            {
                                source.AppendLine($"// component {invocationExpression} = {genericType.GetFullTypeName()}");
                            }
                        }
                    }

                    if (IsArrayMethod(declaringTypeSymbol, methodSymbol.Name))
                    {
                        foreach (ITypeSymbol genericType in methodSymbol.TypeArguments)
                        {
                            if (genericType is ITypeParameterSymbol)
                            {
                                continue;
                            }

                            if (collection.TryAdd(DataKind.Array, genericType.GetFullTypeName()))
                            {
                                source.AppendLine($"// array {invocationExpression} = {genericType.GetFullTypeName()}");
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

                            if (collection.TryAdd(DataKind.Tag, genericType.GetFullTypeName()))
                            {
                                source.AppendLine($"// tag {invocationExpression} = {genericType.GetFullTypeName()}");
                            }
                        }
                    }
                }
            }
            else
            {
                //source.AppendLine($"// {invocationExpression.GetType()} (expression: {invocationExpression.Expression.GetType()}) = {invocationExpression}");
                //if (invocationExpression.Expression is MemberAccessExpressionSyntax memAccess)
                //{
                //    source.AppendLine($"//     {memAccess.Name.GetType()} = {memAccess.Name}");
                //}

                if (invocationExpression.Expression is GenericNameSyntax || invocationExpression.Expression is MemberAccessExpressionSyntax)
                {
                    if (isEntity)
                    {
                        //double check if the method is generic
                        if (!invocationExpression.ToString().Contains("<"))
                        {
                            //source.AppendLine($"// {invocationExpression}");
                            bool isGeneric = false;
                            foreach (SyntaxNode desc in invocationExpression.DescendantNodes())
                            {
                                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(desc);
                                if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
                                {
                                    //source.AppendLine($"// {typeSymbol.GetFullTypeName()}");
                                    string parameterTypeName = typeSymbol.GetFullTypeName();
                                    if (parameterTypeName != "Worlds.ComponentType")
                                    {
                                        isGeneric = true;
                                        break;
                                    }
                                    else if (parameterTypeName != "Worlds.ArrayType")
                                    {
                                        isGeneric = true;
                                        break;
                                    }
                                    else if (parameterTypeName != "Worlds.TagType")
                                    {
                                        isGeneric = true;
                                        break;
                                    }
                                }
                                else if (symbolInfo.Symbol is not null)
                                {
                                    //source.AppendLine($"//    {desc.GetType()} {desc} {symbolInfo.Symbol} {symbolInfo.Symbol.GetType()}");
                                }
                                else
                                {
                                    //source.AppendLine($"//    {desc.GetType()} {desc}");
                                }
                            }

                            if (!isGeneric)
                            {
                                return;
                            }
                        }

                        if (GetMethodName(invocationExpression) is string methodName)
                        {
                            //source.AppendLine($"// entity {typeDeclaration.Identifier} with method {methodName}: {invocationExpression.Expression}");
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

                                            if (collection.TryAdd(DataKind.Component, genericType.GetFullTypeName()))
                                            {
                                                source.AppendLine($"// component {invocationExpression} = {genericType.GetFullTypeName()}");
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

                                            if (collection.TryAdd(DataKind.Component, genericType.GetFullTypeName()))
                                            {
                                                source.AppendLine($"// component {invocationExpression} = {genericType.GetFullTypeName()}");
                                            }
                                        }
                                    }
                                }
                            }

                            if (IsArrayMethod(methodName))
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

                                            if (collection.TryAdd(DataKind.Array, genericType.GetFullTypeName()))
                                            {
                                                source.AppendLine($"// array {invocationExpression} = {genericType.GetFullTypeName()}");
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
                                        const string SpanStart = "System.Span<";
                                        if (fullTypeName.StartsWith(SpanStart))
                                        {
                                            //trim start and end
                                            fullTypeName = fullTypeName.Substring(SpanStart.Length, fullTypeName.Length - SpanStart.Length - 1);
                                        }

                                        if (collection.TryAdd(DataKind.Array, fullTypeName))
                                        {
                                            source.AppendLine($"// array {invocationExpression} = {genericType.GetFullTypeName()}");
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

                                        if (collection.TryAdd(DataKind.Tag, genericType.GetFullTypeName()))
                                        {
                                            source.AppendLine($"// tag {invocationExpression} = {genericType.GetFullTypeName()}");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //source.AppendLine($"// entity: {typeDeclaration.Identifier}: not known method {invocationExpression.Expression}+{invocationExpression.Expression.GetType()};");
                        }
                    }
                    else
                    {
                        //source.AppendLine($"// not entity: {typeDeclaration.Identifier} {invocationExpression} {invocationExpression.GetType()}");
                    }
                }
            }
        }

        public static bool TryGenerate(IReadOnlyList<Input> inputs, out string typeName, out string sourceCode)
        {
            SourceBuilder source = new();
            string? assemblyName = null;
            IReadOnlyList<DataType> collection = GetMentionedDataTypes(source, inputs);

            if (collection.Count == 0)
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
            source.Append(SchemaBankInterfaceName);
            source.AppendLine();

            source.BeginGroup();
            {
                source.Append("readonly void ");
                source.Append(SchemaBankInterfaceName);
                source.Append(".Load(Schema schema)");
                source.AppendLine();

                source.BeginGroup();
                {
                    foreach (DataType result in collection)
                    {
                        source.Append("schema.Register");
                        if (result.kind == DataKind.Component)
                        {
                            source.Append("Component<");
                        }
                        else if (result.kind == DataKind.Array)
                        {
                            source.Append("Array<");
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