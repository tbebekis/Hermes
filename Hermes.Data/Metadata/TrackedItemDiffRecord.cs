// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Represents the classified synchronization difference of a tracked item.
/// </summary>
public class TrackedItemDiffRecord
{
    // ● properties

    /// <summary>
    /// Gets or sets the tracked item id.
    /// </summary>
    public string TrackedItemId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the classified difference kind.
    /// </summary>
    public SyncDiffKind DiffKind { get; set; }
}
