using Verdant.Core;
using Verdant.Replay;
using Xunit;

namespace Verdant.Core.Tests;

public sealed class TransactionPipelineTests
{
    [Fact]
    public void SuccessCommitsStateLogDeterministicStateAndOrderedEventsAtomically()
    {
        var adapter = new TestAdapter();
        var pipeline = CreatePipeline(adapter);
        var initial = CreateAuthority();
        var command = new TestCommand(3, 2);

        var success = AssertSuccess(pipeline.Execute(initial, command));

        Assert.Equal(13, success.Authority.State.Value);
        Assert.Equal(["existing", "item-0", "item-1"], success.Authority.State.Items);
        Assert.Equal(1, success.Authority.ActionCount);
        Assert.Equal([command], success.Authority.ActionLog);
        Assert.Equal(102, success.Authority.DeterministicState.Cursor);
        Assert.Equal(["changed:13", "cursor:102"], success.Events.Select(item => item.Code));
        Assert.Equal([13, 102], success.Events.Select(item => item.Value));

        Assert.Equal(10, initial.State.Value);
        Assert.Equal(["existing"], initial.State.Items);
        Assert.Empty(initial.ActionLog);
        Assert.Equal(100, initial.DeterministicState.Cursor);
    }

    [Fact]
    public void RejectionPreservesEveryAuthorityFieldAndEmitsNoSemanticEvents()
    {
        var adapter = new TestAdapter();
        var pipeline = CreatePipeline(adapter);
        var initial = CreateAuthority();

        var failure = AssertFailure(
            pipeline.Execute(initial, new TestCommand(-1, 4)));

        Assert.Same(initial, failure.Authority);
        Assert.Equal(TestAdapter.RejectedCode, failure.Diagnostic.Code);
        Assert.Equal("delta:-1", failure.Diagnostic.Detail);
        Assert.Equal(10, failure.Authority.State.Value);
        Assert.Equal(["existing"], failure.Authority.State.Items);
        Assert.Equal(0, failure.Authority.ActionCount);
        Assert.Empty(failure.Authority.ActionLog);
        Assert.Equal(100, failure.Authority.DeterministicState.Cursor);
        Assert.Equal(0, adapter.PublishedEventCount);
    }

