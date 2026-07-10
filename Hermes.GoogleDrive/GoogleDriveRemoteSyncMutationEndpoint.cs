// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Google Drive implementation of remote synchronization mutations.
/// </summary>
public class GoogleDriveRemoteSyncMutationEndpoint : IRemoteSyncMutationEndpoint
{
    // ● fields

    readonly GoogleDriveClient fClient;

    // ● private

    async Task AuthenticateAsync(CancellationToken CancellationToken)
    {
        await fClient.AuthenticateAsync(CancellationToken);
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleDriveRemoteSyncMutationEndpoint"/> class.
    /// </summary>
    public GoogleDriveRemoteSyncMutationEndpoint(GoogleDriveClient Client)
    {
        fClient = Guard.NotNull(Client, nameof(Client));
    }

    // ● public

    /// <inheritdoc/>
    public async Task<StorageResult<StorageItem>> CreateFolderAsync(string Name, string ParentId, CancellationToken CancellationToken)
    {
        return await GoogleDriveStorageRunner.RunAsync("CreateFolder", async () =>
        {
            await AuthenticateAsync(CancellationToken);
            return await fClient.CreateFolderAsync(Name, ParentId, CancellationToken);
        });
    }
    /// <inheritdoc/>
    public async Task<StorageResult<StorageItem>> UploadFileAsync(string LocalFilePath, string ParentId, CancellationToken CancellationToken)
    {
        return await GoogleDriveStorageRunner.RunAsync("UploadFile", async () =>
        {
            await AuthenticateAsync(CancellationToken);
            return await fClient.UploadFileAsync(LocalFilePath, ParentId, CancellationToken);
        });
    }
    /// <inheritdoc/>
    public async Task<StorageResult<StorageItem>> UpdateFileContentAsync(string RemoteItemId, string LocalFilePath, CancellationToken CancellationToken)
    {
        return await GoogleDriveStorageRunner.RunAsync("UpdateFileContent", RemoteItemId, async () =>
        {
            await AuthenticateAsync(CancellationToken);
            return await fClient.UpdateFileContentAsync(RemoteItemId, LocalFilePath, CancellationToken);
        });
    }
    /// <inheritdoc/>
    public async Task<StorageResult<StorageItem>> DownloadFileAsync(string RemoteItemId, string LocalFilePath, CancellationToken CancellationToken)
    {
        return await GoogleDriveStorageRunner.RunAsync("DownloadFile", RemoteItemId, async () =>
        {
            await AuthenticateAsync(CancellationToken);
            return await fClient.DownloadFileAsync(RemoteItemId, LocalFilePath, CancellationToken);
        });
    }
    /// <inheritdoc/>
    public async Task<StorageResult<StorageItem>> DeleteItemAsync(string RemoteItemId, CancellationToken CancellationToken)
    {
        return await GoogleDriveStorageRunner.RunAsync("DeleteItem", RemoteItemId, async () =>
        {
            await AuthenticateAsync(CancellationToken);
            return await fClient.TrashFileAsync(RemoteItemId, CancellationToken);
        });
    }
}
