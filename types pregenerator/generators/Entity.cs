using System.IO;

public static class Entity
{
    private const string TypeName = "Entity";

    public static void Generate()
    {
        string template = File.ReadAllText("Entity.cs.template");
        for (uint i = 0; i < 16; i++)
        {
            string source = template;
            source = source.Replace("{{TypeName}}", TypeName);
            source = source.Replace("{{GenericTypeArguments}}", SharedFunctions.GetGenericTypeArguments(i));
            source = source.Replace("{{TypeConstraints}}", SharedFunctions.GetTypeConstraints(i));
            source = source.Replace("{{DefaultTypes}}", SharedFunctions.GetDefaultTypes(i));
            source = source.Replace("{{TypeParametersSignature}}", SharedFunctions.GetTypeParametersSignature(i));
            source = source.Replace("{{TypeParameters}}", SharedFunctions.GetTypeParameters(i));
            source = source.Replace("{{DeclareComponentFields}}", SharedFunctions.DeclareComponentFields(i));
            source = source.Replace("{{AssignComponentFields}}", SharedFunctions.AssignComponentFields(i));
            source = source.Replace("{{DescribeEntity}}", SharedFunctions.DescribeEntity(i));
            File.WriteAllText($"{TypeName}{i + 1}.cs", source);
        }
    }
}