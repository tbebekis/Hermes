// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests local synchronization mutation endpoint behavior.
/// </summary>
public class LocalSyncMutationEndpointTests
{
    // ● private

    /// <summary>
    /// Provides a temporary folder for local mutation endpoint tests.
    /// </summary>
    sealed class TempFolder : IDisposable
    {
        // ● fields

        bool fDisposed;

        // ● constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TempFolder"/> class.
        /// </summary>
        public TempFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hermes-local-mutation-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        // ● public

        /// <summary>
        /// Deletes the temporary folder.
        /// </summary>
        public void Dispose()
        {
            if (fDisposed)
                return;

            if (Directory.Exists(Path))
                Directory.Delete(Path, true);

            fDisposed = true;
        }

        // ● properties

        /// <summary>
        /// Gets the temporary folder path.
        /// </summary>
        public string Path { get; }
    }

    // ● public

    /// <summary>
    /// Verifies relative paths resolve inside the local root.
    /// </summary>
    [Fact]
    public void ResolvePathReturnsPathInsideRoot()
    {
        using TempFolder Folder = new();
        LocalSyncMutationEndpoint Endpoint = new(Folder.Path);

        string FullPath = Endpoint.ResolvePath("Folder/File.txt");

        Assert.Equal(System.IO.Path.Combine(Folder.Path, "Folder", "File.txt"), FullPath);
    }

    /// <summary>
    /// Verifies rooted paths are rejected.
    /// </summary>
    [Fact]
    public void ResolvePathRejectsRootedPath()
    {
        using TempFolder Folder = new();
        LocalSyncMutationEndpoint Endpoint = new(Folder.Path);

        Assert.Throws<ArgumentException>(() => Endpoint.ResolvePath("/tmp/File.txt"));
    }

    /// <summary>
    /// Verifies parent directory creation.
    /// </summary>
    [Fact]
    public async Task EnsureParentDirectoryAsyncCreatesParentDirectory()
    {
        using TempFolder Folder = new();
        LocalSyncMutationEndpoint Endpoint = new(Folder.Path);

        Result Result = await Endpoint.EnsureParentDirectoryAsync("Folder/File.txt", CancellationToken.None);

        Assert.True(Result.Succeeded);
        Assert.True(Directory.Exists(System.IO.Path.Combine(Folder.Path, "Folder")));
    }

    /// <summary>
    /// Verifies directory creation.
    /// </summary>
    [Fact]
    public async Task CreateDirectoryAsyncCreatesDirectory()
    {
        using TempFolder Folder = new();
        LocalSyncMutationEndpoint Endpoint = new(Folder.Path);

        Result Result = await Endpoint.CreateDirectoryAsync("Folder/Subfolder", CancellationToken.None);

        Assert.True(Result.Succeeded);
        Assert.True(Directory.Exists(System.IO.Path.Combine(Folder.Path, "Folder", "Subfolder")));
    }

    /// <summary>
    /// Verifies local item deletion.
    /// </summary>
    [Fact]
    public async Task DeleteItemAsyncDeletesFile()
    {
        using TempFolder Folder = new();
        LocalSyncMutationEndpoint Endpoint = new(Folder.Path);
        string FilePath = System.IO.Path.Combine(Folder.Path, "File.txt");
        await System.IO.File.WriteAllTextAsync(FilePath, "content");

        Result Result = await Endpoint.DeleteItemAsync("File.txt", CancellationToken.None);

        Assert.True(Result.Succeeded);
        Assert.False(System.IO.File.Exists(FilePath));
    }
    /// <summary>
    /// Verifies local directory deletion is recursive.
    /// </summary>
    [Fact]
    public async Task DeleteItemAsyncDeletesDirectoryRecursively()
    {
        using TempFolder Folder = new();
        LocalSyncMutationEndpoint Endpoint = new(Folder.Path);
        string DirectoryPath = System.IO.Path.Combine(Folder.Path, "Folder");
        string FilePath = System.IO.Path.Combine(DirectoryPath, "File.txt");
        Directory.CreateDirectory(DirectoryPath);
        await System.IO.File.WriteAllTextAsync(FilePath, "content");

        Result Result = await Endpoint.DeleteItemAsync("Folder", CancellationToken.None);

        Assert.True(Result.Succeeded);
        Assert.False(Directory.Exists(DirectoryPath));
    }
    /// <summary>
    /// Verifies local file moves create the target parent directory.
    /// </summary>
    [Fact]
    public async Task MoveFileAsyncMovesFile()
    {
        using TempFolder Folder = new();
        LocalSyncMutationEndpoint Endpoint = new(Folder.Path);
        string SourcePath = System.IO.Path.Combine(Folder.Path, "File.txt");
        string TargetPath = System.IO.Path.Combine(Folder.Path, "Folder", "Moved.txt");
        await System.IO.File.WriteAllTextAsync(SourcePath, "content");

        Result Result = await Endpoint.MoveFileAsync("File.txt", "Folder/Moved.txt", CancellationToken.None);

        Assert.True(Result.Succeeded);
        Assert.False(System.IO.File.Exists(SourcePath));
        Assert.True(System.IO.File.Exists(TargetPath));
        Assert.Equal("content", await System.IO.File.ReadAllTextAsync(TargetPath));
    }
    /// <summary>
    /// Verifies local directory moves create the target parent directory.
    /// </summary>
    [Fact]
    public async Task MoveDirectoryAsyncMovesDirectory()
    {
        using TempFolder Folder = new();
        LocalSyncMutationEndpoint Endpoint = new(Folder.Path);
        string SourcePath = System.IO.Path.Combine(Folder.Path, "Folder");
        string NestedFilePath = System.IO.Path.Combine(SourcePath, "File.txt");
        string TargetPath = System.IO.Path.Combine(Folder.Path, "Target", "Folder");
        Directory.CreateDirectory(SourcePath);
        await System.IO.File.WriteAllTextAsync(NestedFilePath, "content");

        Result Result = await Endpoint.MoveDirectoryAsync("Folder", "Target/Folder", CancellationToken.None);

        Assert.True(Result.Succeeded);
        Assert.False(Directory.Exists(SourcePath));
        Assert.True(Directory.Exists(TargetPath));
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(TargetPath, "File.txt")));
    }
    /// <summary>
    /// Verifies cancelled local mutations do not execute.
    /// </summary>
    [Fact]
    public async Task CreateDirectoryAsyncHonorsCancellation()
    {
        using TempFolder Folder = new();
        LocalSyncMutationEndpoint Endpoint = new(Folder.Path);
        using CancellationTokenSource Cancellation = new();
        Cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => Endpoint.CreateDirectoryAsync("Folder", Cancellation.Token));

        Assert.False(Directory.Exists(System.IO.Path.Combine(Folder.Path, "Folder")));
    }

    /// <summary>
    /// Verifies escaped relative paths fail as mutation results.
    /// </summary>
    [Fact]
    public async Task MutationRejectsEscapedPath()
    {
        using TempFolder Folder = new();
        LocalSyncMutationEndpoint Endpoint = new(Folder.Path);

        Result Result = await Endpoint.CreateDirectoryAsync("../Outside", CancellationToken.None);

        Assert.True(Result.Failed);
        Assert.Contains("inside the local root", Result.ErrorText);
    }
}
