using System.IO;
using static SharedFunctions;

//not really used anymore
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
            source = source.Replace("{{GenericTypeArguments}}", GenericTypeArguments(i));
            source = source.Replace("{{TypeConstraints}}", TypeConstraints(i));
            source = source.Replace("{{DefaultTypes}}", DefaultTypes(i));
            source = source.Replace("{{TypeParametersSignature}}", TypeParametersSignature(i));
            source = source.Replace("{{TypeParameters}}", TypeParameters(i));
            source = source.Replace("{{DeclareComponentFields}}", DeclareComponentFields(i, GetIndent(source, "{{DeclareComponentFields}}")));
            source = source.Replace("{{AssignComponentFields}}", AssignComponentFields(i, GetIndent(source, "{{AssignComponentFields}}")));
            source = source.Replace("{{DescribeEntity}}", DescribeEntity(i, GetIndent(source, "{{DescribeEntity}}")));
            File.WriteAllText($"{TypeName}{i + 1}.cs", source);
        }
    }
}