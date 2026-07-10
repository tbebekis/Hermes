// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests unsupported synchronization executor behavior.
/// </summary>
public class UnsupportedSyncExecutorTests
{
    // ● private

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
        CommittedTime = new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc),
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
        ObservedTime = new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc),
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
        ObservedTime = new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc),
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
    /// Verifies executable requests are reported as blocked until execution is implemented.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncReturnsBlockedForExecutableRequests()
    {
        UnsupportedSyncExecutor Executor = new();

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.UploadToRemote)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.Blocked, Results[0].ResultKind);
        Assert.Equal("Synchronization execution is not implemented.", Results[0].Message);
    }
    /// <summary>
    /// Verifies non-executable requests are still rejected by base validation.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncUsesBaseValidationForNonExecutableRequests()
    {
        UnsupportedSyncExecutor Executor = new();

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.Conflict)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.Conflict, Results[0].ResultKind);
        Assert.Contains("Conflict resolution is required.", Results[0].Message);
    }
}
