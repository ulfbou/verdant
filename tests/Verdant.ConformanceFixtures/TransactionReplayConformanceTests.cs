using Verdant.Core;
using Verdant.Replay;
using Xunit;

namespace Verdant.ConformanceFixtures;

public sealed class TransactionReplayConformanceTests
{
    [Fact]
    public void SuccessfulTransactionsCommitAuthorityAndReplayEveryCanonicalPrefix()
    {
        var transactionAdapter = new ConformanceAdapter();
        var pipeline = CreatePipeline(transactionAdapter);
        var initial = CreateAuthority();

        var first = AssertSucceeded(
            pipeline.Execute(initial, new TestCommand(2, 2)));

        var rejected = AssertFailed(
            pipeline.Execute(first.Authority, new TestCommand(-1, 1)));

        var second = AssertSucceeded(
            pipeline.Execute(rejected.Authority, new TestCommand(3, 1)));

        var third = AssertSucceeded(
            pipeline.Execute(second.Authority, new TestCommand(5, 2)));

        Assert.Equal(20, third.Authority.State.Value);
        Assert.Equal(["initial", "command:2", "command:3", "command:5"],
            third.Authority.State.Trace);
        Assert.Equal([2, 3, 5],
            third.Authority.ActionLog.Select(command => command.Delta));
        Assert.Equal(3, third.Authority.ActionCount);
        Assert.Equal(5, third.Authority.DeterministicState.Cursor);

        Assert.Same(first.Authority, rejected.Authority);
        Assert.Equal(ConformanceAdapter.RejectedCode, rejected.Diagnostic.Code);

        var replayAdapter = new ConformanceAdapter();
        var replay = CreateReplay(replayAdapter);
        var expectedValues = new[] { 10, 12, 15, 20 };
        var expectedCursors = new[] { 0, 2, 3, 5 };

        for (var prefix = 0; prefix <= third.Authority.ActionLog.Count; prefix++)
        {
            var result = AssertReplaySucceeded(
                replay.Replay(
                    new ReplayRequest<TestInitialization, TestCommand>(
                        new TestInitialization(10),
                        third.Authority.ActionLog,
                        prefix)));

            Assert.Equal(expectedValues[prefix], result.State.Value);
            Assert.Equal(prefix, result.AppliedActionCount);
            Assert.Equal(prefix, result.EventBatches.Count);

            var expectedTrace = new List<string> { "initial" };
            expectedTrace.AddRange(
                third.Authority.ActionLog
                    .Take(prefix)
                    .Select(command => $"command:{command.Delta}"));

            Assert.Equal(expectedTrace, result.State.Trace);

            var replayedEventCount = third.Authority.ActionLog
                .Take(prefix)
                .Sum(command => command.EventCount);

            Assert.Equal(replayedEventCount, result.Events.Count);
            Assert.Equal(expectedCursors[prefix], replayAdapter.LastReplayCursor);
        }

        Assert.Equal(4, replayAdapter.InitializationCount);
    }

    [Fact]
    public void RejectionPreservesCompleteAuthorityAndProducesNoSemanticHistory()
    {
        var adapter = new ConformanceAdapter(mutateRejectedCandidate: true);
        var pipeline = CreatePipeline(adapter);
        var initial = CreateAuthority();

        var success = AssertSucceeded(
            pipeline.Execute(initial, new TestCommand(4, 2)));

        var authorityBeforeRejection = success.Authority;

        var failure = AssertFailed(
            pipeline.Execute(authorityBeforeRejection, new TestCommand(-9, 3)));

        Assert.Same(authorityBeforeRejection, failure.Authority);
        Assert.Equal(14, failure.Authority.State.Value);
        Assert.Equal(["initial", "command:4"], failure.Authority.State.Trace);
        Assert.Equal([4],
            failure.Authority.ActionLog.Select(command => command.Delta));
        Assert.Equal(1, failure.Authority.ActionCount);
        Assert.Equal(2, failure.Authority.DeterministicState.Cursor);

        Assert.Equal(ConformanceAdapter.RejectedCode, failure.Diagnostic.Code);
        Assert.Equal("delta:-9", failure.Diagnostic.Detail);

        Assert.Equal(0, adapter.PublishedSemanticEventCount);
        Assert.Equal(0, adapter.StorageReads);
        Assert.Equal(0, adapter.StorageWrites);
        Assert.Equal(0, adapter.UiCalls);
        Assert.Equal(0, adapter.PresentationCalls);
        Assert.Equal(0, adapter.AnalyticsCalls);
        Assert.Equal(0, adapter.ClockReads);
        Assert.Equal(0, adapter.SessionMutations);
        Assert.Equal(0, adapter.HostRandomCalls);
    }

