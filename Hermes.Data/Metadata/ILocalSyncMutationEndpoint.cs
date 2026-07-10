// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Defines local filesystem mutations required by synchronization execution.
/// </summary>
public interface ILocalSyncMutationEndpoint
{
    // ● public

    /// <summary>
    /// Resolves a local relative path to an absolute local path.
    /// </summary>
    string ResolvePath(string LocalRelativePath);

    /// <summary>
    /// Creates the parent directory required by a local relative path.
    /// </summary>
    Task<Result> EnsureParentDirectoryAsync(string LocalRelativePath, CancellationToken CancellationToken);

    /// <summary>
    /// Creates a local directory.
    /// </summary>
    Task<Result> CreateDirectoryAsync(string LocalRelativePath, CancellationToken CancellationToken);

    /// <summary>
    /// Deletes a local file or directory.
    /// </summary>
    Task<Result> DeleteItemAsync(string LocalRelativePath, CancellationToken CancellationToken);
}
