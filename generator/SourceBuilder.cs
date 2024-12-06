using System.Collections.Generic;
using System.Text;

namespace Worlds.TypeTableGenerator
{
    public class SourceBuilder
    {
        private readonly StringBuilder builder = new();
        private int indentation;

        public IEnumerable<string> Lines
        {
            get
            {
                string[] lines = builder.ToString().Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    yield return lines[i].TrimEnd('\r');
                }
            }
        }

        public override string ToString()
        {
            return builder.ToString();
        }

        public void Clear()
        {
            indentation = 0;
            builder.Clear();
        }

        public void BeginGroup()
        {
            AppendIndentation();
            builder.Append('{');
            builder.AppendLine();
            indentation++;
        }

        public void EndGroup()
        {
            indentation--;
            AppendIndentation();
            builder.Append('}');
            builder.AppendLine();
        }

        public void AppendIndentation()
        {
            for (int i = 0; i < indentation; i++)
            {
                builder.Append("    ");
            }
        }

        public void AppendLine(object text)
        {
            AppendIndentation();
            builder.AppendLine(text.ToString());
        }

        public void AppendLine()
        {
            builder.AppendLine();
        }
    }
}