using Verdant.Core;

namespace Verdant.Replay;

public sealed record ReplayRequest<TInitialization, TCommand>
{
    public ReplayRequest(
        TInitialization initialization,
        ActionLog<TCommand> actionLog,
        int actionCount)
    {
        ArgumentNullException.ThrowIfNull(initialization);
        ArgumentNullException.ThrowIfNull(actionLog);

        Initialization = initialization;
        ActionLog = actionLog;
        ActionCount = actionCount;
    }

    public ReplayRequest(
        TInitialization initialization,
        IReadOnlyList<TCommand> actionLog,
        int actionCount)
        : this(
            initialization,
            new ActionLog<TCommand>(actionLog),
            actionCount)
    {
    }

    public TInitialization Initialization { get; }

    public ActionLog<TCommand> ActionLog { get; }

    public int ActionCount { get; }
}