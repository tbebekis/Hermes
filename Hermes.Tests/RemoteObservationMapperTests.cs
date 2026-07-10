// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests remote observation mapping.
/// </summary>
public class RemoteObservationMapperTests
{
    // ● public

    /// <summary>
    /// Verifies storage item mapping to existing remote observation.
    /// </summary>
    [Fact]
    public void FromStorageItemMapsExistingObservation()
    {
        DateTimeOffset CreatedTime = new(2026, 7, 10, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset ModifiedTime = new(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);
        DateTimeOffset ChangeTime = new(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        DateTime ObservedTime = new(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc);
        StorageItem Item = new(
            "remote-1",
            "remote-parent",
            "Report.txt",
            "/Report.txt",
            StorageItemKind.File,
            "text/plain",
            42,
            "hash-1",
            CreatedTime,
            ModifiedTime,
            7,
            false);

        RemoteObservedSnapshotRecord Record = RemoteObservationMapper.FromStorageItem(Item, "item-1", ObservedTime, ChangeTime);

        Assert.Equal("item-1", Record.TrackedItemId);
        Assert.Equal("remote-1", Record.RemoteItemId);
        Assert.True(Record.ExistsFlag);
        Assert.False(Record.Removed);
        Assert.Equal("Report.txt", Record.Name);
        Assert.Equal("remote-parent", Record.RemoteParentId);
        Assert.Equal("File", Record.ItemType);
        Assert.Equal("text/plain", Record.MimeType);
        Assert.Equal(42, Record.Size);
        Assert.Equal("hash-1", Record.ContentHash);
        Assert.Equal(7, Record.ProviderVersion);
        Assert.False(Record.Trashed);
        Assert.Equal(ChangeTime.UtcDateTime, Record.ProviderChangeTime);
        Assert.Equal(ObservedTime, Record.ObservedTime);
    }
    /// <summary>
    /// Verifies storage change tombstone mapping.
    /// </summary>
    [Fact]
    public void FromChangeMapsPermanentDeleteTombstone()
    {
        DateTimeOffset ChangeTime = new(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        DateTime ObservedTime = new(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc);
        StorageChange Change = new("remote-1", true, ChangeTime, null);

        RemoteObservedSnapshotRecord Record = RemoteObservationMapper.FromChange(Change, "item-1", ObservedTime);

        Assert.Equal("item-1", Record.TrackedItemId);
        Assert.Equal("remote-1", Record.RemoteItemId);
        Assert.False(Record.ExistsFlag);
        Assert.True(Record.Removed);
        Assert.Null(Record.Name);
        Assert.Null(Record.RemoteParentId);
        Assert.Equal(ChangeTime.UtcDateTime, Record.ProviderChangeTime);
        Assert.Equal(ObservedTime, Record.ObservedTime);
    }
}
