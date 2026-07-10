// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Google Drive implementation of <see cref="IStorageProvider"/>.
/// </summary>
public class GoogleDriveStorageProvider : IStorageProvider
{
    // ● fields

    readonly GoogleDriveClient fClient;

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleDriveStorageProvider"/> class.
    /// </summary>
    public GoogleDriveStorageProvider(GoogleDriveClient Client)
    {
        fClient = Guard.NotNull(Client, nameof(Client));
        Capabilities = new StorageProviderCapabilities
        {
            SupportsChangeTokens = true,
            SupportsResumableUpload = true,
            SupportsTrash = true
        };
    }

    // ● public

    /// <inheritdoc/>
    public async Task<StorageResult<string>> GetStartPageTokenAsync(CancellationToken CancellationToken)
    {
        return await GoogleDriveStorageRunner.RunAsync("GetStartPageToken", async () =>
        {
            await fClient.AuthenticateAsync(CancellationToken);
            return await fClient.GetStartPageTokenAsync(CancellationToken);
        });
    }
    /// <inheritdoc/>
    public async Task<StorageResult<StorageItem>> GetItemAsync(string ItemId, CancellationToken CancellationToken)
    {
        return await GoogleDriveStorageRunner.RunAsync("GetItem", ItemId, async () =>
        {
            await fClient.AuthenticateAsync(CancellationToken);
            return await fClient.GetFileAsync(ItemId, CancellationToken);
        });
    }
    /// <inheritdoc/>
    public async Task<StorageResult<IReadOnlyList<StorageItem>>> ListFolderAsync(string FolderId, CancellationToken CancellationToken)
    {
        return await GoogleDriveStorageRunner.RunAsync("ListFolder", FolderId, async () =>
        {
            await fClient.AuthenticateAsync(CancellationToken);
            return await fClient.ListFolderAsync(FolderId, CancellationToken);
        });
    }
    /// <inheritdoc/>
    public async Task<StorageResult<StorageChangeListResult>> ListChangesAsync(string PageToken, CancellationToken CancellationToken)
    {
        return await GoogleDriveStorageRunner.RunCheckpointAsync("ListChanges", PageToken, async () =>
        {
            await fClient.AuthenticateAsync(CancellationToken);
            return await fClient.ListChangesAsync(PageToken, CancellationToken);
        });
    }

    // ● properties

    /// <inheritdoc/>
    public string Name => "Google Drive";
    /// <inheritdoc/>
    public StorageProviderCapabilities Capabilities { get; }
}
