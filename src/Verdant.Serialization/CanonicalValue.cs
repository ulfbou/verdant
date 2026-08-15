namespace Verdant.Serialization;

public abstract record CanonicalValue
{
    private CanonicalValue() { }

    public sealed record ObjectValue : CanonicalValue
    {
        public ObjectValue(IEnumerable<KeyValuePair<string, CanonicalValue>> members)
        {
            ArgumentNullException.ThrowIfNull(members);
            var materialized = members.ToArray();
            foreach (var member in materialized)
            {
                ArgumentNullException.ThrowIfNull(member.Key);
                ArgumentNullException.ThrowIfNull(member.Value);
                ValidateText(member.Key, nameof(members));
            }
            if (materialized.Select(x => x.Key).Distinct(StringComparer.Ordinal).Count() != materialized.Length)
            {
                throw new ArgumentException("Canonical object member names must be unique.", nameof(members));
            }
            Members = System.Array.AsReadOnly(materialized);
        }
        public IReadOnlyList<KeyValuePair<string, CanonicalValue>> Members { get; }
    }

    public sealed record Array : CanonicalValue
    {
        public Array(IEnumerable<CanonicalValue> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            var materialized = items.ToArray();
            if (materialized.Any(item => item is null))
            {
                throw new ArgumentException("Canonical arrays cannot contain null references.", nameof(items));
            }
            Items = System.Array.AsReadOnly(materialized);
        }
        public IReadOnlyList<CanonicalValue> Items { get; }
    }

    public sealed record StringValue : CanonicalValue
    {
        public StringValue(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            ValidateText(value, nameof(value));
            Value = value;
        }
        public string Value { get; }
    }

    public sealed record BooleanValue(bool Value) : CanonicalValue;
    public sealed record IntegerValue(long Value) : CanonicalValue;

    public sealed record Null : CanonicalValue
    {
        public static Null Value { get; } = new();
        private Null() { }
    }

    private static void ValidateText(string value, string parameterName)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsSurrogate(value[index]))
            {
                if (!char.IsHighSurrogate(value[index]) ||
                    index + 1 >= value.Length ||
                    !char.IsLowSurrogate(value[index + 1]))
                {
                    throw new ArgumentException("Canonical text must contain valid Unicode scalar values.", parameterName);
                }
                index++;
            }
        }
    }
}