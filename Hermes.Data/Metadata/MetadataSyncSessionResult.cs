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
    /// Gets the base snapshots committed during metadata-only advancement.
    /// </summary>
    public List<BaseSnapshotRecord> CommittedBaseSnapshots { get; } = new();
}
