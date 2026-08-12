namespace Verdant.Replay;

public sealed record ReplayError(
    ReplayErrorCode Code,
    int? ActionIndex = null,
    string? AdapterFailureCode = null);
