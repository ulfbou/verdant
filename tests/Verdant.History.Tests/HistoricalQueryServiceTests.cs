using System.Reflection;
using Verdant.History;
using Verdant.Replay;
using Xunit;

namespace Verdant.History.Tests;

public sealed class HistoricalQueryServiceTests
{
    private static readonly IReadOnlyList<TestCommand> CanonicalLog =
    [
        new TestCommand(2, 2),
        new TestCommand(3, 1),
        new TestCommand(5, 2)
    ];

    [Fact]
    public void ZeroQueryReconstructsCanonicalInitialization()
    {
        var adapter = new TestReplayAdapter();
        var service = CreateService(adapter);

        var result = AssertSuccess(service.Query(10, CanonicalLog, 0));

        Assert.Equal(10, result.State.Value);
        Assert.Equal(["initial"], result.State.Trace);
        Assert.Empty(result.Events);
        Assert.Empty(result.EventBatches);
        Assert.Equal(0, result.RequestedActionCount);
        Assert.Equal(0, result.AppliedActionCount);
        Assert.Equal(1, adapter.InitializationCount);
        Assert.Equal(0, adapter.ExecutionCount);
    }

    [Fact]
    public void EveryValidPrefixReturnsExactStateEventsBatchesAndCount()
    {
        var adapter = new TestReplayAdapter();
        var service = CreateService(adapter);
        var expectedValues = new[] { 10, 12, 15, 20 };
        var expectedEventCounts = new[] { 0, 2, 3, 5 };

        for (var actionCount = 0;
             actionCount <= CanonicalLog.Count;
             actionCount++)
        {
            var result = AssertSuccess(
                service.Query(10, CanonicalLog, actionCount));

            Assert.Equal(expectedValues[actionCount], result.State.Value);
            Assert.Equal(actionCount, result.RequestedActionCount);
            Assert.Equal(actionCount, result.AppliedActionCount);
            Assert.Equal(actionCount, result.EventBatches.Count);
            Assert.Equal(expectedEventCounts[actionCount], result.Events.Count);

            var expectedTrace = new List<string> { "initial" };
            expectedTrace.AddRange(
                CanonicalLog
                    .Take(actionCount)
                    .Select(command => $"command:{command.Delta}"));
            Assert.Equal(expectedTrace, result.State.Trace);

            Assert.Equal(
                Enumerable.Range(0, actionCount)
                    .SelectMany(index => ExpectedEvents(index, CanonicalLog[index])),
                result.Events);

            for (var index = 0; index < actionCount; index++)
            {
                Assert.Equal(
                    ExpectedEvents(index, CanonicalLog[index]),
                    result.EventBatches[index]);
            }
        }

        Assert.Equal(CanonicalLog.Count + 1, adapter.InitializationCount);
    }

    [Fact]
    public void RepeatedFullQueryIsStructurallyIdenticalAndFresh()
    {
        var adapter = new TestReplayAdapter();
        var service = CreateService(adapter);

        var first = AssertSuccess(
            service.Query(10, CanonicalLog, CanonicalLog.Count));
        var second = AssertSuccess(
            service.Query(10, CanonicalLog, CanonicalLog.Count));
        var third = AssertSuccess(
            service.Query(10, CanonicalLog, CanonicalLog.Count));

        AssertHistoryEqual(first, second);
        AssertHistoryEqual(second, third);
        Assert.NotSame(first.State, second.State);
        Assert.NotSame(second.State, third.State);
        Assert.NotSame(first.Events, second.Events);
        Assert.NotSame(first.EventBatches, second.EventBatches);
        Assert.Equal(3, adapter.InitializationCount);
        Assert.Equal(9, adapter.ExecutionCount);
    }

    [Fact]
    public void HistoricalNavigationIsPathIndependent()
    {
        var service = CreateService(new TestReplayAdapter());

        var fullFirst = AssertSuccess(service.Query(10, CanonicalLog, 3));
        var earlier = AssertSuccess(service.Query(10, CanonicalLog, 1));
        var middle = AssertSuccess(service.Query(10, CanonicalLog, 2));
        var fullAgain = AssertSuccess(service.Query(10, CanonicalLog, 3));

        AssertHistoryEqual(
            AssertSuccess(CreateService(new TestReplayAdapter()).Query(10, CanonicalLog, 3)),
            fullFirst);
        AssertHistoryEqual(
            AssertSuccess(CreateService(new TestReplayAdapter()).Query(10, CanonicalLog, 1)),
            earlier);
        AssertHistoryEqual(
            AssertSuccess(CreateService(new TestReplayAdapter()).Query(10, CanonicalLog, 2)),
            middle);
        AssertHistoryEqual(fullFirst, fullAgain);
    }

