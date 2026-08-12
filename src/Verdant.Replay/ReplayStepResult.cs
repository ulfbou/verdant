namespace Verdant.Replay;

public abstract record ReplayStepResult<TState, TEvent>
{
    private ReplayStepResult()
    {
    }

    public sealed record Accepted(
        TState State,
        IReadOnlyList<TEvent> Events)
        : ReplayStepResult<TState, TEvent>;

    public sealed record Rejected(
        string FailureCode)
        : ReplayStepResult<TState, TEvent>;
}