    [Fact]
    public void SuccessfulOnlyActionLogAndSemanticEventsPreserveCanonicalOrder()
    {
        var pipeline = CreatePipeline(new ConformanceAdapter());
        var authority = CreateAuthority();

        var firstCommand = new TestCommand(2, 2);
        var secondCommand = new TestCommand(3, 1);
        var thirdCommand = new TestCommand(5, 2);

        var first = AssertSucceeded(pipeline.Execute(authority, firstCommand));
        authority = first.Authority;

        var rejected = AssertFailed(
            pipeline.Execute(authority, new TestCommand(-100, 5)));
        authority = rejected.Authority;

        var second = AssertSucceeded(pipeline.Execute(authority, secondCommand));
        authority = second.Authority;

        var third = AssertSucceeded(pipeline.Execute(authority, thirdCommand));
        authority = third.Authority;

        Assert.Equal(
            [firstCommand, secondCommand, thirdCommand],
            authority.ActionLog);

        Assert.Equal(
            ["evt:0:2:0", "evt:0:2:1"],
            first.Events.Select(item => item.Id));
        Assert.Equal(
            ["evt:1:3:0"],
            second.Events.Select(item => item.Id));
        Assert.Equal(
            ["evt:2:5:0", "evt:2:5:1"],
            third.Events.Select(item => item.Id));

        Assert.Equal(
            [(0, 2, 0), (0, 2, 1)],
            first.Events.Select(item =>
                (item.ActionIndex, item.CommandDelta, item.Ordinal)));
        Assert.Equal(
            [(1, 3, 0)],
            second.Events.Select(item =>
                (item.ActionIndex, item.CommandDelta, item.Ordinal)));
        Assert.Equal(
            [(2, 5, 0), (2, 5, 1)],
            third.Events.Select(item =>
                (item.ActionIndex, item.CommandDelta, item.Ordinal)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(1000000)]
    public void InvalidReplayPrefixFailsExplicitlyWithoutInitializationOrClamping(
        int actionCount)
    {
        var adapter = new ConformanceAdapter();
        var replay = CreateReplay(adapter);

        var log = new ActionLog<TestCommand>(
        [
            new TestCommand(2, 1),
            new TestCommand(3, 1),
            new TestCommand(5, 1)
        ]);

        var result = replay.Replay(
            new ReplayRequest<TestInitialization, TestCommand>(
                new TestInitialization(10),
                log,
                actionCount));

        var failure =
            Assert.IsType<ReplayResult<MutableState, TestEvent>.Failure>(result);

        Assert.Equal(
            ReplayErrorCode.InvalidReplayActionCount,
            failure.Error.Code);
        Assert.Null(failure.Error.ActionIndex);
        Assert.Null(failure.Error.AdapterFailureCode);

        Assert.Equal(0, adapter.InitializationCount);
        Assert.Equal(0, adapter.ReplayExecutionCount);
        Assert.Equal(0, adapter.StorageReads);
        Assert.Equal(0, adapter.UiCalls);
        Assert.Equal(0, adapter.HostRandomCalls);
    }

    [Fact]
    public void RepeatedReplayIsStructurallyEquivalentAndSideEffectFree()
    {
        var adapter = new ConformanceAdapter();
        var replay = CreateReplay(adapter);

        var log = new ActionLog<TestCommand>(
        [
            new TestCommand(2, 2),
            new TestCommand(3, 1),
            new TestCommand(5, 2)
        ]);

        var request =
            new ReplayRequest<TestInitialization, TestCommand>(
                new TestInitialization(10),
                log,
                log.Count);

        var first = AssertReplaySucceeded(replay.Replay(request));
        var second = AssertReplaySucceeded(replay.Replay(request));
        var third = AssertReplaySucceeded(replay.Replay(request));

        Assert.Equal(first.State.Value, second.State.Value);
        Assert.Equal(second.State.Value, third.State.Value);

        Assert.Equal(first.State.Trace, second.State.Trace);
        Assert.Equal(second.State.Trace, third.State.Trace);

        AssertEventsEqual(first.Events, second.Events);
        AssertEventsEqual(second.Events, third.Events);

        AssertEventBatchesEqual(first.EventBatches, second.EventBatches);
        AssertEventBatchesEqual(second.EventBatches, third.EventBatches);

        Assert.NotSame(first.State, second.State);
        Assert.NotSame(second.State, third.State);

        Assert.Equal(3, adapter.InitializationCount);
        Assert.Equal(9, adapter.ReplayExecutionCount);

        Assert.Equal(0, adapter.StorageReads);
        Assert.Equal(0, adapter.StorageWrites);
        Assert.Equal(0, adapter.UiCalls);
        Assert.Equal(0, adapter.PresentationCalls);
        Assert.Equal(0, adapter.AnalyticsCalls);
        Assert.Equal(0, adapter.ClockReads);
        Assert.Equal(0, adapter.SessionMutations);
        Assert.Equal(0, adapter.HostRandomCalls);
        Assert.Equal(0, adapter.PublishedSemanticEventCount);
    }

    [Fact]
    public void CallerOwnedMutableHistoryCannotChangeEstablishedReplayAuthority()
    {
        var mutableHistory = new List<TestCommand>
        {
            new(2, 1),
            new(3, 2)
        };

        var request =
            new ReplayRequest<TestInitialization, TestCommand>(
                new TestInitialization(10),
                mutableHistory,
                mutableHistory.Count);

        mutableHistory[0] = new TestCommand(500, 8);
        mutableHistory.Clear();
        mutableHistory.Add(new TestCommand(900, 8));

        Assert.Equal(2, request.ActionLog.Count);
        Assert.Equal([2, 3],
            request.ActionLog.Select(command => command.Delta));

        var replay = CreateReplay(new ConformanceAdapter());
        var result = AssertReplaySucceeded(replay.Replay(request));

        Assert.Equal(15, result.State.Value);
        Assert.Equal(["initial", "command:2", "command:3"], result.State.Trace);
        Assert.Equal(
            ["evt:0:2:0", "evt:1:3:0", "evt:1:3:1"],
            result.Events.Select(item => item.Id));
    }

    [Fact]
    public void TransactionProducedActionLogIsReplayAuthority()
    {
        var pipeline = CreatePipeline(new ConformanceAdapter());
        var authority = CreateAuthority();

        authority = AssertSucceeded(
            pipeline.Execute(authority, new TestCommand(2, 1))).Authority;

        var rejected = AssertFailed(
            pipeline.Execute(authority, new TestCommand(-1, 4)));

        authority = rejected.Authority;

        authority = AssertSucceeded(
            pipeline.Execute(authority, new TestCommand(3, 2))).Authority;

        authority = AssertSucceeded(
            pipeline.Execute(authority, new TestCommand(5, 1))).Authority;

        Assert.Equal([2, 3, 5],
            authority.ActionLog.Select(command => command.Delta));

        var replay = CreateReplay(new ConformanceAdapter());

        var replayed = AssertReplaySucceeded(
            replay.Replay(
                new ReplayRequest<TestInitialization, TestCommand>(
                    new TestInitialization(10),
                    authority.ActionLog,
                    authority.ActionCount)));

        Assert.Equal(authority.State.Value, replayed.State.Value);
        Assert.Equal(authority.State.Trace, replayed.State.Trace);
        Assert.Equal(authority.ActionCount, replayed.AppliedActionCount);
    }

    private static TransactionAuthority<
        MutableState,
        TestCommand,
        MutableDeterministicState> CreateAuthority() =>
        new(
            new MutableState(10, ["initial"]),
            new ActionLog<TestCommand>(Array.Empty<TestCommand>()),
            new MutableDeterministicState(0));

    private static TransactionPipeline<
        MutableState,
        TestCommand,
        TestEvent,
        MutableDeterministicState> CreatePipeline(
            ConformanceAdapter adapter) =>
        new(adapter);

    private static ReplayEngine<
        TestInitialization,
        MutableState,
        TestCommand,
        TestEvent> CreateReplay(ConformanceAdapter adapter) =>
        new(adapter);

    private static TransactionOutcome<
        MutableState,
        TestCommand,
        TestEvent,
        MutableDeterministicState>.Succeeded AssertSucceeded(
            TransactionOutcome<
                MutableState,
                TestCommand,
                TestEvent,
                MutableDeterministicState> outcome) =>
        Assert.IsType<TransactionOutcome<
            MutableState,
            TestCommand,
            TestEvent,
            MutableDeterministicState>.Succeeded>(outcome);

    private static TransactionOutcome<
        MutableState,
        TestCommand,
        TestEvent,
        MutableDeterministicState>.Failed AssertFailed(
            TransactionOutcome<
                MutableState,
                TestCommand,
                TestEvent,
                MutableDeterministicState> outcome) =>
        Assert.IsType<TransactionOutcome<
            MutableState,
            TestCommand,
            TestEvent,
            MutableDeterministicState>.Failed>(outcome);

    private static ReplayResult<MutableState, TestEvent>.Success
        AssertReplaySucceeded(
            ReplayResult<MutableState, TestEvent> result) =>
        Assert.IsType<ReplayResult<MutableState, TestEvent>.Success>(result);

    private static void AssertEventsEqual(
        IReadOnlyList<TestEvent> expected,
        IReadOnlyList<TestEvent> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        for (var index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index].Id, actual[index].Id);
            Assert.Equal(
                expected[index].ActionIndex,
                actual[index].ActionIndex);
            Assert.Equal(
                expected[index].CommandDelta,
                actual[index].CommandDelta);
            Assert.Equal(expected[index].Ordinal, actual[index].Ordinal);
            Assert.Equal(expected[index].ResultValue, actual[index].ResultValue);
        }
    }

