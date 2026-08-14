namespace Verdant.Core;

public abstract record TransactionCandidate<TState, TEvent, TDeterministicState>
{
    private TransactionCandidate()
    {
    }

    public sealed record Accepted(
        TState State,
        TDeterministicState DeterministicState,
        IReadOnlyList<TEvent> Events)
        : TransactionCandidate<TState, TEvent, TDeterministicState>;

    public sealed record Rejected(
        TransactionDiagnostic Diagnostic)
        : TransactionCandidate<TState, TEvent, TDeterministicState>;
}