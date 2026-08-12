namespace Verdant.Replay;

public interface IReplayAdapter<TInitialization, TState, TCommand, TEvent>
{
    TState CreateInitialState(TInitialization initialization);

    ReplayStepResult<TState, TEvent> Execute(
        TState state,
        TCommand command);
}
