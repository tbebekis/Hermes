// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Maps provider storage changes to remote observation records.
/// </summary>
static public class RemoteObservationMapper
{
    // ● private

    static string ItemType(StorageItem Item) => Item.Kind == StorageItemKind.Folder ? "Folder" : "File";
    static DateTime? UtcDateTime(DateTimeOffset Value)
    {
        return Value == default ? null : Value.UtcDateTime;
    }
    static DateTime? UtcDateTime(DateTimeOffset? Value)
    {
        return Value.HasValue ? Value.Value.UtcDateTime : null;
    }

    // ● public

    /// <summary>
    /// Maps an existing storage item to a remote observation record.
    /// </summary>
    static public RemoteObservedSnapshotRecord FromStorageItem(StorageItem Item, string TrackedItemId, DateTime ObservedTime, DateTimeOffset? ProviderChangeTime = null)
    {
        Guard.NotNull(Item, nameof(Item));
        Guard.NotNullOrWhiteSpace(TrackedItemId, nameof(TrackedItemId));

        return new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = TrackedItemId,
            RemoteItemId = Item.Id,
            ExistsFlag = true,
            Removed = false,
            Name = Item.Name,
            RemoteParentId = Item.ParentId,
            ItemType = ItemType(Item),
            MimeType = Item.MimeType,
            Size = Item.IsFolder ? null : Item.Size,
            ContentHash = Item.Md5Hash,
            CreatedTime = UtcDateTime(Item.CreatedTime),
            ModifiedTime = UtcDateTime(Item.ModifiedTime),
            ProviderVersion = Item.Version,
            Trashed = Item.Trashed,
            ProviderChangeTime = UtcDateTime(ProviderChangeTime),
            ObservedTime = ObservedTime,
        };
    }
    /// <summary>
    /// Maps a provider change to a remote observation record.
    /// </summary>
    static public RemoteObservedSnapshotRecord FromChange(StorageChange Change, string TrackedItemId, DateTime ObservedTime)
    {
        Guard.NotNull(Change, nameof(Change));
        Guard.NotNullOrWhiteSpace(TrackedItemId, nameof(TrackedItemId));

        if (Change.Item != null)
            return FromStorageItem(Change.Item, TrackedItemId, ObservedTime, Change.Time);

        return new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = TrackedItemId,
            RemoteItemId = Change.ItemId,
            ExistsFlag = false,
            Removed = Change.Removed,
            ProviderChangeTime = UtcDateTime(Change.Time),
            ObservedTime = ObservedTime,
        };
    }
}
