// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Contains the result of bootstrapping remote provider items into tracked items.
/// </summary>
public class RemoteBootstrapResult
{
    // ● properties

    /// <summary>
    /// Gets the tracked items created for previously unknown remote items.
    /// </summary>
    public List<TrackedItemRecord> CreatedTrackedItems { get; } = new();
    /// <summary>
    /// Gets the existing local tracked items adopted by matching remote items.
    /// </summary>
    public List<TrackedItemRecord> AdoptedTrackedItems { get; } = new();

    /// <summary>
    /// Gets the remote observations created or updated during bootstrap.
    /// </summary>
    public List<RemoteObservedSnapshotRecord> Observations { get; } = new();
    /// <summary>
    /// Gets the base snapshots committed for adopted local and remote items.
    /// </summary>
    public List<BaseSnapshotRecord> CommittedBaseSnapshots { get; } = new();
}
