// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Google Drive implementation of <see cref="IStorageProvider"/>.
/// </summary>
public class GoogleDriveStorageProvider : IStorageProvider
{
    // ● private

    readonly GoogleDriveClient fClient;
    StorageError MapNotFound(GoogleDriveNotFoundException Ex, string OperationName)
    {
        return new StorageError(
            StorageErrorKind.NotFound,
            Ex.Message,
            false,
            false,
            TimeSpan.Zero,
            "Google Drive",
            string.Empty,
            HttpStatusCode.NotFound.ToString(),
            OperationName,
            Ex.FileId,
            string.Empty,
            Ex);
    }
    async Task<StorageResult<T>> RunAsync<T>(string OperationName, Func<Task<T>> Operation)
    {
        try
        {
            T Value = await Operation();
            return StorageResult<T>.Success(Value);
        }
        catch (GoogleDriveNotFoundException Ex)
        {
            return StorageResult<T>.Failure(MapNotFound(Ex, OperationName));
        }
        catch (GoogleApiException Ex)
        {
            return StorageResult<T>.Failure(GoogleDriveErrorMapper.Map(Ex, OperationName));
        }
    }
    async Task<StorageResult<T>> RunAsync<T>(string OperationName, string ItemId, Func<Task<T>> Operation)
    {
        try
        {
            T Value = await Operation();
            return StorageResult<T>.Success(Value);
        }
        catch (GoogleDriveNotFoundException Ex)
        {
            return StorageResult<T>.Failure(MapNotFound(Ex, OperationName));
        }
        catch (GoogleApiException Ex)
        {
            return StorageResult<T>.Failure(GoogleDriveErrorMapper.Map(Ex, OperationName, ItemId));
        }
    }
    async Task<StorageResult<T>> RunCheckpointAsync<T>(string OperationName, string Checkpoint, Func<Task<T>> Operation)
    {
        try
        {
            T Value = await Operation();
            return StorageResult<T>.Success(Value);
        }
        catch (GoogleApiException Ex)
        {
            return StorageResult<T>.Failure(GoogleDriveErrorMapper.Map(Ex, OperationName, string.Empty, Checkpoint));
        }
    }

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
        return await RunAsync("GetStartPageToken", async () =>
        {
            await fClient.AuthenticateAsync(CancellationToken);
            return await fClient.GetStartPageTokenAsync(CancellationToken);
        });
    }
    /// <inheritdoc/>
    public async Task<StorageResult<StorageItem>> GetItemAsync(string ItemId, CancellationToken CancellationToken)
    {
        return await RunAsync("GetItem", ItemId, async () =>
        {
            await fClient.AuthenticateAsync(CancellationToken);
            return await fClient.GetFileAsync(ItemId, CancellationToken);
        });
    }
    /// <inheritdoc/>
    public async Task<StorageResult<IReadOnlyList<StorageItem>>> ListFolderAsync(string FolderId, CancellationToken CancellationToken)
    {
        return await RunAsync("ListFolder", FolderId, async () =>
        {
            await fClient.AuthenticateAsync(CancellationToken);
            return await fClient.ListFolderAsync(FolderId, CancellationToken);
        });
    }
    /// <inheritdoc/>
    public async Task<StorageResult<StorageChangeListResult>> ListChangesAsync(string PageToken, CancellationToken CancellationToken)
    {
        return await RunCheckpointAsync("ListChanges", PageToken, async () =>
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
