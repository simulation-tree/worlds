using System.Text;

public static class SharedFunctions
{
    private const char GenericTypePrefix = 'C';
    private const string ComponentOffsetType = "uint";
    private const string ComponentOffsetFieldName = "componentOffset";
    private const string ComponentVariableName = "component";

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

    public static string DeclareComponentQueryEnumeratorFields(uint count, int indent)
    {
        // declare component types and offsets
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append("private readonly ");
            builder.Append(ComponentOffsetType);
            builder.Append(' ');
            builder.Append(ComponentOffsetFieldName);
            builder.Append(i);
            builder.Append(';');

            if (i < count + 1)
            {
                builder.Append('\r');
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
            builder.Append(' ');
            builder.Append(ComponentVariableName);
            builder.Append(i);
            builder.Append(" = ref componentRow.Read<");
            builder.Append(GenericTypePrefix);
            builder.Append(i);
            builder.Append(">(");
            builder.Append(ComponentOffsetFieldName);
            builder.Append(i);
            builder.Append(");");

            if (i < count + 1)
            {
                builder.Append('\r');
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
            builder.Append("ref ");
            builder.Append(ComponentVariableName);
            builder.Append(i);
            builder.Append(", ");
        }

        builder.Length -= 2;
        return builder.ToString();
    }

    public static string AssignComponentOffsets(uint count, int indent)
    {
        StringBuilder builder = new();
        for (uint i = 1; i <= count + 1; i++)
        {
            builder.Append(ComponentOffsetFieldName);
            builder.Append(i);
            builder.Append(" = schema.schema->componentOffsets[schema.GetComponentType<C");
            builder.Append(i);
            builder.Append(">()];");

            if (i < count + 1)
            {
                builder.Append('\r');
                builder.Append('\n');
                builder.Append(new string(' ', indent));
            }
        }

        return builder.ToString();
    }
}