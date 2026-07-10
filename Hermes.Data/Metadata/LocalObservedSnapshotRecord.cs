// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Represents the latest local filesystem observation for a tracked item.
/// </summary>
public class LocalObservedSnapshotRecord
{
    // ● properties

    /// <summary>
    /// Gets or sets the tracked item id.
    /// </summary>
    public string TrackedItemId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the item was observed locally.
    /// </summary>
    public bool ExistsFlag { get; set; }
    /// <summary>
    /// Gets or sets the relative path.
    /// </summary>
    public string RelativePath { get; set; }
    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the parent relative path.
    /// </summary>
    public string ParentRelativePath { get; set; }
    /// <summary>
    /// Gets or sets the item type.
    /// </summary>
    public string ItemType { get; set; }
    /// <summary>
    /// Gets or sets the content size.
    /// </summary>
    public long? Size { get; set; }
    /// <summary>
    /// Gets or sets the local modified time.
    /// </summary>
    public DateTime? ModifiedTime { get; set; }
    /// <summary>
    /// Gets or sets the content hash.
    /// </summary>
    public string ContentHash { get; set; }
    /// <summary>
    /// Gets or sets the observation time.
    /// </summary>
    public DateTime ObservedTime { get; set; }
    /// <summary>
    /// Gets or sets the scan id.
    /// </summary>
    public string ScanId { get; set; }
}