    private static void AssertEventBatchesEqual(
        IReadOnlyList<IReadOnlyList<TestEvent>> expected,
        IReadOnlyList<IReadOnlyList<TestEvent>> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        for (var index = 0; index < expected.Count; index++)
        {
            AssertEventsEqual(expected[index], actual[index]);
        }
    }

    private sealed record TestInitialization(int InitialValue);

    private sealed class MutableState(int value, List<string> trace)
    {
        public int Value { get; set; } = value;

        public List<string> Trace { get; } = trace;
    }

    private sealed class MutableDeterministicState(int cursor)
    {
        public int Cursor { get; set; } = cursor;
    }

    private sealed record TestCommand(int Delta, int EventCount);

    private sealed record TestEvent(
        string Id,
        int ActionIndex,
        int CommandDelta,
        int Ordinal,
        int ResultValue);

    private sealed class ConformanceAdapter :
        ITransactionAdapter<
            MutableState,
            TestCommand,
            TestEvent,
            MutableDeterministicState>,
        IReplayAdapter<
            TestInitialization,
            MutableState,
            TestCommand,
            TestEvent>
    {
        public const string RejectedCode = "TEST_COMMAND_REJECTED";
        public const string EventLimitCode = "TEST_EVENT_LIMIT_EXCEEDED";

        private readonly bool _mutateRejectedCandidate;
        private int _replayCursor;

        public ConformanceAdapter(bool mutateRejectedCandidate = false)
        {
            _mutateRejectedCandidate = mutateRejectedCandidate;
        }

        public int InitializationCount { get; private set; }

        public int ReplayExecutionCount { get; private set; }

        public int LastReplayCursor => _replayCursor;

        public int StorageReads { get; private set; }

        public int StorageWrites { get; private set; }

        public int UiCalls { get; private set; }

        public int PresentationCalls { get; private set; }

        public int AnalyticsCalls { get; private set; }

        public int ClockReads { get; private set; }

        public int SessionMutations { get; private set; }

        public int HostRandomCalls { get; private set; }

        public int PublishedSemanticEventCount { get; private set; }

        public MutableState SnapshotState(MutableState state) =>
            new(state.Value, [.. state.Trace]);

        public MutableDeterministicState SnapshotDeterministicState(
            MutableDeterministicState deterministicState) =>
            new(deterministicState.Cursor);

        public TransactionCandidate<
            MutableState,
            TestEvent,
            MutableDeterministicState> Execute(
                MutableState candidateState,
                MutableDeterministicState candidateDeterministicState,
                int actionCount,
                TestCommand command)
        {
            if (command.Delta < 0)
            {
                if (_mutateRejectedCandidate)
                {
                    candidateState.Value = int.MinValue;
                    candidateState.Trace.Clear();
                    candidateDeterministicState.Cursor = int.MaxValue;
                }

                return new TransactionCandidate<
                    MutableState,
                    TestEvent,
                    MutableDeterministicState>.Rejected(
                        new TransactionDiagnostic(
                            RejectedCode,
                            $"delta:{command.Delta}"));
            }

            if (command.EventCount < 0 || command.EventCount > 8)
            {
                return new TransactionCandidate<
                    MutableState,
                    TestEvent,
                    MutableDeterministicState>.Rejected(
                        new TransactionDiagnostic(EventLimitCode));
            }

            candidateState.Value =
                checked(candidateState.Value + command.Delta);
            candidateState.Trace.Add($"command:{command.Delta}");
            candidateDeterministicState.Cursor =
                checked(
                    candidateDeterministicState.Cursor
                    + command.EventCount);

            var events = CreateEvents(
                actionCount,
                command,
                candidateState.Value);

            return new TransactionCandidate<
                MutableState,
                TestEvent,
                MutableDeterministicState>.Accepted(
                    candidateState,
                    candidateDeterministicState,
                    events);
        }

        public MutableState CreateInitialState(TestInitialization initialization)
        {
            InitializationCount++;
            _replayCursor = 0;
            return new MutableState(
                initialization.InitialValue,
                ["initial"]);
        }

        ReplayStepResult<MutableState, TestEvent>
            IReplayAdapter<
                TestInitialization,
                MutableState,
                TestCommand,
                TestEvent>.Execute(
                    MutableState state,
                    TestCommand command)
        {
            ReplayExecutionCount++;

            if (command.Delta < 0)
            {
                return new ReplayStepResult<
                    MutableState,
                    TestEvent>.Rejected(RejectedCode);
            }

            if (command.EventCount < 0 || command.EventCount > 8)
            {
                return new ReplayStepResult<
                    MutableState,
                    TestEvent>.Rejected(EventLimitCode);
            }

            var next = SnapshotState(state);
            next.Value = checked(next.Value + command.Delta);
            next.Trace.Add($"command:{command.Delta}");

            var actionIndex = next.Trace.Count - 2;
            var events = CreateEvents(
                actionIndex,
                command,
                next.Value);

            _replayCursor =
                checked(_replayCursor + command.EventCount);

            return new ReplayStepResult<
                MutableState,
                TestEvent>.Accepted(next, events);
        }

        private static TestEvent[] CreateEvents(
            int actionIndex,
            TestCommand command,
            int resultValue) =>
            Enumerable.Range(0, command.EventCount)
                .Select(
                    ordinal => new TestEvent(
                        $"evt:{actionIndex}:{command.Delta}:{ordinal}",
                        actionIndex,
                        command.Delta,
                        ordinal,
                        resultValue))
                .ToArray();
    }
}