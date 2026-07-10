// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Represents a planner decision for one tracked item.
/// </summary>
public class SyncPlanDecision
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlanDecision"/> class.
    /// </summary>
    public SyncPlanDecision(string TrackedItemId, SyncDiffKind DiffKind, SyncPlanDecisionKind DecisionKind)
    {
        this.TrackedItemId = TrackedItemId ?? string.Empty;
        this.DiffKind = DiffKind;
        this.DecisionKind = DecisionKind;
    }

    // ● properties

    /// <summary>
    /// Gets the tracked item id.
    /// </summary>
    public string TrackedItemId { get; }
    /// <summary>
    /// Gets the classified difference kind.
    /// </summary>
    public SyncDiffKind DiffKind { get; }
    /// <summary>
    /// Gets the planner decision kind.
    /// </summary>
    public SyncPlanDecisionKind DecisionKind { get; }
}