    [Fact]
    public void LaterQueriesDoNotMutatePreviouslyReturnedHistory()
    {
        var service = CreateService(new TestReplayAdapter());
        var earlier = AssertSuccess(service.Query(10, CanonicalLog, 1));
        var stateSnapshot = CaptureState(earlier.State);
        var eventSnapshot = earlier.Events.ToArray();
        var batchSnapshot = earlier.EventBatches
            .Select(batch => batch.ToArray())
            .ToArray();

        _ = AssertSuccess(service.Query(10, CanonicalLog, 3));
        _ = AssertSuccess(service.Query(10, CanonicalLog, 2));

        Assert.Equal(stateSnapshot, CaptureState(earlier.State));
        Assert.Equal(eventSnapshot, earlier.Events);
        Assert.Equal(batchSnapshot.Length, earlier.EventBatches.Count);
        for (var index = 0; index < batchSnapshot.Length; index++)
        {
            Assert.Equal(batchSnapshot[index], earlier.EventBatches[index]);
        }
    }

    [Fact]
    public void MutableCallerOwnedHistoryCannotAlterEstablishedQueryInput()
    {
        var mutableLog = CanonicalLog.ToList();
        var service = CreateService(new TestReplayAdapter());

        var first = AssertSuccess(service.Query(10, mutableLog, 2));
        mutableLog[0] = new TestCommand(500, 8);
        mutableLog.Clear();
        mutableLog.Add(new TestCommand(900, 8));

        Assert.Equal(15, first.State.Value);
        Assert.Equal(["initial", "command:2", "command:3"], first.State.Trace);
        Assert.Equal([2, 3], CanonicalLog.Take(2).Select(item => item.Delta));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(1000000)]
    public void InvalidPrefixPreservesExactReplayFailureBeforeExecution(
        int actionCount)
    {
        var adapter = new TestReplayAdapter();
        var service = CreateService(adapter);

        var failure = AssertFailure(
            service.Query(10, CanonicalLog, actionCount));

        Assert.Equal(ReplayErrorCode.InvalidReplayActionCount, failure.Error.Code);
        Assert.Null(failure.Error.ActionIndex);
        Assert.Null(failure.Error.AdapterFailureCode);
        Assert.Equal(0, adapter.InitializationCount);
        Assert.Equal(0, adapter.ExecutionCount);
    }

    [Fact]
    public void AuthoritativeCommandRejectionPreservesExactReplayFailure()
    {
        IReadOnlyList<TestCommand> log =
        [
            new TestCommand(2, 1),
            new TestCommand(0, 1),
            new TestCommand(5, 1)
        ];
        var adapter = new TestReplayAdapter();
        var service = CreateService(adapter);

        var failure = AssertFailure(service.Query(10, log, log.Count));

        Assert.Equal(
            ReplayErrorCode.AuthoritativeCommandRejected,
            failure.Error.Code);
        Assert.Equal(1, failure.Error.ActionIndex);
        Assert.Equal(TestReplayAdapter.RejectedCode, failure.Error.AdapterFailureCode);
        Assert.Equal(1, adapter.InitializationCount);
        Assert.Equal(2, adapter.ExecutionCount);
    }

    [Fact]
    public void QueriesDoNotMutateLiveAuthorityOrAppendOrTruncateItsActionLog()
    {
        var liveState = new MutableLiveState(999);
        var liveLog = CanonicalLog.ToList();
        var before = liveLog.ToArray();
        var service = CreateService(new TestReplayAdapter());

        _ = AssertSuccess(service.Query(10, liveLog, 1));
        _ = AssertSuccess(service.Query(10, liveLog, liveLog.Count));

        Assert.Equal(999, liveState.Value);
        Assert.Equal(before, liveLog);
        Assert.Equal(3, liveLog.Count);
    }

