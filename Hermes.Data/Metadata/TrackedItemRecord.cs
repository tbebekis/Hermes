// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Represents a provider-neutral tracked item identity.
/// </summary>
public class TrackedItemRecord
{
    // ● properties

    /// <summary>
    /// Gets or sets the tracked item id.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the sync root id.
    /// </summary>
    public string SyncRootId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the remote item id.
    /// </summary>
    public string RemoteItemId { get; set; }
    /// <summary>
    /// Gets or sets the local identity key.
    /// </summary>
    public string LocalKey { get; set; }
    /// <summary>
    /// Gets or sets the item type.
    /// </summary>
    public string ItemType { get; set; } = string.Empty;
}
