// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Maps local scan items to local observation records.
/// </summary>
static public class LocalObservationMapper
{
    // ● public

    /// <summary>
    /// Maps a local scan item to a local observation record.
    /// </summary>
    static public LocalObservedSnapshotRecord FromScanItem(LocalScanItem Item, string TrackedItemId, DateTime ObservedTime, string ScanId)
    {
        Guard.NotNull(Item, nameof(Item));
        Guard.NotNullOrWhiteSpace(TrackedItemId, nameof(TrackedItemId));

        return new LocalObservedSnapshotRecord()
        {
            TrackedItemId = TrackedItemId,
            ExistsFlag = true,
            RelativePath = Item.RelativePath,
            Name = Item.Name,
            ParentRelativePath = Item.ParentRelativePath,
            ItemType = Item.ItemType,
            Size = Item.Size,
            ModifiedTime = Item.ModifiedTime,
            ContentHash = Item.ContentHash,
            ObservedTime = ObservedTime,
            ScanId = ScanId,
        };
    }
    /// <summary>
    /// Creates a missing local observation record.
    /// </summary>
    static public LocalObservedSnapshotRecord Missing(string TrackedItemId, DateTime ObservedTime, string ScanId)
    {
        Guard.NotNullOrWhiteSpace(TrackedItemId, nameof(TrackedItemId));

        return new LocalObservedSnapshotRecord()
        {
            TrackedItemId = TrackedItemId,
            ExistsFlag = false,
            ObservedTime = ObservedTime,
            ScanId = ScanId,
        };
    }
}
