// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Represents a Google Drive change mapped to provider-neutral data.
/// </summary>
public class GoogleDriveChangeItem
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleDriveChangeItem"/> class.
    /// </summary>
    public GoogleDriveChangeItem(string FileId, bool Removed, DateTimeOffset Time, StorageItem Item)
    {
        this.FileId = FileId ?? string.Empty;
        this.Removed = Removed;
        this.Time = Time;
        this.Item = Item;
    }

    // ● properties

    /// <summary>
    /// Gets the changed file id.
    /// </summary>
    public string FileId { get; }
    /// <summary>
    /// Gets a value indicating whether the item was removed.
    /// </summary>
    public bool Removed { get; }
    /// <summary>
    /// Gets the change timestamp.
    /// </summary>
    public DateTimeOffset Time { get; }
    /// <summary>
    /// Gets the mapped storage item when the change contains a file object.
    /// </summary>
    public StorageItem Item { get; }
    /// <summary>
    /// Gets a value indicating whether this change contains a file object.
    /// </summary>
    public bool HasFile => Item != null;
}
