// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Represents an item observed during a local filesystem scan.
/// </summary>
public class LocalScanItem
{
    // ● properties

    /// <summary>
    /// Gets or sets the local relative path.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parent relative path.
    /// </summary>
    public string ParentRelativePath { get; set; }
    /// <summary>
    /// Gets or sets the item type.
    /// </summary>
    public string ItemType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content size.
    /// </summary>
    public long? Size { get; set; }
    /// <summary>
    /// Gets or sets the modified time.
    /// </summary>
    public DateTime ModifiedTime { get; set; }
    /// <summary>
    /// Gets or sets the content hash.
    /// </summary>
    public string ContentHash { get; set; }
}