    [Fact]
    public void HistoricalServiceInvokesReplayAndHasNoHostDependencies()
    {
        var type = typeof(HistoricalQueryService<,,,>);
        var fields = type.GetFields(
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.Single(fields);
        Assert.Equal(typeof(ReplayEngine<,,,>), fields[0].FieldType.GetGenericTypeDefinition());

        var referencedAssemblies = type.Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToArray();

        Assert.DoesNotContain(referencedAssemblies, name =>
            name!.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referencedAssemblies, name =>
            name!.Contains("JSInterop", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referencedAssemblies, name =>
            name!.Contains("Mold", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void QueryInvokesOnlyReplayInitializationAndExecution()
    {
        var adapter = new TestReplayAdapter();
        var service = CreateService(adapter);

        _ = AssertSuccess(service.Query(10, CanonicalLog, 2));

        Assert.Equal(1, adapter.InitializationCount);
        Assert.Equal(2, adapter.ExecutionCount);
        Assert.Equal(0, adapter.StorageCalls);
        Assert.Equal(0, adapter.UiCalls);
        Assert.Equal(0, adapter.PresentationCalls);
        Assert.Equal(0, adapter.AnalyticsCalls);
        Assert.Equal(0, adapter.ClockReads);
        Assert.Equal(0, adapter.SessionMutations);
        Assert.Equal(0, adapter.HostRandomCalls);
        Assert.Equal(0, adapter.PreviewCalls);
    }

    private static HistoricalQueryService<int, TestState, TestCommand, TestEvent>
        CreateService(TestReplayAdapter adapter) =>
        new(adapter);

    private static HistoricalQueryResult<TestState, TestEvent>.Success
        AssertSuccess(HistoricalQueryResult<TestState, TestEvent> result) =>
        Assert.IsType<HistoricalQueryResult<TestState, TestEvent>.Success>(result);

    private static HistoricalQueryResult<TestState, TestEvent>.Failure
        AssertFailure(HistoricalQueryResult<TestState, TestEvent> result) =>
        Assert.IsType<HistoricalQueryResult<TestState, TestEvent>.Failure>(result);

    private static TestEvent[] ExpectedEvents(
        int actionIndex,
        TestCommand command) =>
        Enumerable.Range(0, command.EventCount)
            .Select(ordinal => new TestEvent(
                actionIndex,
                command.Delta,
                ordinal,
                $"event:{actionIndex}:{ordinal}"))
            .ToArray();

    private static void AssertHistoryEqual(
        HistoricalQueryResult<TestState, TestEvent>.Success expected,
        HistoricalQueryResult<TestState, TestEvent>.Success actual)
    {
        Assert.Equal(expected.State.Value, actual.State.Value);
        Assert.Equal(expected.State.Trace, actual.State.Trace);
        Assert.Equal(expected.Events, actual.Events);
        Assert.Equal(expected.EventBatches.Count, actual.EventBatches.Count);
        for (var index = 0; index < expected.EventBatches.Count; index++)
        {
            Assert.Equal(expected.EventBatches[index], actual.EventBatches[index]);
        }
        Assert.Equal(expected.RequestedActionCount, actual.RequestedActionCount);
        Assert.Equal(expected.AppliedActionCount, actual.AppliedActionCount);
    }

    private static StateSnapshot CaptureState(TestState state) =>
        new(state.Value, state.Trace.ToArray());

    private sealed record StateSnapshot(
        int Value,
        IReadOnlyList<string> Trace)
    {
        public bool Equals(StateSnapshot? other) =>
            other is not null &&
            Value == other.Value &&
            Trace.SequenceEqual(other.Trace);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Value);

            foreach (var item in Trace)
            {
                hash.Add(item);
            }

            return hash.ToHashCode();
        }
    }

    private sealed record TestState(
        int Value,
        IReadOnlyList<string> Trace);

    private sealed record TestCommand(int Delta, int EventCount);

    private sealed record TestEvent(
        int ActionIndex,
        int Delta,
        int Ordinal,
        string Code);

    private sealed class MutableLiveState(int value)
    {
        public int Value { get; set; } = value;
    }

    private sealed class TestReplayAdapter :
        IReplayAdapter<int, TestState, TestCommand, TestEvent>
    {
        public const string RejectedCode = "TEST_COMMAND_REJECTED";

        public int InitializationCount { get; private set; }
        public int ExecutionCount { get; private set; }
        public int StorageCalls { get; private set; }
        public int UiCalls { get; private set; }
        public int PresentationCalls { get; private set; }
        public int AnalyticsCalls { get; private set; }
        public int ClockReads { get; private set; }
        public int SessionMutations { get; private set; }
        public int HostRandomCalls { get; private set; }
        public int PreviewCalls { get; private set; }

        public TestState CreateInitialState(int initialization)
        {
            InitializationCount++;
            return new TestState(initialization, ["initial"]);
        }

        public ReplayStepResult<TestState, TestEvent> Execute(
            TestState state,
            TestCommand command)
        {
            ExecutionCount++;

            if (command.Delta == 0)
            {
                return new ReplayStepResult<TestState, TestEvent>.Rejected(
                    RejectedCode);
            }

            var actionIndex = state.Trace.Count - 1;
            var next = new TestState(
                checked(state.Value + command.Delta),
                [.. state.Trace, $"command:{command.Delta}"]);
            var events = ExpectedEvents(actionIndex, command);

            return new ReplayStepResult<TestState, TestEvent>.Accepted(
                next,
                events);
        }
    }
}