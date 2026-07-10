// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Represents the last committed common state for a tracked item.
/// </summary>
public class BaseSnapshotRecord
{
    // ● properties

    /// <summary>
    /// Gets or sets the tracked item id.
    /// </summary>
    public string TrackedItemId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the item exists in the committed state.
    /// </summary>
    public bool ExistsFlag { get; set; }
    /// <summary>
    /// Gets or sets the item type.
    /// </summary>
    public string ItemType { get; set; }
    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the resolved local relative path.
    /// </summary>
    public string LocalRelativePath { get; set; }
    /// <summary>
    /// Gets or sets the remote parent id.
    /// </summary>
    public string RemoteParentId { get; set; }
    /// <summary>
    /// Gets or sets the content size.
    /// </summary>
    public long? Size { get; set; }
    /// <summary>
    /// Gets or sets the content hash.
    /// </summary>
    public string ContentHash { get; set; }
    /// <summary>
    /// Gets or sets the provider created time.
    /// </summary>
    public DateTime? CreatedTime { get; set; }
    /// <summary>
    /// Gets or sets the provider modified time.
    /// </summary>
    public DateTime? ModifiedTime { get; set; }
    /// <summary>
    /// Gets or sets the provider version.
    /// </summary>
    public long? ProviderVersion { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the item is trashed remotely.
    /// </summary>
    public bool? Trashed { get; set; }
    /// <summary>
    /// Gets or sets the commit time.
    /// </summary>
    public DateTime CommittedTime { get; set; }
}
