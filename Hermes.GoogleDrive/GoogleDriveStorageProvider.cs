// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Google Drive implementation of <see cref="IStorageProvider"/>.
/// </summary>
public class GoogleDriveStorageProvider : IStorageProvider
{
    // ● private

    private readonly GoogleDriveClient fClient;

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
        string Token = await fClient.GetStartPageTokenAsync(CancellationToken);
        return Result<string>.Success(Token);
    }

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<StorageItem>>> ListFilesAsync(string FolderId, CancellationToken CancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<StorageChange>>> ListChangesAsync(string PageToken, CancellationToken CancellationToken)
    {
        throw new NotImplementedException();
    }

    // ● properties

    /// <inheritdoc/>
    public string Name => "Google Drive";

    /// <inheritdoc/>
    public StorageProviderCapabilities Capabilities { get; }
}
