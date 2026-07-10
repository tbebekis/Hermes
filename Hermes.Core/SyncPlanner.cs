// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Creates synchronization plans from local and remote changes.
/// </summary>
public class SyncPlanner
{
    // ● public

    /// <summary>
    /// Creates a planning decision for a classified tracked item.
    /// </summary>
    public SyncPlanDecision CreateDecision(SyncPlanInput Input)
    {
        Guard.NotNull(Input, nameof(Input));

        SyncPlanDecisionKind DecisionKind = Input.DiffKind switch
        {
            SyncDiffKind.NoChange => SyncPlanDecisionKind.None,
            SyncDiffKind.LocalChanged => SyncPlanDecisionKind.UploadToRemote,
            SyncDiffKind.RemoteChanged => SyncPlanDecisionKind.DownloadToLocal,
            SyncDiffKind.RemoteNamespaceChanged => SyncPlanDecisionKind.ApplyRemoteNamespaceToLocal,
            SyncDiffKind.LocalNamespaceChanged => SyncPlanDecisionKind.ApplyLocalNamespaceToRemote,
            SyncDiffKind.BothChangedCompatible => SyncPlanDecisionKind.CommitBase,
            SyncDiffKind.Conflict => SyncPlanDecisionKind.Conflict,
            SyncDiffKind.LocalMissing => SyncPlanDecisionKind.PropagateLocalDelete,
            SyncDiffKind.RemoteMissing => SyncPlanDecisionKind.PropagateRemoteDelete,
            SyncDiffKind.RemoteTrashed => SyncPlanDecisionKind.PropagateRemoteDelete,
            SyncDiffKind.RemoteRemoved => SyncPlanDecisionKind.PropagateRemoteDelete,
            SyncDiffKind.NamespaceCollision => SyncPlanDecisionKind.Blocked,
            _ => SyncPlanDecisionKind.Blocked,
        };

        return new SyncPlanDecision(Input.TrackedItemId, Input.DiffKind, DecisionKind);
    }
    /// <summary>
    /// Creates planning decisions for classified tracked items.
    /// </summary>
    public IReadOnlyList<SyncPlanDecision> CreateDecisions(IEnumerable<SyncPlanInput> Inputs)
    {
        Guard.NotNull(Inputs, nameof(Inputs));

        List<SyncPlanDecision> Result = new();

        foreach (SyncPlanInput Input in Inputs)
            Result.Add(CreateDecision(Input));

        return Result;
    }
}
