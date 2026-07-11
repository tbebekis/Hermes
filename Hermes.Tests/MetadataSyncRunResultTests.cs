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
}

