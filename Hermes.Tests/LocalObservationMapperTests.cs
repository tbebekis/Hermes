// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests local observation mapping.
/// </summary>
public class LocalObservationMapperTests
{
    // ● public

    /// <summary>
    /// Verifies local scan items map to existing local observations.
    /// </summary>
    [Fact]
    public void FromScanItemMapsExistingObservation()
    {
        DateTime ModifiedTime = new(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc);
        DateTime ObservedTime = new(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc);
        LocalScanItem Item = new()
        {
            RelativePath = "Folder/File.txt",
            Name = "File.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 5,
            ModifiedTime = ModifiedTime,
            ContentHash = "hash-1",
        };

        LocalObservedSnapshotRecord Record = LocalObservationMapper.FromScanItem(Item, "item-1", ObservedTime, "scan-1");

        Assert.Equal("item-1", Record.TrackedItemId);
        Assert.True(Record.ExistsFlag);
        Assert.Equal("Folder/File.txt", Record.RelativePath);
        Assert.Equal("File.txt", Record.Name);
        Assert.Equal("Folder", Record.ParentRelativePath);
        Assert.Equal("File", Record.ItemType);
        Assert.Equal(5, Record.Size);
        Assert.Equal(ModifiedTime, Record.ModifiedTime);
        Assert.Equal("hash-1", Record.ContentHash);
        Assert.Equal(ObservedTime, Record.ObservedTime);
        Assert.Equal("scan-1", Record.ScanId);
    }
    /// <summary>
    /// Verifies missing observations contain no file metadata.
    /// </summary>
    [Fact]
    public void MissingCreatesMissingObservation()
    {
        DateTime ObservedTime = new(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc);

        LocalObservedSnapshotRecord Record = LocalObservationMapper.Missing("item-1", ObservedTime, "scan-1");

        Assert.Equal("item-1", Record.TrackedItemId);
        Assert.False(Record.ExistsFlag);
        Assert.Null(Record.RelativePath);
        Assert.Null(Record.Name);
        Assert.Equal(ObservedTime, Record.ObservedTime);
        Assert.Equal("scan-1", Record.ScanId);
    }
}
