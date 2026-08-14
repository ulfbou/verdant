using Verdant.Replay;
using Xunit;

namespace Verdant.ConformanceFixtures;

public sealed class ReplayAuthorityTests
{
    private static readonly IReadOnlyList<TestCommand> CanonicalLog =
    [
        new TestCommand(2),
        new TestCommand(3),
        new TestCommand(5)
    ];

    [Fact]
    public void ReplayZeroReconstructsCanonicalInitialState()
    {
        var engine = CreateEngine();

        var result = AssertSuccess(
            engine.Replay(
                new ReplayRequest<int, TestCommand>(
                    10,
                    CanonicalLog,
                    0)));

        Assert.Equal(10, result.State.Value);
        Assert.Empty(result.Events);
        Assert.Empty(result.EventBatches);
        Assert.Equal(0, result.AppliedActionCount);
    }

    [Fact]
    public void ReplayFullPrefixAppliesEveryRequestedCommand()
    {
        var engine = CreateEngine();

        var result = AssertSuccess(
            engine.Replay(
                new ReplayRequest<int, TestCommand>(
                    10,
                    CanonicalLog,
                    CanonicalLog.Count)));

        Assert.Equal(20, result.State.Value);
        Assert.Equal(
            ["value:12", "value:15", "value:20"],
            result.Events.Select(item => item.Value));

        Assert.Equal(3, result.EventBatches.Count);
        Assert.Equal(["value:12"], result.EventBatches[0].Select(item => item.Value));
        Assert.Equal(["value:15"], result.EventBatches[1].Select(item => item.Value));
        Assert.Equal(["value:20"], result.EventBatches[2].Select(item => item.Value));
        Assert.Equal(3, result.AppliedActionCount);
    }

    [Fact]
    public void EveryPrefixReconstructsIndependently()
    {
        var adapter = new TestReplayAdapter();
        var engine = new ReplayEngine<
            int,
            TestState,
            TestCommand,
            TestEvent>(adapter);

        var expectedValues = new[] { 10, 12, 15, 20 };

        for (var actionCount = 0;
             actionCount <= CanonicalLog.Count;
             actionCount++)
        {
            var result = AssertSuccess(
                engine.Replay(
                    new ReplayRequest<int, TestCommand>(
                        10,
                        CanonicalLog,
                        actionCount)));

            Assert.Equal(expectedValues[actionCount], result.State.Value);
            Assert.Equal(actionCount, result.AppliedActionCount);
        }

        Assert.Equal(4, adapter.InitializationCount);
    }

    [Fact]
    public void RepeatedReplayProducesIdenticalStateAndEvents()
    {
        var engine = CreateEngine();
        var request = new ReplayRequest<int, TestCommand>(
            10,
            CanonicalLog,
            CanonicalLog.Count);

        var first = AssertSuccess(engine.Replay(request));
        var second = AssertSuccess(engine.Replay(request));
        var third = AssertSuccess(engine.Replay(request));

        Assert.Equal(first.State, second.State);
        Assert.Equal(second.State, third.State);

        Assert.Equal(first.Events, second.Events);
        Assert.Equal(second.Events, third.Events);

        Assert.Equal(
            first.EventBatches.SelectMany(batch => batch),
            second.EventBatches.SelectMany(batch => batch));

        Assert.Equal(
            second.EventBatches.SelectMany(batch => batch),
            third.EventBatches.SelectMany(batch => batch));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(999)]
    public void InvalidPrefixCountReturnsTypedFailureWithoutClamping(
        int actionCount)
    {
        var adapter = new TestReplayAdapter();
        var engine = new ReplayEngine<
            int,
            TestState,
            TestCommand,
            TestEvent>(adapter);

        var result = engine.Replay(
            new ReplayRequest<int, TestCommand>(
                10,
                CanonicalLog,
                actionCount));

        var failure =
            Assert.IsType<ReplayResult<TestState, TestEvent>.Failure>(
                result);

        Assert.Equal(
            ReplayErrorCode.InvalidReplayActionCount,
            failure.Error.Code);

        Assert.Null(failure.Error.ActionIndex);
        Assert.Null(failure.Error.AdapterFailureCode);
        Assert.Equal(0, adapter.InitializationCount);
        Assert.Equal(0, adapter.ExecutionCount);
    }

