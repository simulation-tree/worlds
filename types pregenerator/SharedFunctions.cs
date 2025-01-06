using System.Text;

public static class SharedFunctions
{
    private const char GenericTypePrefix = 'C';

    public static string GetGenericTypeArguments(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(", ");
        }

        builder.Length -= 2;
        return builder.ToString();
    }

    public static string GetTypeConstraints(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("where ");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(" : ");
            builder.Append("unmanaged ");
        }

        builder.Length -= 1;
        return builder.ToString();
    }

    public static string GetDefaultTypes(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("default(");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append("), ");
        }

        builder.Length -= 2;
        return builder.ToString();
    }

    public static string GetTypeParametersSignature(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(" c");
            builder.Append(i);
            builder.Append(", ");
        }

        builder.Length -= 2;
        return builder.ToString();
    }

    public static string GetTypeParameters(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append('c');
            builder.Append(i);
            builder.Append(", ");
        }

        builder.Length -= 2;
        return builder.ToString();
    }

    public static string DeclareComponentFields(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("            ");
            builder.Append("public readonly ");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(" c");
            builder.Append(i);
            builder.Append(";\n");
        }

        builder.Length -= 1;
        return builder.ToString();
    }

    public static string AssignComponentFields(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("                ");
            builder.Append('c');
            builder.Append(i);
            builder.Append(" = entity.AsEntity().GetComponent<");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(">();\n");
        }

        builder.Length -= 1;
        return builder.ToString();
    }

    public static string DeclareComponentTypeFields(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("            ");
            builder.Append("private readonly ComponentType c");
            builder.Append(i);
            builder.Append(";\n");
        }

        builder.Length -= 1;
        return builder.ToString();
    }

    public static string AssignComponentTypeFields(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("                    ");
            builder.Append('c');
            builder.Append(i);
            builder.Append(" = schema.GetComponent<");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(">();\n");
        }

        builder.Length -= 1;
        return builder.ToString();
    }

    public static string GetRefIndexingComponent(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("ref span");
            builder.Append(i);
            builder.Append("[entityIndex - 1]");
            builder.Append(", ");
        }

        builder.Length -= 2;
        return builder.ToString();
    }

    public static string DeclareComponentSpans(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("            ");
            builder.Append("private USpan<");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append("> span");
            builder.Append(i);
            builder.Append(";\n");
        }

        builder.Length -= 1;
        return builder.ToString();
    }

    public static string AssignComponentSpans(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("                ");
            builder.Append("span");
            builder.Append(i);
            builder.Append(" = chunk.GetComponents<");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(">(c");
            builder.Append(i);
            builder.Append(");\n");
        }

        builder.Length -= 1;
        return builder.ToString();
    }

    public static string DescribeEntity(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("            ");
            builder.Append("archetype.AddComponentType<");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(">();\n");
        }

        builder.Length -= 1;
        return builder.ToString();
    }
}