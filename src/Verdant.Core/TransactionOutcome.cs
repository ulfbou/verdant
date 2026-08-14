namespace Verdant.Core;

public abstract record TransactionOutcome<
    TState,
    TCommand,
    TEvent,
    TDeterministicState>
{
    private TransactionOutcome()
    {
    }

    public sealed record Succeeded(
        TransactionAuthority<TState, TCommand, TDeterministicState> Authority,
        IReadOnlyList<TEvent> Events)
        : TransactionOutcome<TState, TCommand, TEvent, TDeterministicState>;

    public sealed record Failed(
        TransactionAuthority<TState, TCommand, TDeterministicState> Authority,
        TransactionDiagnostic Diagnostic)
        : TransactionOutcome<TState, TCommand, TEvent, TDeterministicState>;
}