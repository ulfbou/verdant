namespace Verdant.Core;

public sealed class ActionLog<TCommand> : IReadOnlyList<TCommand>
{
    private readonly TCommand[] _commands;

    public ActionLog(IEnumerable<TCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        _commands = commands.ToArray();
    }

    private ActionLog(TCommand[] commands)
    {
        _commands = commands;
    }

    public int Count => _commands.Length;

    public TCommand this[int index] => _commands[index];

    public ActionLog<TCommand> Append(TCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var appended = new TCommand[checked(_commands.Length + 1)];
        Array.Copy(_commands, appended, _commands.Length);
        appended[^1] = command;
        return new ActionLog<TCommand>(appended);
    }

    public IEnumerator<TCommand> GetEnumerator() =>
        ((IEnumerable<TCommand>)_commands).GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
        _commands.GetEnumerator();
}