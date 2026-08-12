namespace Verdant.Replay;

public sealed record ReplayRequest<TInitialization, TCommand>(
    TInitialization Initialization,
    IReadOnlyList<TCommand> ActionLog,
    int ActionCount);
