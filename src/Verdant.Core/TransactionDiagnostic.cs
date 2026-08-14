namespace Verdant.Core;

public sealed record TransactionDiagnostic(string Code, string? Detail = null)
{
    public string Code { get; } =
        string.IsNullOrWhiteSpace(Code)
            ? throw new ArgumentException(
                "A stable diagnostic code is required.",
                nameof(Code))
            : Code;
}