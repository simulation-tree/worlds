using Microsoft.CodeAnalysis;
using Types;

namespace Worlds
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
                context.AddSource($"{TypeName}.generated.cs", Generate(compilation));
            }
        }

        private static string Generate(Compilation compilation)
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

            builder.Append("public static class ");
            builder.Append(TypeName);
            builder.AppendLine();
            builder.BeginGroup();
            {
                builder.AppendLine("public static void Load(Schema schema)");
                builder.BeginGroup();
                {
                    foreach (ITypeSymbol type in compilation.GetAllTypes())
                    {
                        if (type.IsRefLikeType)
                        {
                            continue;
                        }

                        if (type.DeclaredAccessibility == Accessibility.Private || type.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                        {
                            continue;
                        }

                        if (type is INamedTypeSymbol namedType)
                        {
                            if (namedType.IsGenericType)
                            {
                                continue;
                            }
                        }

                        if (type.HasInterface("Worlds.ISchemaBank"))
                        {
                            builder.Append("schema.Load<");
                            builder.Append(type.GetFullTypeName());
                            builder.Append(">();");
                            builder.AppendLine();
                        }
                    }
                }
                builder.EndGroup();
                builder.AppendLine();
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
    }
}