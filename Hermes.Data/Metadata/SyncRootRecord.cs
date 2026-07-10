// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Represents a synchronization root persisted in the metadata store.
/// </summary>
public class SyncRootRecord
{
    // ● properties

    /// <summary>
    /// Gets or sets the sync root id.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the provider connection id.
    /// </summary>
    public string ConnectionId { get; set; }
    /// <summary>
    /// Gets or sets the local root path.
    /// </summary>
    public string LocalRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the remote root item id.
    /// </summary>
    public string RemoteRootItemId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether this sync root is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime CreatedTime { get; set; }
}
