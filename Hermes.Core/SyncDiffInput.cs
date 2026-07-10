// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Contains the three-way state input for synchronization diff classification.
/// </summary>
public class SyncDiffInput
{
    // ● properties

    /// <summary>
    /// Gets or sets the last committed common state.
    /// </summary>
    public SyncItemState BaseState { get; set; }
    /// <summary>
    /// Gets or sets the latest local observation.
    /// </summary>
    public SyncItemState LocalState { get; set; }
    /// <summary>
    /// Gets or sets the latest remote observation.
    /// </summary>
    public SyncItemState RemoteState { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether namespace mapping found a collision.
    /// </summary>
    public bool NamespaceCollisionDetected { get; set; }
}
