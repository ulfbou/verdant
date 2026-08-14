namespace Verdant.Core;

public interface ITransactionAdapter<
    TState,
    TCommand,
    TEvent,
    TDeterministicState>
{
    TState SnapshotState(TState state);

    TDeterministicState SnapshotDeterministicState(
        TDeterministicState deterministicState);

    TransactionCandidate<TState, TEvent, TDeterministicState> Execute(
        TState candidateState,
        TDeterministicState candidateDeterministicState,
        int actionCount,
        TCommand command);
}