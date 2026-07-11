// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Represents a recent synchronization activity entry.
/// </summary>
public class SyncActivityRecord
{
    // ● properties

    /// <summary>
    /// Gets or sets the activity timestamp in UTC.
    /// </summary>
    public DateTime TimestampUtc { get; set; }
    /// <summary>
    /// Gets or sets the activity level.
    /// </summary>
    public string Level { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the synchronization root id.
    /// </summary>
    public string SyncRootId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the activity title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the activity details.
    /// </summary>
    public string Details { get; set; } = string.Empty;
}
