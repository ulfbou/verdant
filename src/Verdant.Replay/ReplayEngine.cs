namespace Verdant.Replay;

public sealed class ReplayEngine<TInitialization, TState, TCommand, TEvent>
{
    private readonly IReplayAdapter<TInitialization, TState, TCommand, TEvent> _adapter;

    public ReplayEngine(
        IReplayAdapter<TInitialization, TState, TCommand, TEvent> adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        _adapter = adapter;
    }

    public ReplayResult<TState, TEvent> Replay(
        ReplayRequest<TInitialization, TCommand> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ActionLog);

        if (request.ActionCount < 0 ||
            request.ActionCount > request.ActionLog.Count)
        {
            return new ReplayResult<TState, TEvent>.Failure(
                new ReplayError(
                    ReplayErrorCode.InvalidReplayActionCount));
        }

        var state = _adapter.CreateInitialState(request.Initialization);
        var events = new List<TEvent>();
        var eventBatches = new List<IReadOnlyList<TEvent>>(
            request.ActionCount);

        for (var actionIndex = 0;
             actionIndex < request.ActionCount;
             actionIndex++)
        {
            var command = request.ActionLog[actionIndex];
            var step = _adapter.Execute(state, command);

            if (step is ReplayStepResult<TState, TEvent>.Rejected rejected)
            {
                return new ReplayResult<TState, TEvent>.Failure(
                    new ReplayError(
                        ReplayErrorCode.AuthoritativeCommandRejected,
                        actionIndex,
                        rejected.FailureCode));
            }

            var accepted =
                (ReplayStepResult<TState, TEvent>.Accepted)step;

            state = accepted.State;

            var batch = accepted.Events.ToArray();
            eventBatches.Add(batch);
            events.AddRange(batch);
        }

        return new ReplayResult<TState, TEvent>.Success(
            state,
            events.ToArray(),
            eventBatches.ToArray(),
            request.ActionCount);
    }
}
