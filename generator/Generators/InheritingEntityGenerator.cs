using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Worlds.Generators
{
    [Generator(LanguageNames.CSharp)]
    internal class InheritingEntityGenerator : IIncrementalGenerator
    {
        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            var structTypes = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            var input = structTypes.Combine(context.CompilationProvider);
            context.RegisterSourceOutput(input, Generate);
        }

        private static void Generate(SourceProductionContext context, ((EntityType? input, Diagnostic? diagnostic) result, Compilation compilation) data)
        {
            if (data.result.input is not null)
            {
                Generate(context, data.result.input, data.compilation);
            }

            if (data.result.diagnostic is not null)
            {
                context.ReportDiagnostic(data.result.diagnostic);
            }
        }

        private static void Generate(SourceProductionContext context, EntityType type, Compilation compilation)
        {
            SourceBuilder builder = new();
            builder.AppendLine("#nullable enable");
            builder.AppendLine("using Worlds;");
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Diagnostics;");
            builder.AppendLine("using System.Threading;");
            builder.AppendLine("using System.Threading.Tasks;");
            builder.AppendLine("using Unmanaged;");
            builder.AppendLine("using Collections.Generic;");
            builder.AppendLine("using Array = Collections.Array;");
            builder.AppendLine();

            //inside a namespace if needed
            INamespaceSymbol? containedNamespace = type.typeSymbol.ContainingNamespace;
            if (containedNamespace is not null)
            {
                builder.Append("namespace ");
                builder.Append(containedNamespace.ToDisplayString());
                builder.AppendLine();
                builder.BeginGroup();
            }

            //inside a type if needed
            INamedTypeSymbol? containingType = type.typeSymbol.ContainingType;
            if (containingType is not null)
            {
                builder.Append("public ");
                if (containingType.IsReadOnly)
                {
                    builder.Append("readonly ");
                }

                builder.Append("partial ");
                if (containingType.IsReferenceType)
                {
                    builder.Append("class ");
                }
                else
                {
                    builder.Append("struct ");
                }

                builder.Append(containingType.Name);
                builder.AppendLine();
                builder.BeginGroup();
            }

            //write the source
            string source = EntityTemplate.Source;
            string accessors = "public";
            if (type.typeSymbol.IsReadOnly)
            {
                accessors += " readonly";
            }

            int complianceIndent = GetIndentation(source, "{{ComplianceChecks}}");
            int bodyIndent = GetIndentation(source, "{{DisposeMethod}}");

            IReadOnlyList<DataType> requiredTypes = GetComplianceRequirements(type, compilation);
            string interfaces = GetInterfaces(type);
            string typeName = type.typeSymbol.Name;
            string? complianceChecks = GetComplianceChecks(type, complianceIndent, requiredTypes);
            string disposeMethod = GetDisposeMethod(type, bodyIndent);
            string equalityMethods = GetEqualityMethods(type, bodyIndent);
            string untilCompliant = GetUntilCompliantMethod(bodyIndent + 4, requiredTypes);
            string? staticDefinitionGetter = GetStaticDefinitionGetter(bodyIndent, requiredTypes);
            source = source.Replace("{{EntityInterfaces}}", interfaces);
            source = source.Replace("{{DisposeMethod}}", disposeMethod);
            source = source.Replace("{{EqualityMethods}}", equalityMethods);
            source = source.Replace("{{Accessors}}", accessors);
            source = source.Replace("{{ComplianceChecks}}", complianceChecks);
            source = source.Replace("{{TypeName}}", typeName);
            source = source.Replace("{{UntilCompliant}}", untilCompliant);
            source = source.Replace("{{StaticDefinitionGetter}}", staticDefinitionGetter);
            string[] lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                ref string line = ref lines[i];
                if (line.Length > 0)
                {
                    line = "    " + line;
                }
            }

            source = string.Join("\n", lines);
            source = source.TrimStart();
            builder.Append(source);
            builder.AppendLine();

            if (containingType is not null)
            {
                builder.EndGroup();
            }

            if (containedNamespace is not null)
            {
                builder.EndGroup();
            }

            context.AddSource($"{type.typeSymbol.Name}.generated.cs", builder.ToString());
        }

        private static int GetIndentation(string text, string search)
        {
            int index = text.IndexOf(search);
            if (index == -1)
            {
                return 0;
            }

            int indentation = 0;
            for (int i = index - 1; i >= 0; i--)
            {
                char c = text[i];
                if (c == ' ')
                {
                    indentation++;
                }
                else
                {
                    break;
                }
            }

            return indentation;
        }

        private static IReadOnlyList<DataType> GetComplianceRequirements(EntityType type, Compilation compilation)
        {
            IMethodSymbol? describeMethod = null;
            foreach (IMethodSymbol method in type.typeSymbol.GetMethods())
            {
                if (method.Parameters.Length == 1)
                {
                    IParameterSymbol firstParameter = method.Parameters[0];
                    if (firstParameter.RefKind == RefKind.Ref && firstParameter.Type.ToDisplayString() == "Worlds.Archetype")
                    {
                        if (method.MethodKind == MethodKind.ExplicitInterfaceImplementation)
                        {
                            if (method.Name == "Worlds.IEntity.Describe")
                            {
                                describeMethod = method;
                                break;
                            }
                        }
                        else if (method.MethodKind == MethodKind.Ordinary)
                        {
                            if (method.Name == "Describe")
                            {
                                describeMethod = method;
                                break;
                            }
                        }
                    }
                }
            }

            if (describeMethod is not null)
            {
                if (describeMethod.DeclaringSyntaxReferences.Length > 0)
                {
                    SyntaxNode node = describeMethod.DeclaringSyntaxReferences[0].GetSyntax();
                    SemanticModel semanticModel = compilation.GetSemanticModel(type.syntaxNode.SyntaxTree);
                    List<MemberAccessExpressionSyntax> invocations = new();
                    foreach (SyntaxNode descendant in node.DescendantNodes())
                    {
                        if (descendant is MemberAccessExpressionSyntax invocationExpression)
                        {
                            invocations.Add(invocationExpression);
                        }
                    }

                    if (invocations.Count > 0)
                    {
                        List<DataType> requiredTypes = new();
                        foreach (MemberAccessExpressionSyntax invocation in invocations)
                        {
                            foreach (SyntaxNode child in invocation.ChildNodes())
                            {
                                if (child is GenericNameSyntax genericNameSyntax)
                                {
                                    DataKind dataKind = default;
                                    if (genericNameSyntax.Identifier.ToString() == "AddComponentType")
                                    {
                                        dataKind = DataKind.Component;
                                    }
                                    else if (genericNameSyntax.Identifier.ToString() == "AddArrayType")
                                    {
                                        dataKind = DataKind.Array;
                                    }
                                    else if (genericNameSyntax.Identifier.ToString() == "AddTagType")
                                    {
                                        dataKind = DataKind.Tag;
                                    }

                                    if (dataKind != default)
                                    {
                                        foreach (SyntaxNode grandChild in child.ChildNodes())
                                        {
                                            if (grandChild is TypeArgumentListSyntax typeArguments)
                                            {
                                                foreach (TypeSyntax typeArgument in typeArguments.Arguments)
                                                {
                                                    TypeInfo foundTypeInfo = semanticModel.GetTypeInfo(typeArgument);
                                                    string fullTypeName;
                                                    if (foundTypeInfo.Type is not null)
                                                    {
                                                        fullTypeName = foundTypeInfo.Type.GetFullTypeName();
                                                    }
                                                    else
                                                    {
                                                        //full name is required
                                                        //todo: emit a diagnostic asking for full type
                                                        //but then once a full type name is given, how does this
                                                        //generator know its a full type name?
                                                        fullTypeName = typeArgument.ToFullString();
                                                    }

                                                    requiredTypes.Add(new(dataKind, fullTypeName));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return requiredTypes;
                    }
                }
            }

            return Array.Empty<DataType>();
        }

        private static string? GetComplianceChecks(EntityType type, int indent, IReadOnlyList<DataType> requiredTypes)
        {
            if (requiredTypes.Count > 0)
            {
                SourceBuilder builder = new();
                builder.Indent(indent);
                builder.Append("Schema schema = world.Schema;");
                builder.AppendLine();
                builder.Append("Definition definition = world.GetDefinition(value);");
                builder.AppendLine();
                foreach (DataType dataType in requiredTypes)
                {
                    builder.Append("if (!definition.Contains");
                    builder.Append(dataType.kind.ToString());
                    builder.Append('<');
                    builder.Append(dataType.fullTypeName);
                    builder.Append(">(schema))");
                    builder.AppendLine();
                    builder.BeginGroup();
                    {
                        builder.AppendLine("return false;");
                    }
                    builder.EndGroup();
                    builder.AppendLine();
                }

                builder.Length -= 2;
                return builder.ToString();
            }
            else
            {
                return null;
            }
        }

        private static string GetDisposeMethod(EntityType type, int indent)
        {
            bool hasDisposeMethod = false;
            foreach (IMethodSymbol method in type.typeSymbol.GetMethods())
            {
                if (method.Name == "Dispose" && method.Parameters.Length == 0)
                {
                    hasDisposeMethod = true;
                    break;
                }
            }

            if (hasDisposeMethod)
            {
                return string.Empty;
            }
            else
            {
                SourceBuilder builder = new();
                builder.Indent(indent);

                builder.AppendLine();
                builder.Append("/// <inheritdoc/>");

                builder.AppendLine();
                builder.Append("public readonly void Dispose()");
                builder.AppendLine();
                builder.BeginGroup();
                {
                    builder.AppendLine("world.DestroyEntity(value);");
                }
                builder.EndGroup();
                return builder.ToString();
            }
        }

        private static string GetInterfaces(EntityType type)
        {
            //check if type already implements IEquatable
            if (type.typeSymbol.HasInterface($"System.IEquatable<{type.fullTypeName}>"))
            {
                return string.Empty;
            }

            return ", IEquatable<{{TypeName}}>";
        }

        private static string GetEqualityMethods(EntityType type, int indent)
        {
            if (type.typeSymbol.HasInterface($"System.IEquatable<{type.fullTypeName}>"))
            {
                return string.Empty;
            }

            SourceBuilder builder = new();
            builder.Indent(indent);

            builder.AppendLine();
            builder.Append("/// <inheritdoc/>");

            builder.AppendLine();
            builder.AppendLine("public readonly override bool Equals(object? obj)");
            builder.BeginGroup();
            {
                builder.Append("return obj is ");
                builder.Append(type.fullTypeName);
                builder.Append(" entity && Equals(entity);");
                builder.AppendLine();
            }
            builder.EndGroup();

            builder.AppendLine();
            builder.Append("/// <inheritdoc/>");

            builder.AppendLine();
            builder.AppendLine($"public readonly bool Equals({type.fullTypeName} other)");
            builder.BeginGroup();
            {
                builder.AppendLine("return world == other.world && value == other.value;");
            }
            builder.EndGroup();

            builder.AppendLine();
            builder.Append("/// <inheritdoc/>");

            builder.AppendLine();
            builder.AppendLine("public readonly override int GetHashCode()");
            builder.BeginGroup();
            {
                builder.AppendLine("int hash = 17;");
                builder.AppendLine("hash = hash * 31 + world.GetHashCode();");
                builder.AppendLine("hash = hash * 31 + (int)value;");
                builder.AppendLine("return hash;");
            }
            builder.EndGroup();

            builder.AppendLine();
            builder.Append("/// <inheritdoc/>");

            builder.AppendLine();
            builder.Append("public static bool operator ==(");
            builder.Append(type.fullTypeName);
            builder.Append(" left, ");
            builder.Append(type.fullTypeName);
            builder.Append(" right)");
            builder.AppendLine();
            builder.BeginGroup();
            {
                builder.AppendLine("return left.Equals(right);");
            }
            builder.EndGroup();

            builder.AppendLine();
            builder.Append("/// <inheritdoc/>");

            builder.AppendLine();
            builder.Append("public static bool operator !=(");
            builder.Append(type.fullTypeName);
            builder.Append(" left, ");
            builder.Append(type.fullTypeName);
            builder.Append(" right)");
            builder.AppendLine();
            builder.BeginGroup();
            {
                builder.AppendLine("return !(left == right);");
            }
            builder.EndGroup();
            return builder.ToString();
        }

        public static string? GetStaticDefinitionGetter(int indent, IReadOnlyList<DataType> requiredTypes)
        {
            if (requiredTypes.Count > 0)
            {
                List<DataType> componentTypes = new();
                List<DataType> arrayTypes = new();
                List<DataType> tagTypes = new();
                for (int i = 0; i < requiredTypes.Count; i++)
                {
                    DataType requiredType = requiredTypes[i];
                    if (requiredType.kind == DataKind.Component)
                    {
                        componentTypes.Add(requiredType);
                    }
                    else if (requiredType.kind == DataKind.Array)
                    {
                        arrayTypes.Add(requiredType);
                    }
                    else if (requiredType.kind == DataKind.Tag)
                    {
                        tagTypes.Add(requiredType);
                    }
                }

                SourceBuilder builder = new();
                builder.Indent(indent);

                builder.AppendLine();
                builder.Append("/// <summary>");
                builder.AppendLine();
                builder.Append("/// Retrieves the definition that this entity argues in order for it to be compliant.");
                builder.AppendLine();
                builder.Append("/// </summary>");
                builder.AppendLine();
                builder.Append("public static Definition GetDescribedDefinition(World world)");
                builder.AppendLine();
                builder.BeginGroup();
                {
                    builder.Append("Schema schema = world.Schema;");
                    builder.AppendLine();
                    builder.Append("BitMask componentTypes = new(");
                    for (int i = 0; i < componentTypes.Count; i++)
                    {
                        DataType requiredType = componentTypes[i];
                        builder.Append("schema.GetComponentType<");
                        builder.Append(requiredType.fullTypeName);
                        builder.Append(">()");
                        if (i != componentTypes.Count - 1)
                        {
                            builder.Append(',');
                            builder.Append(' ');
                        }
                    }

                    builder.Append(");");
                    builder.AppendLine();
                    builder.Append("BitMask arrayTypes = new(");
                    for (int i = 0; i < arrayTypes.Count; i++)
                    {
                        DataType requiredType = arrayTypes[i];
                        builder.Append("schema.GetArrayType<");
                        builder.Append(requiredType.fullTypeName);
                        builder.Append(">()");
                        if (i != arrayTypes.Count - 1)
                        {
                            builder.Append(',');
                            builder.Append(' ');
                        }
                    }

                    builder.Append(");");
                    builder.AppendLine();
                    builder.Append("BitMask tagTypes = new(");
                    for (int i = 0; i < tagTypes.Count; i++)
                    {
                        DataType requiredType = tagTypes[i];
                        builder.Append("schema.GetTagType<");
                        builder.Append(requiredType.fullTypeName);
                        builder.Append(">()");
                        if (i != tagTypes.Count - 1)
                        {
                            builder.Append(',');
                            builder.Append(' ');
                        }
                    }

                    builder.Append(");");
                    builder.AppendLine();
                    builder.Append("return new(componentTypes, arrayTypes, tagTypes);");
                    builder.AppendLine();
                }
                builder.EndGroup();
                return builder.ToString();
            }
            else
            {
                return null;
            }
        }

        public static string GetUntilCompliantMethod(int indent, IReadOnlyList<DataType> requiredTypes)
        {
            if (requiredTypes.Count == 0)
            {
                return "await Task.CompletedTask;";
            }

            List<DataType> componentTypes = new();
            List<DataType> arrayTypes = new();
            List<DataType> tagTypes = new();
            for (int i = 0; i < requiredTypes.Count; i++)
            {
                DataType requiredType = requiredTypes[i];
                if (requiredType.kind == DataKind.Component)
                {
                    componentTypes.Add(requiredType);
                }
                else if (requiredType.kind == DataKind.Array)
                {
                    arrayTypes.Add(requiredType);
                }
                else if (requiredType.kind == DataKind.Tag)
                {
                    tagTypes.Add(requiredType);
                }
            }

            SourceBuilder builder = new();
            builder.Indent(indent);

            builder.Append("Definition currentDefinition;");
            builder.AppendLine();
            builder.Append("Schema schema = world.Schema;");
            builder.AppendLine();

            if (componentTypes.Count > 0)
            {
                builder.Append("BitMask componentTypes = new(");
                for (int i = 0; i < componentTypes.Count; i++)
                {
                    DataType componentType = componentTypes[i];
                    builder.Append("schema.GetComponentType<");
                    builder.Append(componentType.fullTypeName);
                    builder.Append(">()");

                    if (i != componentTypes.Count - 1)
                    {
                        builder.Append(',');
                        builder.Append(' ');
                    }
                }

                builder.Append(");");
                builder.AppendLine();
                builder.Append("BitMask currentComponentTypes;");
                builder.AppendLine();
            }

            if (arrayTypes.Count > 0)
            {
                builder.Append("BitMask arrayTypes = new(");
                for (int i = 0; i < arrayTypes.Count; i++)
                {
                    DataType requiredType = arrayTypes[i];
                    builder.Append("schema.GetArrayType<");
                    builder.Append(requiredType.fullTypeName);
                    builder.Append(">()");

                    if (i != arrayTypes.Count - 1)
                    {
                        builder.Append(',');
                        builder.Append(' ');
                    }
                }

                builder.Append(");");
                builder.AppendLine();
                builder.Append("BitMask currentArrayTypes;");
                builder.AppendLine();
            }

            if (tagTypes.Count > 0)
            {
                builder.Append("BitMask tagTypes = new(");
                for (int i = 0; i < tagTypes.Count; i++)
                {
                    DataType requiredType = tagTypes[i];
                    builder.Append("schema.GetTagType<");
                    builder.Append(requiredType.fullTypeName);
                    builder.Append(">()");

                    if (i != tagTypes.Count - 1)
                    {
                        builder.Append(',');
                        builder.Append(' ');
                    }
                }

                builder.Append(");");
                builder.AppendLine();
                builder.Append("BitMask currentTagTypes;");
                builder.AppendLine();
            }

            builder.Append("do");
            builder.AppendLine();

            builder.BeginGroup();
            {
                builder.Append("currentDefinition = world.GetDefinition(value);");
                builder.AppendLine();

                if (componentTypes.Count > 0)
                {
                    builder.Append("currentComponentTypes = currentDefinition.componentTypes;");
                    builder.AppendLine();
                }

                if (arrayTypes.Count > 0)
                {
                    builder.Append("currentArrayTypes = currentDefinition.arrayTypes;");
                    builder.AppendLine();
                }

                if (tagTypes.Count > 0)
                {
                    builder.Append("currentTagTypes = currentDefinition.tagTypes;");
                    builder.AppendLine();
                }

                builder.Append("if (");

                bool buildingCondition = false;
                if (componentTypes.Count > 0)
                {
                    builder.Append("currentComponentTypes.ContainsAll(componentTypes)");
                    buildingCondition = true;
                }

                if (arrayTypes.Count > 0)
                {
                    if (buildingCondition)
                    {
                        builder.Append(" && ");
                    }

                    builder.Append("currentArrayTypes.ContainsAll(arrayTypes)");
                    buildingCondition = true;
                }

                if (tagTypes.Count > 0)
                {
                    if (buildingCondition)
                    {
                        builder.Append(" && ");
                    }

                    builder.Append("currentTagTypes.ContainsAll(tagTypes)");
                    buildingCondition = true;
                }

                builder.Append(')');
                builder.AppendLine();
                builder.BeginGroup();
                {
                    builder.Append("return;");
                    builder.AppendLine();
                }
                builder.EndGroup();
                builder.AppendLine();
                builder.Append("update();");
                builder.AppendLine();
                builder.Append("await Task.Yield();");
                builder.AppendLine();
                builder.Append("cancellationToken.ThrowIfCancellationRequested();");
                builder.AppendLine();
            }
            builder.EndGroup();
            builder.Append("while (true);");
            return builder.ToString();
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.StructDeclaration);
        }

        private static (EntityType? result, Diagnostic? diagnostic) Transform(GeneratorSyntaxContext syntaxContext, CancellationToken token)
        {
            StructDeclarationSyntax node = (StructDeclarationSyntax)syntaxContext.Node;
            ITypeSymbol? typeSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(node);
            if (typeSymbol is null)
            {
                return default;
            }

            if (typeSymbol is INamedTypeSymbol namedSymbol && namedSymbol.Arity > 0)
            {
                return default;
            }

            if (typeSymbol.HasInterface("Worlds.IEntity"))
            {
                if (!node.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    string message = $"The type {typeSymbol.Name} must be partial when inheriting another type";
                    string title = "Missing partial keyword";
                    Diagnostic diagnostic = Diagnostic.Create("W0003", "Inheritance", message, DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1, false, title, location: node.GetLocation());
                    return (null, diagnostic);
                }

                string fullTypeName = typeSymbol.GetFullTypeName();
                return (new(node, typeSymbol, fullTypeName), null);
            }

            return default;
        }
    }
}