// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Contains the result of a metadata synchronization session step.
/// </summary>
public class MetadataSyncSessionResult
{
    // ● properties

    /// <summary>
    /// Gets the planner decisions produced for the sync root.
    /// </summary>
    public List<SyncPlanDecision> Decisions { get; } = new();

    /// <summary>
    /// Gets tracked items created while importing endpoint observations.
    /// </summary>
    public List<TrackedItemRecord> CreatedTrackedItems { get; } = new();

    /// <summary>
    /// Gets remote changes that could not be attached to a tracked item.
    /// </summary>
    public List<StorageChange> UntrackedRemoteChanges { get; } = new();

    /// <summary>
    /// Gets the base snapshots committed during metadata-only advancement.
    /// </summary>
    public List<BaseSnapshotRecord> CommittedBaseSnapshots { get; } = new();

    /// <summary>
    /// Gets the planner decisions that still require executor action or external resolution.
    /// </summary>
    public List<SyncPlanDecision> PendingExecutorDecisions { get; } = new();

    /// <summary>
    /// Gets metadata context for planner decisions that still require executor action or external resolution.
    /// </summary>
    public List<SyncExecutionRequest> PendingExecutionRequests { get; } = new();
}
