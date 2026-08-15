using Verdant.Replay;

namespace Verdant.History;

public sealed class HistoricalQueryService<
    TInitialization,
    TState,
    TCommand,
    TEvent>
{
    private readonly ReplayEngine<
        TInitialization,
        TState,
        TCommand,
        TEvent> _replay;

    public HistoricalQueryService(
        IReplayAdapter<TInitialization, TState, TCommand, TEvent> adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        _replay = new ReplayEngine<
            TInitialization,
            TState,
            TCommand,
            TEvent>(adapter);
    }

    public HistoricalQueryResult<TState, TEvent> Query(
        TInitialization initialization,
        IReadOnlyList<TCommand> actionLog,
        int actionCount)
    {
        ArgumentNullException.ThrowIfNull(initialization);
        ArgumentNullException.ThrowIfNull(actionLog);

        var replayResult = _replay.Replay(
            new ReplayRequest<TInitialization, TCommand>(
                initialization,
                actionLog,
                actionCount));

        return replayResult switch
        {
            ReplayResult<TState, TEvent>.Success success =>
                new HistoricalQueryResult<TState, TEvent>.Success(
                    success.State,
                    success.Events,
                    success.EventBatches,
                    actionCount,
                    success.AppliedActionCount),
            ReplayResult<TState, TEvent>.Failure failure =>
                new HistoricalQueryResult<TState, TEvent>.Failure(
                    failure.Error),
            _ => throw new InvalidOperationException(
                "Replay returned an unsupported result type.")
        };
    }
}