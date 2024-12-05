using System.IO;
using System.Text;

const string TypeName = "ComponentQuery";

string template = File.ReadAllText("ComponentQuery.cs.template");
for (uint i = 0; i < 16; i++)
{
    string source = template;
    source = source.Replace("{{TypeName}}", TypeName);
    source = source.Replace("{{GenericTypeArguments}}", GetGenericTypeArguments(i));
    source = source.Replace("{{TypeConstraints}}", GetTypeConstraints(i));
    File.WriteAllText($"{TypeName}{i + 1}.cs", source);
}

string GetGenericTypeArguments(uint count)
{
    StringBuilder builder = new();
    for (uint i = 1; i <= count + 1; i++)
    {
        builder.Append('C');
        builder.Append(i);
        if (i <= count)
        {
            builder.Append(", ");
        }
    }

    return builder.ToString();
}

string GetTypeConstraints(uint count)
{
    StringBuilder builder = new();
    for (uint i = 1; i <= count + 1; i++)
    {
        builder.Append("where C");
        builder.Append(i);
        builder.Append(" : ");
        builder.Append("unmanaged");
        if (i <= count)
        {
            builder.Append(' ');
        }
    }

    return builder.ToString();
}