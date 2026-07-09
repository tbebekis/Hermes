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
    public async Task<Result<string>> GetStartPageTokenAsync(CancellationToken CancellationToken)
    {
        await fClient.AuthenticateAsync(CancellationToken);
        string Token = await fClient.GetStartPageTokenAsync(CancellationToken);
        return Result<string>.Success(Token);
    }
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<StorageItem>>> ListFilesAsync(string FolderId, CancellationToken CancellationToken)
    {
        await fClient.AuthenticateAsync(CancellationToken);
        IReadOnlyList<StorageItem> Items = await fClient.ListFilesAsync(CancellationToken);
        return Result<IReadOnlyList<StorageItem>>.Success(Items);
    }
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<StorageChange>>> ListChangesAsync(string PageToken, CancellationToken CancellationToken)
    {
        await fClient.AuthenticateAsync(CancellationToken);
        IReadOnlyList<StorageChange> Changes = await fClient.ListChangesAsync(PageToken, CancellationToken);
        return Result<IReadOnlyList<StorageChange>>.Success(Changes);
    }

    // ● properties

    /// <inheritdoc/>
    public string Name => "Google Drive";
    /// <inheritdoc/>
    public StorageProviderCapabilities Capabilities { get; }
}
