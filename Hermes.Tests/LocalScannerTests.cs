// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests local filesystem scanning.
/// </summary>
public class LocalScannerTests
{
    // ● private

    /// <summary>
    /// Provides a temporary folder for scanner tests.
    /// </summary>
    sealed class TempFolder : IDisposable
    {
        // ● private

        bool fDisposed;

        // ● constructor

        public TempFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hermes-local-scanner-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        // ● public

        public void Dispose()
        {
            if (fDisposed)
                return;

            if (Directory.Exists(Path))
                Directory.Delete(Path, true);

            fDisposed = true;
        }

        // ● properties

        public string Path { get; }
    }
    static LocalScanItem Find(IReadOnlyList<LocalScanItem> Items, string RelativePath)
    {
        return Items.First(Item => Item.RelativePath == RelativePath);
    }

    // ● public

    /// <summary>
    /// Verifies local scanner returns folders and files with normalized metadata.
    /// </summary>
    [Fact]
    public async Task ScanAsyncReturnsLocalItems()
    {
        using TempFolder Folder = new();
        string NestedFolder = System.IO.Path.Combine(Folder.Path, "FolderA");
        string NestedFile = System.IO.Path.Combine(NestedFolder, "File.txt");
        LocalScanner Scanner = new();

        Directory.CreateDirectory(NestedFolder);
        await System.IO.File.WriteAllTextAsync(NestedFile, "hello");

        Result<IReadOnlyList<LocalScanItem>> Result = await Scanner.ScanAsync(Folder.Path, CancellationToken.None);

        Assert.True(Result.Succeeded);
        Assert.Equal(2, Result.Value.Count);

        LocalScanItem FolderItem = Find(Result.Value, "FolderA");
        Assert.Equal("Folder", FolderItem.ItemType);
        Assert.Equal("FolderA", FolderItem.Name);
        Assert.Null(FolderItem.ParentRelativePath);
        Assert.Null(FolderItem.Size);
        Assert.Null(FolderItem.ContentHash);

        LocalScanItem FileItem = Find(Result.Value, "FolderA/File.txt");
        Assert.Equal("File", FileItem.ItemType);
        Assert.Equal("File.txt", FileItem.Name);
        Assert.Equal("FolderA", FileItem.ParentRelativePath);
        Assert.Equal(5, FileItem.Size);
        Assert.Equal("5d41402abc4b2a76b9719d911017c592", FileItem.ContentHash);
    }
    /// <summary>
    /// Verifies local scanner reports missing root failure.
    /// </summary>
    [Fact]
    public async Task ScanAsyncFailsWhenRootDoesNotExist()
    {
        LocalScanner Scanner = new();
        string MissingPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hermes-missing-root", Guid.NewGuid().ToString("N"));

        Result<IReadOnlyList<LocalScanItem>> Result = await Scanner.ScanAsync(MissingPath, CancellationToken.None);

        Assert.True(Result.Failed);
        Assert.Contains("does not exist", Result.ErrorText);
    }
}
