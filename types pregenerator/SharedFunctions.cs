using System.Text;

public static class SharedFunctions
{
    private const char GenericTypePrefix = 'C';
    private const string ComponentType = "int";
    private const string SpanType = "Span";

    public static int GetIndent(string source, string keyword)
    {
        int index = source.IndexOf(keyword);
        int indent = 0;
        while (index > 0)
        {
            char c = source[index];
            if (c == '\n' || c == '\r')
            {
                break;
            }

            if (c == ' ')
            {
                indent++;
            }

            index--;
        }

        return indent;
    }

    public static string GenericTypeArguments(uint count)
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

    public static string TypeConstraints(uint count)
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

    public static string DefaultTypes(uint count)
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

    public static string TypeParametersSignature(uint count)
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

    public static string TypeParameters(uint count)
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

    public static string DeclareComponentFields(uint count, int indent)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append(new string(' ', indent));
            builder.Append("public readonly ");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(" c");
            builder.Append(i);
            builder.Append(';');

            if (i < count + 1)
            {
                builder.Append('\n');
                builder.Append(new string(' ', indent));
            }
        }

        return builder.ToString();
    }

    public static string AssignComponentFields(uint count, int indent)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append(new string(' ', indent));
            builder.Append('c');
            builder.Append(i);
            builder.Append(" = entity.AsEntity().GetComponent<");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(">();");

            if (i < count + 1)
            {
                builder.Append('\n');
                builder.Append(new string(' ', indent));
            }
        }

        return builder.ToString();
    }

    public static string DeclareComponentTypeFields(uint count, int indent)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("private readonly ");
            builder.Append(ComponentType);
            builder.Append(" c");
            builder.Append(i);
            builder.Append(';');

            if (i < count + 1)
            {
                builder.Append('\n');
                builder.Append(new string(' ', indent));
            }
        }

        return builder.ToString();
    }

    public static string AssignComponentTypeFields(uint count, int indent)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append('c');
            builder.Append(i);
            builder.Append(" = schema.GetComponentTypeIndex<");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(">();");

            if (i < count + 1)
            {
                builder.Append('\n');
                builder.Append(new string(' ', indent));
            }
        }

        return builder.ToString();
    }

    public static string AccessComponents(uint count, int indent)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("ref ");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(" c");
            builder.Append(i);
            builder.Append(" = ref list");
            builder.Append(i);
            builder.Append("[index];");

            if (i < count + 1)
            {
                builder.Append('\n');
                builder.Append(new string(' ', indent));
            }
        }

        return builder.ToString();
    }

    public static string ReferenceComponents(uint count)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("ref c");
            builder.Append(i);
            builder.Append(", ");
        }

        builder.Length -= 2;
        return builder.ToString();
    }

    public static string DeclareComponentLists(uint count, int indent)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("private ");
            builder.Append(SpanType);
            builder.Append('<');
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append("> list");
            builder.Append(i);
            builder.Append(';');

            if (i < count + 1)
            {
                builder.Append('\n');
                builder.Append(new string(' ', indent));
            }
        }

        return builder.ToString();
    }

    public static string AssignComponentLists(uint count, int indent)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("list");
            builder.Append(i);
            builder.Append(" = chunk.GetComponents<");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(">(c");
            builder.Append(i);
            builder.Append(");");

            if (i < count + 1)
            {
                builder.Append('\n');
                builder.Append(new string(' ', indent));
            }
        }

        return builder.ToString();
    }

    public static string DescribeEntity(uint count, int indent)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("archetype.AddComponentType<");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(">();");

            if (i < count + 1)
            {
                builder.Append('\n');
                builder.Append(new string(' ', indent));
            }
        }

        return builder.ToString();
    }
}