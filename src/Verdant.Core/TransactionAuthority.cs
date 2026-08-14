namespace Verdant.Core;

public sealed record TransactionAuthority<TState, TCommand, TDeterministicState>
{
    public TransactionAuthority(
        TState state,
        ActionLog<TCommand> actionLog,
        TDeterministicState deterministicState)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(actionLog);
        ArgumentNullException.ThrowIfNull(deterministicState);

        State = state;
        ActionLog = actionLog;
        DeterministicState = deterministicState;
    }

    public TState State { get; }

    public ActionLog<TCommand> ActionLog { get; }

    public TDeterministicState DeterministicState { get; }

    public int ActionCount => ActionLog.Count;
}