namespace Verdant.Replay;

public abstract record ReplayResult<TState, TEvent>
{
    private ReplayResult()
    {
    }

    public sealed record Success(
        TState State,
        IReadOnlyList<TEvent> Events,
        IReadOnlyList<IReadOnlyList<TEvent>> EventBatches,
        int AppliedActionCount)
        : ReplayResult<TState, TEvent>;

    public sealed record Failure(
        ReplayError Error)
        : ReplayResult<TState, TEvent>;
}
