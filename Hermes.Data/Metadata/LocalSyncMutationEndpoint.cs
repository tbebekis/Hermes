// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Local filesystem implementation of synchronization mutations.
/// </summary>
public class LocalSyncMutationEndpoint : ILocalSyncMutationEndpoint
{
    // ● fields

    readonly string fLocalRootPath;
    readonly string fLocalRootPrefix;

    // ● private

    static string NormalizeRelativePath(string LocalRelativePath)
    {
        return LocalRelativePath.Replace('/', Path.DirectorySeparatorChar);
    }
    static string RootPrefix(string LocalRootPath)
    {
        string Separator = Path.DirectorySeparatorChar.ToString();
        return LocalRootPath.EndsWith(Separator, StringComparison.Ordinal) ? LocalRootPath : LocalRootPath + Separator;
    }
    bool IsUnderRoot(string FullPath)
    {
        return string.Equals(FullPath, fLocalRootPath, StringComparison.Ordinal)
            || FullPath.StartsWith(fLocalRootPrefix, StringComparison.Ordinal);
    }
    Result RunMutation(string LocalRelativePath, Action<string> Mutation)
    {
        try
        {
            string FullPath = ResolvePath(LocalRelativePath);
            Mutation(FullPath);
            return Result.Success();
        }
        catch (Exception Ex)
        {
            return Result.Failure(Ex.Message);
        }
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalSyncMutationEndpoint"/> class.
    /// </summary>
    public LocalSyncMutationEndpoint(string LocalRootPath)
    {
        fLocalRootPath = Path.GetFullPath(Guard.NotNullOrWhiteSpace(LocalRootPath, nameof(LocalRootPath)));
        fLocalRootPrefix = RootPrefix(fLocalRootPath);
    }

    // ● public

    /// <inheritdoc/>
    public string ResolvePath(string LocalRelativePath)
    {
        Guard.NotNullOrWhiteSpace(LocalRelativePath, nameof(LocalRelativePath));

        if (Path.IsPathRooted(LocalRelativePath))
            throw new ArgumentException("Local relative path must not be rooted.", nameof(LocalRelativePath));

        string FullPath = Path.GetFullPath(Path.Combine(fLocalRootPath, NormalizeRelativePath(LocalRelativePath)));

        if (!IsUnderRoot(FullPath))
            throw new ArgumentException("Local relative path must stay inside the local root.", nameof(LocalRelativePath));

        return FullPath;
    }
    /// <inheritdoc/>
    public Task<Result> EnsureParentDirectoryAsync(string LocalRelativePath, CancellationToken CancellationToken)
    {
        CancellationToken.ThrowIfCancellationRequested();

        Result Result = RunMutation(LocalRelativePath, FullPath =>
        {
            string ParentPath = Path.GetDirectoryName(FullPath);
            if (!string.IsNullOrWhiteSpace(ParentPath))
                Directory.CreateDirectory(ParentPath);
        });

        return Task.FromResult(Result);
    }
    /// <inheritdoc/>
    public Task<Result> CreateDirectoryAsync(string LocalRelativePath, CancellationToken CancellationToken)
    {
        CancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(RunMutation(LocalRelativePath, FullPath => Directory.CreateDirectory(FullPath)));
    }
    /// <inheritdoc/>
    public Task<Result> DeleteItemAsync(string LocalRelativePath, CancellationToken CancellationToken)
    {
        CancellationToken.ThrowIfCancellationRequested();

        Result Result = RunMutation(LocalRelativePath, FullPath =>
        {
            if (File.Exists(FullPath))
                File.Delete(FullPath);
            else if (Directory.Exists(FullPath))
                Directory.Delete(FullPath, true);
        });

        return Task.FromResult(Result);
    }
    /// <inheritdoc/>
    public Task<Result> MoveFileAsync(string SourceRelativePath, string TargetRelativePath, CancellationToken CancellationToken)
    {
        CancellationToken.ThrowIfCancellationRequested();

        Result Result = RunMutation(SourceRelativePath, SourcePath =>
        {
            string TargetPath = ResolvePath(TargetRelativePath);
            string ParentPath = Path.GetDirectoryName(TargetPath);

            if (!File.Exists(SourcePath))
                throw new FileNotFoundException("Move source file was not found.", SourcePath);
            if (File.Exists(TargetPath) || Directory.Exists(TargetPath))
                throw new IOException("Move target already exists.");
            if (!string.IsNullOrWhiteSpace(ParentPath))
                Directory.CreateDirectory(ParentPath);

            File.Move(SourcePath, TargetPath);
        });

        return Task.FromResult(Result);
    }
    /// <inheritdoc/>
    public Task<Result> MoveDirectoryAsync(string SourceRelativePath, string TargetRelativePath, CancellationToken CancellationToken)
    {
        CancellationToken.ThrowIfCancellationRequested();

        Result Result = RunMutation(SourceRelativePath, SourcePath =>
        {
            string TargetPath = ResolvePath(TargetRelativePath);
            string ParentPath = Path.GetDirectoryName(TargetPath);

            if (!Directory.Exists(SourcePath))
                throw new DirectoryNotFoundException("Move source directory was not found.");
            if (File.Exists(TargetPath) || Directory.Exists(TargetPath))
                throw new IOException("Move target already exists.");
            if (!string.IsNullOrWhiteSpace(ParentPath))
                Directory.CreateDirectory(ParentPath);

            Directory.Move(SourcePath, TargetPath);
        });

        return Task.FromResult(Result);
    }
}
