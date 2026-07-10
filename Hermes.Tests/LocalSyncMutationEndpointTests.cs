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
