using System.Text;

namespace Verdant.Serialization;

public static class CanonicalJson
{
    public static byte[] SerializeToUtf8(CanonicalValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var builder = new StringBuilder();
        Write(builder, value);
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public static string Serialize(CanonicalValue value) =>
        Encoding.UTF8.GetString(SerializeToUtf8(value));

    private static void Write(StringBuilder builder, CanonicalValue value)
    {
        switch (value)
        {
            case CanonicalValue.ObjectValue objectValue:
                builder.Append('{');
                var first = true;
                foreach (var member in objectValue.Members.OrderBy(
                    member => member.Key,
                    StringComparer.Ordinal))
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    WriteString(builder, member.Key);
                    builder.Append(':');
                    Write(builder, member.Value);
                    first = false;
                }
                builder.Append('}');
                break;
            case CanonicalValue.Array arrayValue:
                builder.Append('[');
                for (var i = 0; i < arrayValue.Items.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    Write(builder, arrayValue.Items[i]);
                }
                builder.Append(']');
                break;
            case CanonicalValue.StringValue stringValue:
                WriteString(builder, stringValue.Value);
                break;
            case CanonicalValue.BooleanValue booleanValue:
                builder.Append(booleanValue.Value ? "true" : "false");
                break;
            case CanonicalValue.IntegerValue integerValue:
                builder.Append(integerValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;
            case CanonicalValue.Null:
                builder.Append("null");
                break;
            default:
                throw new ArgumentException("Unsupported canonical value.", nameof(value));
        }
    }

    private static void WriteString(StringBuilder builder, string value)
    {
        builder.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (ch < 0x20)
                    {
                        builder.Append("\\u");
                        builder.Append(((int)ch).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(ch);
                    }
                    break;
            }
        }
        builder.Append('"');
    }
}