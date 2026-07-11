// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Represents provider-neutral item state used by the in-memory diff classifier.
/// </summary>
public class SyncItemState
{
    // ● properties

    /// <summary>
    /// Gets or sets a value indicating whether the item exists at the observed endpoint.
    /// </summary>
    public bool Exists { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the remote provider reported removal.
    /// </summary>
    public bool Removed { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the remote item is trashed.
    /// </summary>
    public bool Trashed { get; set; }
    /// <summary>
    /// Gets or sets the item type.
    /// </summary>
    public string ItemType { get; set; }
    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the local relative path.
    /// </summary>
    public string LocalRelativePath { get; set; }
    /// <summary>
    /// Gets or sets the local relative path projected from the remote namespace.
    /// </summary>
    public string ProjectedLocalRelativePath { get; set; }
    /// <summary>
    /// Gets or sets the remote parent id.
    /// </summary>
    public string RemoteParentId { get; set; }
    /// <summary>
    /// Gets or sets the content hash.
    /// </summary>
    public string ContentHash { get; set; }
    /// <summary>
    /// Gets or sets the content size.
    /// </summary>
    public long Size { get; set; }
    /// <summary>
    /// Gets or sets the provider version.
    /// </summary>
    public long ProviderVersion { get; set; }
}
