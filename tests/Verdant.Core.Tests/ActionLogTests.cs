using Verdant.Core;
using Xunit;

namespace Verdant.Core.Tests;

public sealed class ActionLogTests
{
    private static ActionLog<TestCommand> EmptyLog() =>
        new(Array.Empty<TestCommand>());

    [Fact]
    public void EmptyLogHasCanonicalZeroCount()
    {
        var log = EmptyLog();

        Assert.Empty(log);
    }

    [Fact]
    public void ConstructionCopiesCallerOwnedMutableInput()
    {
        var first = new TestCommand(1);
        var source = new List<TestCommand> { first };
        var log = new ActionLog<TestCommand>(source);

        source[0] = new TestCommand(99);
        source.Add(new TestCommand(2));

        Assert.Single(log);
        Assert.Equal(first, log[0]);
    }

    [Fact]
    public void AppendReturnsNewOrderedAuthorityWithoutMutatingPreviousLog()
    {
        var first = new TestCommand(1);
        var second = new TestCommand(2);
        var empty = EmptyLog();
        var one = empty.Append(first);
        var two = one.Append(second);

        Assert.Empty(empty);
        Assert.Equal([first], one);
        Assert.Equal([first, second], two);
    }

    [Fact]
    public void ReturnedAuthorityCannotBeCastToMutableCommandCollection()
    {
        IReadOnlyList<TestCommand> log =
            new ActionLog<TestCommand>([new TestCommand(1)]);

        Assert.False(log is TestCommand[]);
        Assert.False(log is IList<TestCommand>);
        Assert.False(log is ICollection<TestCommand>);
    }

    private sealed record TestCommand(int Value);
}