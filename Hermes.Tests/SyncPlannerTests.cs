// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests synchronization planning decisions.
/// </summary>
public class SyncPlannerTests
{
    // ● private

    static SyncPlanDecisionKind CreateDecision(SyncDiffKind DiffKind)
    {
        SyncPlanner Planner = new();
        SyncPlanDecision Decision = Planner.CreateDecision(new SyncPlanInput()
        {
            TrackedItemId = "item-1",
            DiffKind = DiffKind,
        });

        return Decision.DecisionKind;
    }

    // ● public

    /// <summary>
    /// Verifies diff classifications map to planner decisions.
    /// </summary>
    [Theory]
    [InlineData(SyncDiffKind.NoChange, SyncPlanDecisionKind.None)]
    [InlineData(SyncDiffKind.LocalChanged, SyncPlanDecisionKind.UploadToRemote)]
    [InlineData(SyncDiffKind.RemoteChanged, SyncPlanDecisionKind.DownloadToLocal)]
    [InlineData(SyncDiffKind.RemoteNamespaceChanged, SyncPlanDecisionKind.ApplyRemoteNamespaceToLocal)]
    [InlineData(SyncDiffKind.BothChangedCompatible, SyncPlanDecisionKind.CommitBase)]
    [InlineData(SyncDiffKind.Conflict, SyncPlanDecisionKind.Conflict)]
    [InlineData(SyncDiffKind.LocalMissing, SyncPlanDecisionKind.PropagateLocalDelete)]
    [InlineData(SyncDiffKind.RemoteMissing, SyncPlanDecisionKind.PropagateRemoteDelete)]
    [InlineData(SyncDiffKind.RemoteTrashed, SyncPlanDecisionKind.PropagateRemoteDelete)]
    [InlineData(SyncDiffKind.RemoteRemoved, SyncPlanDecisionKind.PropagateRemoteDelete)]
    [InlineData(SyncDiffKind.NamespaceCollision, SyncPlanDecisionKind.Blocked)]
    public void CreateDecisionMapsDiffKindToDecisionKind(SyncDiffKind DiffKind, SyncPlanDecisionKind Expected)
    {
        Assert.Equal(Expected, CreateDecision(DiffKind));
    }
    /// <summary>
    /// Verifies batch decision creation preserves item ids and order.
    /// </summary>
    [Fact]
    public void CreateDecisionsPreservesItemIdsAndOrder()
    {
        SyncPlanner Planner = new();

        IReadOnlyList<SyncPlanDecision> Decisions = Planner.CreateDecisions(
        [
            new SyncPlanInput() { TrackedItemId = "item-1", DiffKind = SyncDiffKind.LocalChanged },
            new SyncPlanInput() { TrackedItemId = "item-2", DiffKind = SyncDiffKind.RemoteChanged },
        ]);

        Assert.Equal(2, Decisions.Count);
        Assert.Equal("item-1", Decisions[0].TrackedItemId);
        Assert.Equal(SyncPlanDecisionKind.UploadToRemote, Decisions[0].DecisionKind);
        Assert.Equal("item-2", Decisions[1].TrackedItemId);
        Assert.Equal(SyncPlanDecisionKind.DownloadToLocal, Decisions[1].DecisionKind);
    }
}
