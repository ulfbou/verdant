using Verdant.Replay;

namespace Verdant.History;

public abstract record HistoricalQueryResult<TState, TEvent>
{
    private HistoricalQueryResult()
    {
    }

    public sealed record Success(
        TState State,
        IReadOnlyList<TEvent> Events,
        IReadOnlyList<IReadOnlyList<TEvent>> EventBatches,
        int RequestedActionCount,
        int AppliedActionCount)
        : HistoricalQueryResult<TState, TEvent>;

    public sealed record Failure(ReplayError Error)
        : HistoricalQueryResult<TState, TEvent>;
}