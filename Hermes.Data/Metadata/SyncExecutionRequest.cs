// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Contains metadata context for a planner decision that still requires executor work.
/// </summary>
public class SyncExecutionRequest
{
    // ● properties

    /// <summary>
    /// Gets or sets the planner decision.
    /// </summary>
    public SyncPlanDecision Decision { get; set; }

    /// <summary>
    /// Gets or sets the synchronization root.
    /// </summary>
    public SyncRootRecord SyncRoot { get; set; }

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
