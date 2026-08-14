namespace Verdant.Core;

public sealed class TransactionPipeline<
    TState,
    TCommand,
    TEvent,
    TDeterministicState>
{
    private readonly ITransactionAdapter<
        TState,
        TCommand,
        TEvent,
        TDeterministicState> _adapter;

    private readonly object _transactionLock = new();

    public TransactionPipeline(
        ITransactionAdapter<
            TState,
            TCommand,
            TEvent,
            TDeterministicState> adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        _adapter = adapter;
    }

    public TransactionOutcome<
        TState,
        TCommand,
        TEvent,
        TDeterministicState> Execute(
            TransactionAuthority<TState, TCommand, TDeterministicState> authority,
            TCommand command)
    {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(command);

        lock (_transactionLock)
        {
            var candidateState = _adapter.SnapshotState(authority.State);
            var candidateDeterministicState =
                _adapter.SnapshotDeterministicState(
                    authority.DeterministicState);

            var candidate = _adapter.Execute(
                candidateState,
                candidateDeterministicState,
                authority.ActionCount,
                command);

            ArgumentNullException.ThrowIfNull(candidate);

            if (candidate is TransactionCandidate<
                TState,
                TEvent,
                TDeterministicState>.Rejected rejected)
            {
                ArgumentNullException.ThrowIfNull(rejected.Diagnostic);

                return new TransactionOutcome<
                    TState,
                    TCommand,
                    TEvent,
                    TDeterministicState>.Failed(
                        authority,
                        rejected.Diagnostic);
            }

            var accepted = (TransactionCandidate<
                TState,
                TEvent,
                TDeterministicState>.Accepted)candidate;

            ArgumentNullException.ThrowIfNull(accepted.State);
            ArgumentNullException.ThrowIfNull(accepted.DeterministicState);
            ArgumentNullException.ThrowIfNull(accepted.Events);

            var committedState = _adapter.SnapshotState(accepted.State);
            var committedDeterministicState =
                _adapter.SnapshotDeterministicState(
                    accepted.DeterministicState);
            var committedEvents = accepted.Events.ToArray();
            var committedLog = new TCommand[authority.ActionLog.Count + 1];

            for (var index = 0; index < authority.ActionLog.Count; index++)
            {
                committedLog[index] = authority.ActionLog[index];
            }

            committedLog[^1] = command;

            var committedAuthority = new TransactionAuthority<
                TState,
                TCommand,
                TDeterministicState>(
                    committedState,
                    committedLog,
                    committedDeterministicState,
                    checked(authority.ActionCount + 1));

            return new TransactionOutcome<
                TState,
                TCommand,
                TEvent,
                TDeterministicState>.Succeeded(
                    committedAuthority,
                    committedEvents);
        }
    }
}