using System.IO;

public static class ComponentQuery
{
    private const string TypeName = "ComponentQuery";

    public static void Generate()
    {
        string template = File.ReadAllText("ComponentQuery.cs.template");
        for (uint i = 0; i < 16; i++)
        {
            string source = template;
            source = source.Replace("{{TypeName}}", TypeName);
            source = source.Replace("{{GenericTypeArguments}}", SharedFunctions.GetGenericTypeArguments(i));
            source = source.Replace("{{TypeConstraints}}", SharedFunctions.GetTypeConstraints(i));
            File.WriteAllText($"{TypeName}{i + 1}.cs", source);
        }
    }
}