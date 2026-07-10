// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests base snapshot mapping.
/// </summary>
public class BaseSnapshotMapperTests
{
    // ● public

    /// <summary>
    /// Verifies existing local and remote observations map to an existing base snapshot.
    /// </summary>
    [Fact]
    public void ExistingObservationsMapToExistingBaseSnapshot()
    {
        DateTime Time = new(2026, 7, 11, 5, 0, 0, DateTimeKind.Utc);

        BaseSnapshotRecord Record = BaseSnapshotMapper.FromVerifiedObservations(
            new LocalObservedSnapshotRecord()
            {
                TrackedItemId = "item-1",
                ExistsFlag = true,
                RelativePath = "Folder/File.txt",
                Name = "File.txt",
                ItemType = "File",
                Size = 42,
                ContentHash = "hash-local",
                ObservedTime = Time,
            },
            new RemoteObservedSnapshotRecord()
            {
                TrackedItemId = "item-1",
                RemoteItemId = "remote-1",
                ExistsFlag = true,
                Removed = false,
                Name = "File.txt",
                RemoteParentId = "remote-folder",
                ItemType = "File",
                Size = 42,
                ContentHash = "hash-remote",
                ProviderVersion = 7,
                Trashed = false,
                ObservedTime = Time,
            },
            Time);

        Assert.True(Record.ExistsFlag);
        Assert.Equal("item-1", Record.TrackedItemId);
        Assert.Equal("Folder/File.txt", Record.LocalRelativePath);
        Assert.Equal("remote-folder", Record.RemoteParentId);
        Assert.Equal("hash-remote", Record.ContentHash);
        Assert.Equal(7, Record.ProviderVersion);
        Assert.Equal(Time, Record.CommittedTime);
    }
    /// <summary>
    /// Verifies missing observations map to a missing base snapshot.
    /// </summary>
    [Fact]
    public void MissingObservationsMapToMissingBaseSnapshot()
    {
        DateTime Time = new(2026, 7, 11, 5, 30, 0, DateTimeKind.Utc);

        BaseSnapshotRecord Record = BaseSnapshotMapper.FromVerifiedObservations(
            new LocalObservedSnapshotRecord()
            {
                TrackedItemId = "item-1",
                ExistsFlag = false,
                ObservedTime = Time,
            },
            new RemoteObservedSnapshotRecord()
            {
                TrackedItemId = "item-1",
                RemoteItemId = "remote-1",
                ExistsFlag = false,
                Removed = true,
                ObservedTime = Time,
            },
            Time);

        Assert.False(Record.ExistsFlag);
        Assert.Equal("item-1", Record.TrackedItemId);
        Assert.Null(Record.Name);
        Assert.Equal(Time, Record.CommittedTime);
    }
}
