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
            source = source.Replace("{{DeclareComponentTypeFields}}", SharedFunctions.DeclareComponentTypeFields(i));
            source = source.Replace("{{AssignComponentTypeFields}}", SharedFunctions.AssignComponentTypeFields(i));
            source = source.Replace("{{AssignComponentSpans}}", SharedFunctions.AssignComponentSpans(i));
            source = source.Replace("{{TypeParameters}}", SharedFunctions.GetTypeParameters(i));
            source = source.Replace("{{RefIndexingComponent}}", SharedFunctions.GetRefIndexingComponent(i));
            source = source.Replace("{{DeclareComponentSpans}}", SharedFunctions.DeclareComponentSpans(i));
            File.WriteAllText($"{TypeName}{i + 1}.cs", source);
        }
    }
}