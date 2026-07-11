// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests metadata synchronization run result summaries.
/// </summary>
public class MetadataSyncRunResultTests
{
    // ● private

    static SyncExecutionRequest CollisionRequest(string TrackedItemId, string RemoteItemId) => new()
    {
        Decision = new SyncPlanDecision(TrackedItemId, SyncDiffKind.NamespaceCollision, SyncPlanDecisionKind.Blocked),
        RemoteObservation = new RemoteObservedSnapshotRecord()
        {
            RemoteItemId = RemoteItemId,
            RemoteParentId = "remote-parent",
            Name = "DuplicateName.txt",
        },
    };
    static SyncExecutionRequest ConflictRequest() => new()
    {
        Decision = new SyncPlanDecision("item-1", SyncDiffKind.Conflict, SyncPlanDecisionKind.Conflict),
        TrackedItem = new TrackedItemRecord()
        {
            Id = "item-1",
            RemoteItemId = "remote-1",
        },
        LocalObservation = new LocalObservedSnapshotRecord()
        {
            Name = "File1.txt",
        },
        RemoteObservation = new RemoteObservedSnapshotRecord()
        {
            RemoteItemId = "remote-1",
            Removed = true,
        },
    };

    // ● public

    /// <summary>
    /// Verifies namespace collision summary groups colliding remote siblings.
    /// </summary>
    [Fact]
    public void NamespaceCollisionSummaryGroupsCollidingRemoteSiblings()
    {
        MetadataSyncSessionResult SessionResult = new();
        SessionResult.PendingExecutionRequests.Add(CollisionRequest("item-1", "remote-1"));
        SessionResult.PendingExecutionRequests.Add(CollisionRequest("item-2", "remote-2"));
        MetadataSyncRunResult Result = new()
        {
            SessionResult = SessionResult,
        };

        Assert.Equal("DuplicateName.txt@remote-parent=2", Result.NamespaceCollisionSummary);
        Assert.Equal("Blocked=2", Result.PendingExecutionSummary);
        Assert.Equal("NamespaceCollision=2", Result.PendingDiffSummary);
        Assert.Equal("NamespaceCollision:DuplicateName.txt#remote-1, NamespaceCollision:DuplicateName.txt#remote-2", Result.BlockedExecutionSummary);
    }
    /// <summary>
    /// Verifies conflict summaries include pending and uncommitted conflict information.
    /// </summary>
    [Fact]
    public void ConflictSummariesIncludePendingAndUncommittedConflicts()
    {
        SyncExecutionRequest Request = ConflictRequest();
        MetadataSyncSessionResult SessionResult = new();
        SessionResult.PendingExecutionRequests.Add(Request);
        SyncExecutionApplyResult ApplyResult = new();
        ApplyResult.UncommittedResults.Add(new SyncExecutionResult()
        {
            Request = Request,
            ResultKind = SyncExecutionResultKind.Conflict,
            Message = "Conflict resolution is required.",
        });
        MetadataSyncRunResult Result = new()
        {
            SessionResult = SessionResult,
            ExecutionApplyResult = ApplyResult,
        };

        Assert.Equal("Conflict=1", Result.PendingExecutionSummary);
        Assert.Equal("Conflict=1", Result.PendingDiffSummary);
        Assert.Equal("none", Result.BlockedExecutionSummary);
        Assert.Equal("Conflict=1", Result.UncommittedExecutionSummary);
        Assert.Equal("Conflict:File1.txt#remote-1:Conflict resolution is required.", Result.UncommittedExecutionMessages);
    }
}
