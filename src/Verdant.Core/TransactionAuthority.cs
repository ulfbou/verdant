namespace Verdant.Core;

public sealed record TransactionAuthority<TState, TCommand, TDeterministicState>
{
    public TransactionAuthority(
        TState state,
        IReadOnlyList<TCommand> actionLog,
        TDeterministicState deterministicState,
        int actionCount)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(actionLog);
        ArgumentNullException.ThrowIfNull(deterministicState);

        if (actionCount < 0 || actionCount != actionLog.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionCount),
                "Action count must equal the successful ActionLog count.");
        }

        State = state;
        ActionLog = actionLog.ToArray();
        DeterministicState = deterministicState;
        ActionCount = actionCount;
    }

    public TState State { get; }

    public IReadOnlyList<TCommand> ActionLog { get; }

    public TDeterministicState DeterministicState { get; }

    public int ActionCount { get; }
}