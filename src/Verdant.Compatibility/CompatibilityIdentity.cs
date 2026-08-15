namespace Verdant.Compatibility;

public sealed record CompatibilityIdentity
{
    public CompatibilityIdentity(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            throw new ArgumentException(
                "A compatibility identity is required.",
                nameof(value));
        }

        if (!IsSupportedSyntax(value))
        {
            throw new ArgumentException(
                "A compatibility identity must contain only ASCII letters, digits, '.', '_', '+', or '-', and must start with a letter or digit.",
                nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;

    private static bool IsSupportedSyntax(string value)
    {
        if (!IsAsciiLetterOrDigit(value[0]))
        {
            return false;
        }

        for (var index = 1; index < value.Length; index++)
        {
            var character = value[index];
            if (!IsAsciiLetterOrDigit(character) &&
                character is not '.' and not '_' and not '+' and not '-')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiLetterOrDigit(char character) =>
        character is >= 'A' and <= 'Z' or
        >= 'a' and <= 'z' or
        >= '0' and <= '9';
}