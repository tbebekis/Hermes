// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Contains the metadata outcome of applying synchronization execution results.
/// </summary>
public class SyncExecutionApplyResult
{
    // ● properties

    /// <summary>
    /// Gets the execution results that completed and were committed to base snapshots.
    /// </summary>
    public List<SyncExecutionResult> CommittedResults { get; } = new();

    /// <summary>
    /// Gets execution results that were not committed to base snapshots.
    /// </summary>
    public List<SyncExecutionResult> UncommittedResults { get; } = new();

    /// <summary>
    /// Gets the base snapshots committed while applying execution results.
    /// </summary>
    public List<BaseSnapshotRecord> CommittedBaseSnapshots { get; } = new();
}
