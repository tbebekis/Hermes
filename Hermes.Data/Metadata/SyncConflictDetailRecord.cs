// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Contains an open conflict with the metadata context needed for display.
/// </summary>
public class SyncConflictDetailRecord
{
    // ● properties

    /// <summary>
    /// Gets or sets the durable conflict row.
    /// </summary>
    public SyncConflictRecord Conflict { get; set; }
    /// <summary>
    /// Gets or sets the tracked item identity.
    /// </summary>
    public TrackedItemRecord TrackedItem { get; set; }
    /// <summary>
    /// Gets or sets the last committed common state.
    /// </summary>
    public BaseSnapshotRecord BaseSnapshot { get; set; }
    /// <summary>
    /// Gets or sets the latest local observation.
    /// </summary>
    public LocalObservedSnapshotRecord LocalObservation { get; set; }
    /// <summary>
    /// Gets or sets the latest remote observation.
    /// </summary>
    public RemoteObservedSnapshotRecord RemoteObservation { get; set; }
}
