// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Represents a remote storage item change.
/// </summary>
public sealed class StorageChange
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageChange"/> class.
    /// </summary>
    public StorageChange()
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageChange"/> class.
    /// </summary>
    public StorageChange(string ItemId, bool Removed, DateTimeOffset? Time, StorageItem Item)
    {
        this.ItemId = Guard.NotNullOrWhiteSpace(ItemId, nameof(ItemId));
        this.Removed = Removed;
        this.Time = Time;
        this.Item = Item;
    }

    // ● properties

    /// <summary>
    /// Gets or sets the provider-specific item id.
    /// </summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the item was removed.
    /// </summary>
    public bool Removed { get; set; }

    /// <summary>
    /// Gets or sets the provider change timestamp.
    /// </summary>
    public DateTimeOffset? Time { get; set; }

    /// <summary>
    /// Gets or sets the current item state when the item still exists.
    /// </summary>
    public StorageItem Item { get; set; }
}
