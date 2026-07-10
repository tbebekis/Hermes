// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests Google Drive model mapping.
/// </summary>
public class GoogleDriveMapperTests
{
    // ● public

    /// <summary>
    /// Verifies that a Google Drive change with a file maps to current item state.
    /// </summary>
    [Fact]
    public void MapChangeMapsCurrentItemState()
    {
        GoogleDriveMapper Mapper = new();
        DateTimeOffset Time = new(2026, 7, 10, 8, 30, 0, TimeSpan.Zero);
        Change Change = new()
        {
            FileId = "file-id",
            Removed = false,
            TimeDateTimeOffset = Time,
            File = new DriveFile
            {
                Id = "file-id",
                Name = "remote.txt",
                MimeType = "text/plain",
                Parents = new List<string> { "parent-id" },
                Size = 12,
                Md5Checksum = "hash",
                Version = 3,
                Trashed = false
            }
        };

        StorageChange Result = Mapper.MapChange(Change);

        Assert.Equal("file-id", Result.ItemId);
        Assert.False(Result.Removed);
        Assert.Equal(Time, Result.Time);
        Assert.NotNull(Result.Item);
        Assert.Equal("remote.txt", Result.Item.Name);
        Assert.Equal("parent-id", Result.Item.ParentId);
        Assert.Equal(12, Result.Item.Size);
        Assert.Equal("hash", Result.Item.Md5Hash);
        Assert.Equal(3, Result.Item.Version);
    }

    /// <summary>
    /// Verifies that a removed Google Drive change maps to a tombstone.
    /// </summary>
    [Fact]
    public void MapChangeMapsPermanentDeleteTombstone()
    {
        GoogleDriveMapper Mapper = new();
        DateTimeOffset Time = new(2026, 7, 10, 8, 45, 0, TimeSpan.Zero);
        Change Change = new()
        {
            FileId = "deleted-file-id",
            Removed = true,
            TimeDateTimeOffset = Time,
            File = null
        };

        StorageChange Result = Mapper.MapChange(Change);

        Assert.Equal("deleted-file-id", Result.ItemId);
        Assert.True(Result.Removed);
        Assert.Equal(Time, Result.Time);
        Assert.Null(Result.Item);
    }

    /// <summary>
    /// Verifies that a Google Drive change can use the file id when the change id is missing.
    /// </summary>
    [Fact]
    public void MapChangeUsesFileIdWhenChangeFileIdIsMissing()
    {
        GoogleDriveMapper Mapper = new();
        Change Change = new()
        {
            FileId = null,
            Removed = false,
            File = new DriveFile
            {
                Id = "mapped-file-id",
                Name = "remote.txt",
                MimeType = "text/plain"
            }
        };

        StorageChange Result = Mapper.MapChange(Change);

        Assert.Equal("mapped-file-id", Result.ItemId);
        Assert.NotNull(Result.Item);
        Assert.Equal("mapped-file-id", Result.Item.Id);
    }
}
