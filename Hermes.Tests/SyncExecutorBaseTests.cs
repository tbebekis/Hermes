// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests synchronization executor base behavior.
/// </summary>
public class SyncExecutorBaseTests
{
    // ● private

    /// <summary>
    /// Test executor that records executable intents.
    /// </summary>
    sealed class TestSyncExecutor : SyncExecutorBase
    {
        // ● fields

        readonly SyncExecutionResultKind fResultKind;

        // ● protected

        /// <summary>
        /// Executes an intent that passed common validation.
        /// </summary>
        protected override Task<SyncExecutionResult> ExecuteIntentAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
        {
            Intents.Add(Intent);

            return Task.FromResult(new SyncExecutionResult()
            {
                Request = Intent.Request,
                ResultKind = fResultKind,
            });
        }

        // ● constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSyncExecutor"/> class.
        /// </summary>
        public TestSyncExecutor(SyncExecutionResultKind ResultKind = SyncExecutionResultKind.CompletedAndVerified)
        {
            fResultKind = ResultKind;
        }

        // ● properties

        /// <summary>
        /// Gets the executable intents received by the executor.
        /// </summary>
        public List<SyncExecutionIntent> Intents { get; } = new();
    }
    static SyncPlanDecision Decision(SyncPlanDecisionKind DecisionKind) => new("item-1", SyncDiffKind.LocalChanged, DecisionKind);
    static TrackedItemRecord TrackedItem() => new()
    {
        Id = "item-1",
        SyncRootId = "root-1",
        RemoteItemId = "remote-1",
        LocalKey = "File.txt",
        ItemType = "File",
    };
    static BaseSnapshotRecord BaseSnapshot() => new()
    {
        TrackedItemId = "item-1",
        ExistsFlag = true,
        ItemType = "File",
        Name = "File.txt",
        LocalRelativePath = "File.txt",
        RemoteParentId = "remote-root",
        Size = 42,
        ContentHash = "hash-base",
        ProviderVersion = 1,
        Trashed = false,
        CommittedTime = new DateTime(2026, 7, 11, 9, 10, 0, DateTimeKind.Utc),
    };
    static LocalObservedSnapshotRecord LocalObservation() => new()
    {
        TrackedItemId = "item-1",
        ExistsFlag = true,
        RelativePath = "File.txt",
        Name = "File.txt",
        ItemType = "File",
        Size = 42,
        ContentHash = "hash-local",
        ObservedTime = new DateTime(2026, 7, 11, 9, 10, 0, DateTimeKind.Utc),
    };
    static RemoteObservedSnapshotRecord RemoteObservation() => new()
    {
        TrackedItemId = "item-1",
        RemoteItemId = "remote-1",
        ExistsFlag = true,
        Removed = false,
        Name = "File.txt",
        RemoteParentId = "remote-root",
        ItemType = "File",
        Size = 42,
        ContentHash = "hash-remote",
        ProviderVersion = 2,
        Trashed = false,
        ObservedTime = new DateTime(2026, 7, 11, 9, 10, 0, DateTimeKind.Utc),
    };
    static SyncExecutionRequest Request(SyncPlanDecisionKind DecisionKind) => new()
    {
        Decision = Decision(DecisionKind),
        TrackedItem = TrackedItem(),
        BaseSnapshot = BaseSnapshot(),
        LocalObservation = LocalObservation(),
        RemoteObservation = RemoteObservation(),
    };

    // ● public

    /// <summary>
    /// Verifies executable intents are passed to the derived executor.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncExecutesValidIntent()
    {
        TestSyncExecutor Executor = new();

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.UploadToRemote)],
            CancellationToken.None);

        Assert.Single(Executor.Intents);
        Assert.Single(Results);
        Assert.Equal(SyncExecutionIntentKind.UploadToRemote, Executor.Intents[0].IntentKind);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
    }
    /// <summary>
    /// Verifies conflict intents are rejected before derived execution.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncRejectsConflictIntent()
    {
        TestSyncExecutor Executor = new();

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.Conflict)],
            CancellationToken.None);

        Assert.Empty(Executor.Intents);
        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.Conflict, Results[0].ResultKind);
        Assert.Contains("Conflict resolution is required.", Results[0].Message);
    }
    /// <summary>
    /// Verifies invalid executable intents are rejected before derived execution.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncRejectsInvalidIntent()
    {
        TestSyncExecutor Executor = new();
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.PropagateLocalDelete);
        ExecutionRequest.TrackedItem.RemoteItemId = string.Empty;
        ExecutionRequest.RemoteObservation.RemoteItemId = string.Empty;

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [ExecutionRequest],
            CancellationToken.None);

        Assert.Empty(Executor.Intents);
        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.FailedPermanent, Results[0].ResultKind);
        Assert.Contains("Remote item id is required.", Results[0].Message);
    }
}
