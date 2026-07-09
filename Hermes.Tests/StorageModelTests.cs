// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests storage model creation.
/// </summary>
public class StorageModelTests
{
    // ● public

    /// <summary>
    /// Verifies that storage files expose file metadata.
    /// </summary>
    [Fact]
    public void StorageFileStoresMetadata()
    {
        StorageFile File = new("id", "parent", "Report.docx", "/Report.docx", 128, "checksum");

        Assert.Equal("id", File.Id);
        Assert.Equal("parent", File.ParentId);
        Assert.Equal("Report.docx", File.Name);
        Assert.Equal("/Report.docx", File.Path);
        Assert.Equal(StorageItemKind.File, File.Kind);
        Assert.Equal(128, File.Size);
        Assert.Equal("checksum", File.Checksum);
    }

    /// <summary>
    /// Verifies that storage folders expose folder metadata.
    /// </summary>
    [Fact]
    public void StorageFolderStoresMetadata()
    {
        StorageFolder Folder = new("id", "parent", "Docs", "/Docs");

        Assert.Equal("id", Folder.Id);
        Assert.Equal("parent", Folder.ParentId);
        Assert.Equal("Docs", Folder.Name);
        Assert.Equal("/Docs", Folder.Path);
        Assert.Equal(StorageItemKind.Folder, Folder.Kind);
    }
}
