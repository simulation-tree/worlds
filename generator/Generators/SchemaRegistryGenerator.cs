using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Types.Generator;

namespace Worlds.TypeTableGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class SchemaRegistryGenerator : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();
        private const string TypeName = "SchemaRegistry";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, Generate);
        }

        private static void Generate(SourceProductionContext context, Compilation compilation)
        {
            context.AddSource($"{TypeName}.generated.cs", Generate(compilation));
        }

        public static string Generate(Compilation compilation)
        {
            HashSet<ITypeSymbol> componentTypes = [];
            HashSet<ITypeSymbol> arrayElementTypes = [];
            HashSet<ITypeSymbol> tagTypes = [];
            compilation.CollectTypeSymbols(componentTypes, arrayElementTypes, tagTypes);

            source.Clear();
            source.Clear();
            source.AppendLine("using System.Diagnostics;");
            source.AppendLine();
            source.AppendLine($"namespace {SharedFunctions.Namespace}");
            source.BeginGroup();
            {
                source.AppendLine($"internal static partial class {TypeName}");
                source.BeginGroup();
                {
                    source.AppendLine("/// <summary>");
                    source.AppendLine("/// Loads all relevant component and array types into the given schema.");
                    source.AppendLine("/// </summary>");
                    source.AppendLine($"public static void Load(Schema schema)");
                    source.BeginGroup();
                    {
                        foreach (ITypeSymbol componentType in componentTypes)
                        {
                            AppendComponentRegistration(componentType);
                        }

                        foreach (ITypeSymbol arrayElementType in arrayElementTypes)
                        {
                            AppendArrayElementRegistration(arrayElementType);
                        }

                        foreach (ITypeSymbol tagType in tagTypes)
                        {
                            AppendTagRegistration(tagType);
                        }
                    }
                    source.EndGroup();
                    source.AppendLine();
                    source.AppendLine("/// <summary>");
                    source.AppendLine("/// Retrieves a schema containing all possible component and array types.");
                    source.AppendLine("/// </summary>");
                    source.AppendLine($"public static Schema Get()");
                    source.BeginGroup();
                    {
                        source.AppendLine("Schema schema = new();");
                        source.AppendLine("Load(schema);");
                        source.AppendLine("return schema;");
                    }
                    source.EndGroup();
                }
                source.EndGroup();
            }
            source.EndGroup();
            return source.ToString();
        }

        private static void AppendComponentRegistration(ITypeSymbol componentType)
        {
            source.AppendLine($"schema.RegisterComponent<{componentType.GetFullTypeName()}>();");
        }

        private static void AppendArrayElementRegistration(ITypeSymbol arrayElementType)
        {
            source.AppendLine($"schema.RegisterArrayElement<{arrayElementType.GetFullTypeName()}>();");
        }

        private static void AppendTagRegistration(ITypeSymbol tagType)
        {
            source.AppendLine($"schema.RegisterTag<{tagType.GetFullTypeName()}>();");
        }
    }
}