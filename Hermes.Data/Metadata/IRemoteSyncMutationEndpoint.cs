// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Defines remote storage mutations required by synchronization execution.
/// </summary>
public interface IRemoteSyncMutationEndpoint
{
    // ● public

    /// <summary>
    /// Creates a remote folder.
    /// </summary>
    Task<StorageResult<StorageItem>> CreateFolderAsync(string Name, string ParentId, CancellationToken CancellationToken);

    /// <summary>
    /// Uploads a local file as a new remote file.
    /// </summary>
    Task<StorageResult<StorageItem>> UploadFileAsync(string LocalFilePath, string ParentId, CancellationToken CancellationToken);

    /// <summary>
    /// Replaces remote file content from a local file.
    /// </summary>
    Task<StorageResult<StorageItem>> UpdateFileContentAsync(string RemoteItemId, string LocalFilePath, CancellationToken CancellationToken);

    /// <summary>
    /// Downloads remote file content to a local file path.
    /// </summary>
    Task<StorageResult<StorageItem>> DownloadFileAsync(string RemoteItemId, string LocalFilePath, CancellationToken CancellationToken);

    /// <summary>
    /// Moves a remote item to trash or removes it according to provider policy.
    /// </summary>
    Task<StorageResult<StorageItem>> DeleteItemAsync(string RemoteItemId, CancellationToken CancellationToken);
}