    [Fact]
    public void RejectionCannotCommitMutationsMadeToCandidateSnapshots()
    {
        var adapter = new TestAdapter(mutateBeforeRejecting: true);
        var pipeline = CreatePipeline(adapter);
        var initial = CreateAuthority();

        _ = AssertFailure(
            pipeline.Execute(initial, new TestCommand(-1, 9)));

        Assert.Equal(10, initial.State.Value);
        Assert.Equal(["existing"], initial.State.Items);
        Assert.Equal(100, initial.DeterministicState.Cursor);
        Assert.Empty(initial.ActionLog);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-2000000000)]
    public void InvalidNegativeInputIsRejectedWithoutSilentRepair(int delta)
    {
        var pipeline = CreatePipeline(new TestAdapter());
        var initial = CreateAuthority();

        var failure = AssertFailure(
            pipeline.Execute(initial, new TestCommand(delta, 1)));

        Assert.Equal(TestAdapter.RejectedCode, failure.Diagnostic.Code);
        Assert.Equal($"delta:{delta}", failure.Diagnostic.Detail);
        Assert.Same(initial, failure.Authority);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    public void BoundaryValuesAreExecutedExactlyWithoutClamping(int eventCount)
    {
        var pipeline = CreatePipeline(new TestAdapter());
        var initial = CreateAuthority();

        var success = AssertSuccess(
            pipeline.Execute(initial, new TestCommand(1, eventCount)));

        Assert.Equal(11, success.Authority.State.Value);
        Assert.Equal(eventCount, success.Events.Count);
        Assert.Equal(
            Enumerable.Range(0, eventCount).Select(index => $"event:{index}"),
            success.Events.Select(item => item.Code));
    }

    [Fact]
    public void OversizedInputIsRejectedWithoutPartialCommit()
    {
        var pipeline = CreatePipeline(new TestAdapter());
        var initial = CreateAuthority();

        var failure = AssertFailure(
            pipeline.Execute(initial, new TestCommand(1, 9)));

        Assert.Equal(TestAdapter.EventLimitCode, failure.Diagnostic.Code);
        Assert.Same(initial, failure.Authority);
        Assert.Empty(initial.ActionLog);
    }

    [Fact]
    public void SuccessfulOnlyActionLogInvariantHoldsAcrossRepeatedInvocations()
    {
        var pipeline = CreatePipeline(new TestAdapter());
        var initial = CreateAuthority();

        var first = AssertSuccess(
            pipeline.Execute(initial, new TestCommand(2, 1)));
        var rejected = AssertFailure(
            pipeline.Execute(first.Authority, new TestCommand(-1, 1)));
        var second = AssertSuccess(
            pipeline.Execute(rejected.Authority, new TestCommand(5, 1)));

        Assert.Equal(2, second.Authority.ActionCount);
        Assert.Equal([2, 5], second.Authority.ActionLog.Select(item => item.Delta));
        Assert.Equal(17, second.Authority.State.Value);
        Assert.Equal(102, second.Authority.DeterministicState.Cursor);
    }

    [Fact]
    public void IdenticalExecutionFromCanonicalAuthorityIsRepeatable()
    {
        var command = new TestCommand(4, 3);
        var first = AssertSuccess(
            CreatePipeline(new TestAdapter()).Execute(CreateAuthority(), command));
        var second = AssertSuccess(
            CreatePipeline(new TestAdapter()).Execute(CreateAuthority(), command));

        Assert.Equal(first.Authority.State.Value, second.Authority.State.Value);
        Assert.Equal(first.Authority.State.Items, second.Authority.State.Items);
        Assert.Equal(first.Authority.ActionLog, second.Authority.ActionLog);
        Assert.Equal(first.Authority.ActionCount, second.Authority.ActionCount);
        Assert.Equal(
            first.Authority.DeterministicState.Cursor,
            second.Authority.DeterministicState.Cursor);
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void ReturnedCollectionsDoNotAliasAdapterOrSuppliedCollections()
    {
        var sourceLog = new List<TestCommand>();
        var initial = new TransactionAuthority<
            MutableState,
            TestCommand,
            MutableDeterministicState>(
                new MutableState(10, ["existing"]),
                new ActionLog<TestCommand>(sourceLog),
                new MutableDeterministicState(100));
        var adapter = new TestAdapter();
        var pipeline = CreatePipeline(adapter);

        var success = AssertSuccess(
            pipeline.Execute(initial, new TestCommand(1, 2)));

        sourceLog.Add(new TestCommand(99, 0));
        adapter.LastCandidateEvents!.Add(new TestEvent("late", 999));
        adapter.LastCandidateState!.Items.Add("late");

        Assert.Empty(initial.ActionLog);
        Assert.Single(success.Authority.ActionLog);
        Assert.Equal(["changed:11", "cursor:102"], success.Events.Select(item => item.Code));
        Assert.Equal(["existing", "item-0", "item-1"], success.Authority.State.Items);
    }

    [Fact]
    public void TransactionPipelineHasNoHostOrPersistenceDependencies()
    {
        var adapter = new TestAdapter();
        var pipeline = CreatePipeline(adapter);

        _ = AssertSuccess(
            pipeline.Execute(CreateAuthority(), new TestCommand(1, 1)));

        Assert.Equal(0, adapter.StorageReads);
        Assert.Equal(0, adapter.StorageWrites);
        Assert.Equal(0, adapter.UiCalls);
        Assert.Equal(0, adapter.ClockReads);
        Assert.Equal(0, adapter.HostRandomCalls);
    }

    [Fact]
    public void ReplayConsumesTransactionProducedSuccessfulLog()
    {
        var pipeline = CreatePipeline(new TestAdapter());
        var authority = CreateAuthority();

        authority = AssertSuccess(
            pipeline.Execute(authority, new TestCommand(2, 1))).Authority;
        _ = AssertFailure(
            pipeline.Execute(authority, new TestCommand(-1, 1)));
        authority = AssertSuccess(
            pipeline.Execute(authority, new TestCommand(3, 1))).Authority;

        var replay = new ReplayEngine<
            int,
            ReplayState,
            TestCommand,
            TestEvent>(new ReplayAdapter());

        var replayed = Assert.IsType<
            ReplayResult<ReplayState, TestEvent>.Success>(
                replay.Replay(
                    new ReplayRequest<int, TestCommand>(
                        10,
                        authority.ActionLog,
                        authority.ActionCount)));

        Assert.Equal(authority.State.Value, replayed.State.Value);
        Assert.Equal(2, replayed.AppliedActionCount);
        Assert.Equal([2, 3], authority.ActionLog.Select(item => item.Delta));
        Assert.Equal(["replayed:12", "replayed:15"], replayed.Events.Select(item => item.Code));
    }

    private static TransactionAuthority<
        MutableState,
        TestCommand,
        MutableDeterministicState> CreateAuthority() =>
        new(
            new MutableState(10, ["existing"]),
            new ActionLog<TestCommand>(Array.Empty<TestCommand>()),
            new MutableDeterministicState(100));

    private static TransactionPipeline<
        MutableState,
        TestCommand,
        TestEvent,
        MutableDeterministicState> CreatePipeline(TestAdapter adapter) =>
        new(adapter);

    private static TransactionOutcome<
        MutableState,
        TestCommand,
        TestEvent,
        MutableDeterministicState>.Succeeded AssertSuccess(
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
        MutableDeterministicState>.Failed AssertFailure(
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

    private sealed class MutableState(int value, List<string> items)
    {
        public int Value { get; set; } = value;

        public List<string> Items { get; } = items;
    }

    private sealed class MutableDeterministicState(int cursor)
    {
        public int Cursor { get; set; } = cursor;
    }

    private sealed record TestCommand(int Delta, int EventCount);

    private sealed record TestEvent(string Code, int Value);

    private sealed class TestAdapter(bool mutateBeforeRejecting = false) :
        ITransactionAdapter<
            MutableState,
            TestCommand,
            TestEvent,
            MutableDeterministicState>
    {
        public const string RejectedCode = "TEST_COMMAND_REJECTED";
        public const string EventLimitCode = "TEST_EVENT_LIMIT_EXCEEDED";

        public MutableState? LastCandidateState { get; private set; }

        public List<TestEvent>? LastCandidateEvents { get; private set; }

        public int PublishedEventCount { get; private set; }

        public int StorageReads { get; private set; }

        public int StorageWrites { get; private set; }

        public int UiCalls { get; private set; }

        public int ClockReads { get; private set; }

        public int HostRandomCalls { get; private set; }

        public MutableState SnapshotState(MutableState state) =>
            new(state.Value, [.. state.Items]);

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
                if (mutateBeforeRejecting)
                {
                    candidateState.Value = int.MinValue;
                    candidateState.Items.Clear();
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

            if (command.EventCount > 8)
            {
                return new TransactionCandidate<
                    MutableState,
                    TestEvent,
                    MutableDeterministicState>.Rejected(
                        new TransactionDiagnostic(EventLimitCode));
            }

            candidateState.Value = checked(candidateState.Value + command.Delta);
            for (var index = 0; index < command.EventCount; index++)
            {
                candidateState.Items.Add($"item-{index}");
            }

            candidateDeterministicState.Cursor =
                checked(candidateDeterministicState.Cursor + command.EventCount);

            LastCandidateState = candidateState;
            LastCandidateEvents = command.EventCount == 2
                ?
                [
                    new TestEvent("changed:" + candidateState.Value, candidateState.Value),
                    new TestEvent("cursor:" + candidateDeterministicState.Cursor, candidateDeterministicState.Cursor)
                ]
                : Enumerable.Range(0, command.EventCount)
                    .Select(index => new TestEvent($"event:{index}", index))
                    .ToList();

            return new TransactionCandidate<
                MutableState,
                TestEvent,
                MutableDeterministicState>.Accepted(
                    candidateState,
                    candidateDeterministicState,
                    LastCandidateEvents);
        }
    }

    private sealed record ReplayState(int Value);

    private sealed class ReplayAdapter :
        IReplayAdapter<int, ReplayState, TestCommand, TestEvent>
    {
        public ReplayState CreateInitialState(int initialization) =>
            new(initialization);

        public ReplayStepResult<ReplayState, TestEvent> Execute(
            ReplayState state,
            TestCommand command)
        {
            var next = new ReplayState(state.Value + command.Delta);
            return new ReplayStepResult<ReplayState, TestEvent>.Accepted(
                next,
                [new TestEvent($"replayed:{next.Value}", next.Value)]);
        }
    }
}