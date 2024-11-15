using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace Generator
{
    public class Input
    {
        public readonly InvocationExpressionSyntax invocation;
        public readonly IMethodSymbol methodSymbol;
        public readonly SemanticModel semanticModel;

        public Input(InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol, SemanticModel semanticModel)
        {
            this.invocation = invocation;
            this.methodSymbol = methodSymbol;
            this.semanticModel = semanticModel;
        }
    }

    [Generator(LanguageNames.CSharp)]
    public class TypeGenerator : IIncrementalGenerator
    {
        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<Input?> invocations = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            IncrementalValueProvider<(ImmutableArray<Input?>, AnalyzerConfigOptionsProvider)> provider = invocations.Collect().Combine(context.AnalyzerConfigOptionsProvider);
            context.RegisterSourceOutput(provider, Generate);
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.InvocationExpression);
        }

        private static Input? Transform(GeneratorSyntaxContext context, CancellationToken token)
        {
            if (context.Node is InvocationExpressionSyntax invocation)
            {
                SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    return new(invocation, methodSymbol, context.SemanticModel);
                }
            }

            return null;
        }

        private static void Generate(SourceProductionContext context, (ImmutableArray<Input?> inputs, AnalyzerConfigOptionsProvider options) input)
        {
            (ImmutableArray<Input?> inputs, AnalyzerConfigOptionsProvider options) = input;
            const string TypeName = "TypeTable";
            int indentation = 0;
            StringBuilder builder = new();

            //start namespace
            AppendIndentation();
            builder.Append("namespace Simulation");
            builder.AppendLine();

            AppendIndentation();
            builder.Append('{');
            builder.AppendLine();

            indentation++;

            //start type
            AppendIndentation();
            builder.Append("public static class ");
            builder.Append(TypeName);
            builder.AppendLine();

            AppendIndentation();
            builder.Append('{');
            builder.AppendLine();

            indentation++;

            //field declarations
            HashSet<ITypeSymbol> componentTypes = [];
            HashSet<ITypeSymbol> arrayTypes = [];
            foreach (Input? invocation in inputs)
            {
                if (invocation is not null)
                {
                    SymbolInfo symbolInfo = invocation.semanticModel.GetSymbolInfo(invocation.invocation);
                    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                    {
                        string containingNamespace = methodSymbol.ContainingType.ContainingNamespace.ToString();
                        string containingTypeName = methodSymbol.ContainingType.Name.ToString();
                        string methodName = methodSymbol.Name;
                        if (containingNamespace == "Simulation")
                        {
                            if (containingTypeName == "Definition")
                            {
                                if (methodName == "AddComponentType" || methodName == "AddComponentTypes")
                                {
                                    if (methodSymbol.Arity > 0)
                                    {
                                        foreach (ITypeSymbol type in methodSymbol.TypeArguments)
                                        {
                                            if (type.TypeKind == TypeKind.TypeParameter) continue;
                                            if (componentTypes.Add(type))
                                            {
                                                string fullTypeName = type.ToDisplayString();
                                                AppendComponentTypeRegistration(fullTypeName);
                                            }
                                        }
                                    }
                                }
                                else if (methodName == "AddArrayType" || methodName == "AddArrayTypes")
                                {
                                    if (methodSymbol.Arity > 0)
                                    {
                                        foreach (ITypeSymbol type in methodSymbol.TypeArguments)
                                        {
                                            if (type.TypeKind == TypeKind.TypeParameter) continue;
                                            if (arrayTypes.Add(type))
                                            {
                                                string fullTypeName = type.ToDisplayString();
                                                AppendArrayTypeRegistration(fullTypeName);
                                            }
                                        }
                                    }
                                }
                            }
                            else if (containingTypeName == "Entity" || containingTypeName == "World" || containingTypeName == "Operation" || containingTypeName == "Instruction")
                            {
                                if (methodName == "AddComponent" || methodName == "GetComponent" || methodName == "SetComponent" || methodName == "RemoveComponent" || methodName == "ContainsComponent")
                                {
                                    if (methodSymbol.Arity == 1)
                                    {
                                        ITypeSymbol genericType = methodSymbol.TypeArguments[0];
                                        if (genericType.TypeKind == TypeKind.TypeParameter) continue;
                                        if (componentTypes.Add(genericType))
                                        {
                                            string fullTypeName = genericType.ToDisplayString();
                                            AppendComponentTypeRegistration(fullTypeName);
                                        }

                                        continue;
                                    }
                                }
                                else if (methodName == "GetArray" || methodName == "CreateArray" || methodName == "DestroyArray" || methodName == "ContainsArray" || methodName == "SetArrayElement" || methodName == "SetArrayElements" || methodName == "GetArrayLength" || methodName == "ResizeArray")
                                {
                                    if (methodSymbol.Arity == 1)
                                    {
                                        ITypeSymbol genericType = methodSymbol.TypeArguments[0];
                                        if (genericType.TypeKind == TypeKind.TypeParameter) continue;
                                        if (arrayTypes.Add(genericType))
                                        {
                                            string fullTypeName = genericType.ToDisplayString();
                                            AppendArrayTypeRegistration(fullTypeName);
                                        }

                                        continue;
                                    }
                                }
                            }
                            else if (containingTypeName == "ComponentType")
                            {
                                if (methodName == "Get")
                                {
                                    if (methodSymbol.Arity == 1)
                                    {
                                        ITypeSymbol genericType = methodSymbol.TypeArguments[0];
                                        if (genericType.TypeKind == TypeKind.TypeParameter) continue;
                                        if (componentTypes.Add(genericType))
                                        {
                                            string fullTypeName = genericType.ToDisplayString();
                                            AppendComponentTypeRegistration(fullTypeName);
                                        }

                                        continue;
                                    }
                                }
                            }
                            else if (containingTypeName == "ArrayType")
                            {
                                if (methodName == "Get")
                                {
                                    if (methodSymbol.Arity == 1)
                                    {
                                        ITypeSymbol genericType = methodSymbol.TypeArguments[0];
                                        if (genericType.TypeKind == TypeKind.TypeParameter) continue;
                                        if (arrayTypes.Add(genericType))
                                        {
                                            string fullTypeName = genericType.ToDisplayString();
                                            AppendArrayTypeRegistration(fullTypeName);
                                        }

                                        continue;
                                    }
                                }
                            }
                        }

                        /*
                        AppendIndentation();
                        builder.Append("//");
                        builder.Append(containingNamespace);
                        builder.Append('.');
                        builder.Append(containingTypeName);
                        builder.Append('.');
                        builder.Append(methodName);
                        if (methodSymbol.Arity > 0)
                        {
                            builder.Append('<');
                            foreach (ITypeSymbol type in methodSymbol.TypeArguments)
                            {
                                builder.Append(type.ToDisplayString());
                                builder.Append(", ");
                            }

                            builder.Append('>');
                        }

                        builder.Append('(');

                        //get method declaration from invocation
                        var e = invocation.invocation.Expression;
                        if (e is MemberAccessExpressionSyntax memberAccess)
                        {
                            foreach (SyntaxNode child in memberAccess.DescendantNodes())
                            {
                                builder.Append(child.ToString());
                                builder.Append(" (");
                                builder.Append(child.GetType());
                                builder.Append(")");
                                builder.Append(", ");
                            }
                        }

                        builder.Append(')');
                        builder.AppendLine();
                        */
                    }
                }
            }

            indentation--;

            //finish type
            AppendIndentation();
            builder.Append('}');
            builder.AppendLine();

            indentation--;

            //finish namespace
            AppendIndentation();
            builder.Append('}');
            builder.AppendLine();

            context.AddSource($"{TypeName}.generated.cs", builder.ToString());

            void AppendIndentation()
            {
                for (int i = 0; i < indentation; i++)
                {
                    builder.Append("    ");
                }
            }

            void AppendComponentTypeRegistration(string fullTypeName)
            {
                AppendIndentation();
                builder.Append("public static readonly ComponentType @component_");
                builder.Append(fullTypeName.Replace(".", "_"));
                builder.Append(" = ComponentType.Register<");
                builder.Append(fullTypeName);
                builder.Append(">();");
                builder.AppendLine();
            }

            void AppendArrayTypeRegistration(string fullTypeName)
            {
                AppendIndentation();
                builder.Append("public static readonly ArrayType @array_");
                builder.Append(fullTypeName.Replace(".", "_"));
                builder.Append(" = ArrayType.Register<");
                builder.Append(fullTypeName);
                builder.Append(">();");
                builder.AppendLine();
            }
        }
    }
}