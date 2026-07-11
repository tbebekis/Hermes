// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Represents a durable synchronization conflict.
/// </summary>
public class SyncConflictRecord
{
    // ● properties

    /// <summary>
    /// Gets or sets the conflict id.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the sync root id.
    /// </summary>
    public string SyncRootId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the tracked item id.
    /// </summary>
    public string TrackedItemId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the diff kind.
    /// </summary>
    public SyncDiffKind DiffKind { get; set; }
    /// <summary>
    /// Gets or sets the decision kind.
    /// </summary>
    public SyncPlanDecisionKind DecisionKind { get; set; }
    /// <summary>
    /// Gets or sets the conflict state.
    /// </summary>
    public SyncConflictState State { get; set; }
    /// <summary>
    /// Gets or sets the conflict message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the first observed time.
    /// </summary>
    public DateTime FirstObservedTime { get; set; }
    /// <summary>
    /// Gets or sets the last observed time.
    /// </summary>
    public DateTime LastObservedTime { get; set; }
    /// <summary>
    /// Gets or sets the resolved time.
    /// </summary>
    public DateTime? ResolvedTime { get; set; }
}
