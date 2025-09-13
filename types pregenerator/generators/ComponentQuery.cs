using System;
using System.IO;
using static SharedFunctions;

public static class ComponentQuery
{
    private const string TypeName = "ComponentQuery";

    public static void Generate()
    {
        string repositoryRoot = GetRepositoryRoot();
        string destinationFolder = Path.Combine(repositoryRoot, "core", "Component Query");
        string template = File.ReadAllText("ComponentQuery.cs.template");
        for (uint i = 0; i < 16; i++)
        {
            string source = template;
            source = source.Replace("{{TypeName}}", TypeName);
            source = source.Replace("{{GenericTypeArguments}}", GenericTypeArguments(i));
            source = source.Replace("{{TypeConstraints}}", TypeConstraints(i));
            source = source.Replace("{{DeclareComponentQueryEnumeratorFields}}", DeclareComponentQueryEnumeratorFields(i, GetIndent(source, "{{DeclareComponentQueryEnumeratorFields}}")));
            source = source.Replace("{{AssignComponentOffsets}}", AssignComponentOffsets(i, GetIndent(source, "{{AssignComponentOffsets}}")));
            source = source.Replace("{{AccessComponents}}", AccessComponents(i, GetIndent(source, "{{AccessComponents}}")));
            source = source.Replace("{{ReferenceComponents}}", ReferenceComponents(i));
            string fileName = $"{TypeName}{i + 1}.cs";
            string filePath = Path.Combine(destinationFolder, fileName);
            File.WriteAllText(filePath, source);
        }
    }

    private static string GetRepositoryRoot()
    {
        string? directory = Environment.CurrentDirectory;
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory, ".git")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        throw new InvalidOperationException("Could not find repository root");
    }
}