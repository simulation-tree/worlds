using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Worlds.Generators
{
    [Generator(LanguageNames.CSharp)]
    public class SchemaLoaderGenerator : IIncrementalGenerator
    {
        private const string TypeName = "SchemaLoader";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, Generate);
        }

        private void Generate(SourceProductionContext context, Compilation compilation)
        {
            if (compilation.GetEntryPoint(context.CancellationToken) is not null)
            {
                List<SchemaBankGenerator.Input> inputs = new();
                foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
                {
                    SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
                    inputs.Add(new(syntaxTree.GetRoot(), semanticModel));
                }

                SchemaBankGenerator.TryGenerate(inputs, out string typeName, out _);
                context.AddSource($"{TypeName}.generated.cs", Generate(compilation, typeName));
            }
        }

        private static string Generate(Compilation compilation, string schemaBankTypeName)
        {
            string? assemblyName = compilation.AssemblyName;
            SourceBuilder builder = new();
            builder.AppendLine("using Types;");
            builder.AppendLine("using Worlds;");
            builder.AppendLine();

            if (assemblyName is not null)
            {
                builder.Append("namespace ");
                builder.Append(assemblyName);
                builder.AppendLine();
                builder.BeginGroup();
            }

            builder.AppendLine("/// <summary>");
            builder.AppendLine("/// Contains a method for building a schema containing all components,");
            builder.AppendLine("/// arrays and tag types that are mentioned in the codebase.");
            builder.AppendLine("/// </summary>");

            builder.Append("public static class ");
            builder.Append(TypeName);
            builder.AppendLine();
            builder.BeginGroup();
            {
                builder.AppendLine("/// <summary>");
                builder.AppendLine("/// Loads all mentioned components, arrays and tag");
                builder.AppendLine("/// types into the given schema.");
                builder.AppendLine("/// </summary>");

                builder.AppendLine("public static void Load(Schema schema)");
                builder.BeginGroup();
                {
                    List<ITypeSymbol> schemaBankTypes = new();
                    foreach (ITypeSymbol type in compilation.GetAllTypes())
                    {
                        if (!type.HasInterface(SchemaBankGenerator.SchemaBankInterfaceName))
                        {
                            continue;
                        }

                        if (type.IsRefLikeType)
                        {
                            builder.AppendLine($"//skipped {type.GetFullTypeName()} because its a ref like type");
                            continue;
                        }

                        if (type.DeclaredAccessibility == Accessibility.Private || type.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                        {
                            builder.AppendLine($"//skipped {type.GetFullTypeName()} because its accessibility is {type.DeclaredAccessibility}");
                            continue;
                        }

                        if (type is INamedTypeSymbol namedType)
                        {
                            if (namedType.IsGenericType)
                            {
                                builder.AppendLine($"//skipped {type.GetFullTypeName()} because its a generic type");
                                continue;
                            }
                        }

                        schemaBankTypes.Add(type);
                    }

                    int offset = 0;
                    if (!string.IsNullOrEmpty(schemaBankTypeName))
                    {
                        AppendLoadingSchemaBank(builder, schemaBankTypeName, 0);
                        offset++;
                    }

                    for (int i = 0; i < schemaBankTypes.Count; i++)
                    {
                        ITypeSymbol schemaBankType = schemaBankTypes[i];
                        AppendLoadingSchemaBank(builder, schemaBankType.GetFullTypeName(), i + offset);
                    }
                }
                builder.EndGroup();
                builder.AppendLine();

                builder.AppendLine("/// <summary>");
                builder.AppendLine("/// Retrieves a schema containing all mentioned components, arrays");
                builder.AppendLine("/// and tag types.");
                builder.AppendLine("/// </summary>");

                builder.AppendLine("public static Schema Get()");
                builder.BeginGroup();
                {
                    builder.AppendLine("Schema schema = new();");
                    builder.AppendLine("Load(schema);");
                    builder.AppendLine("return schema;");
                }
                builder.EndGroup();
            }
            builder.EndGroup();

            if (assemblyName is not null)
            {
                builder.EndGroup();
            }

            return builder.ToString();
        }

        private static void AppendLoadingSchemaBank(SourceBuilder source, string schemaBankTypeName, int index)
        {
            source.Append(schemaBankTypeName);
            source.Append(" bank");
            source.Append(index);
            source.Append(" = default;");
            source.AppendLine();

            source.Append("bank");
            source.Append(index);
            source.Append(".Load(schema);");
            source.AppendLine();
        }
    }
}