    [Fact]
    public void RejectedAuthoritativeCommandReturnsDeterministicFailure()
    {
        IReadOnlyList<TestCommand> log =
        [
            new TestCommand(2),
            new TestCommand(0),
            new TestCommand(5)
        ];

        var adapter = new TestReplayAdapter();
        var engine = new ReplayEngine<
            int,
            TestState,
            TestCommand,
            TestEvent>(adapter);

        var result = engine.Replay(
            new ReplayRequest<int, TestCommand>(
                10,
                log,
                log.Count));

        var failure =
            Assert.IsType<ReplayResult<TestState, TestEvent>.Failure>(
                result);

        Assert.Equal(
            ReplayErrorCode.AuthoritativeCommandRejected,
            failure.Error.Code);

        Assert.Equal(1, failure.Error.ActionIndex);
        Assert.Equal(
            TestReplayAdapter.RejectedCommandCode,
            failure.Error.AdapterFailureCode);

        Assert.Equal(1, adapter.InitializationCount);
        Assert.Equal(2, adapter.ExecutionCount);
    }


    [Fact]
    public void ReplayRequestCopiesCallerOwnedMutableHistoryIntoAuthority()
    {
        var mutableLog = new List<TestCommand>
        {
            new(2),
            new(3)
        };
        var request = new ReplayRequest<int, TestCommand>(
            10,
            mutableLog,
            mutableLog.Count);

        mutableLog[0] = new TestCommand(99);
        mutableLog.Add(new TestCommand(5));

        Assert.Equal(2, request.ActionLog.Count);
        Assert.Equal([2, 3], request.ActionLog.Select(item => item.Delta));
        Assert.Equal(2, request.ActionCount);
    }

    [Fact]
    public void ReplayDoesNotModifySuppliedActionLog()
    {
        var mutableLog = new List<TestCommand>
        {
            new(2),
            new(3),
            new(5)
        };

        var original = mutableLog.ToArray();
        var engine = CreateEngine();

        _ = AssertSuccess(
            engine.Replay(
                new ReplayRequest<int, TestCommand>(
                    10,
                    mutableLog,
                    mutableLog.Count)));

        Assert.Equal(original, mutableLog);
    }

    [Fact]
    public void ReplayOnlyAppliesRequestedPrefix()
    {
        var adapter = new TestReplayAdapter();
        var engine = new ReplayEngine<
            int,
            TestState,
            TestCommand,
            TestEvent>(adapter);

        var result = AssertSuccess(
            engine.Replay(
                new ReplayRequest<int, TestCommand>(
                    10,
                    CanonicalLog,
                    2)));

        Assert.Equal(15, result.State.Value);
        Assert.Equal(2, adapter.ExecutionCount);
        Assert.Equal(
            ["value:12", "value:15"],
            result.Events.Select(item => item.Value));
    }

    [Fact]
    public void EventOrderMatchesCommandOrder()
    {
        var engine = CreateEngine();

        var result = AssertSuccess(
            engine.Replay(
                new ReplayRequest<int, TestCommand>(
                    10,
                    CanonicalLog,
                    CanonicalLog.Count)));

        Assert.Equal(
            ["value:12", "value:15", "value:20"],
            result.Events.Select(item => item.Value));
    }

    private static ReplayEngine<
        int,
        TestState,
        TestCommand,
        TestEvent> CreateEngine() =>
        new(new TestReplayAdapter());

    private static ReplayResult<TestState, TestEvent>.Success AssertSuccess(
        ReplayResult<TestState, TestEvent> result) =>
        Assert.IsType<ReplayResult<TestState, TestEvent>.Success>(
            result);

    private sealed record TestState(int Value);

    private sealed record TestCommand(int Delta);

    private sealed record TestEvent(string Value);

    private sealed class TestReplayAdapter :
        IReplayAdapter<int, TestState, TestCommand, TestEvent>
    {
        public const string RejectedCommandCode =
            "TEST_COMMAND_REJECTED";

        public int InitializationCount { get; private set; }

        public int ExecutionCount { get; private set; }

        public TestState CreateInitialState(int initialization)
        {
            InitializationCount++;
            return new TestState(initialization);
        }

        public ReplayStepResult<TestState, TestEvent> Execute(
            TestState state,
            TestCommand command)
        {
            ExecutionCount++;

            if (command.Delta == 0)
            {
                return new ReplayStepResult<
                    TestState,
                    TestEvent>.Rejected(
                        RejectedCommandCode);
            }

            var next = new TestState(state.Value + command.Delta);

            return new ReplayStepResult<
                TestState,
                TestEvent>.Accepted(
                    next,
                    [new TestEvent($"value:{next.Value}")]);
        }
    }
}