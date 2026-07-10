// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Represents the remote changes checkpoint for a sync root.
/// </summary>
public class RemoteCheckpointRecord
{
    // ● properties

    /// <summary>
    /// Gets or sets the sync root id.
    /// </summary>
    public string SyncRootId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the provider connection id.
    /// </summary>
    public string ConnectionId { get; set; }
    /// <summary>
    /// Gets or sets the changes start page token.
    /// </summary>
    public string StartPageToken { get; set; }
    /// <summary>
    /// Gets or sets the checkpoint update time.
    /// </summary>
    public DateTime? UpdatedTime { get; set; }
}
