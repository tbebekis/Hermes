// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Stores synchronization metadata.
/// </summary>
public class MetadataStore
{
    // ● private

    private string fDriveStartPageToken = string.Empty;

    // ● public

    /// <summary>
    /// Loads the stored remote cursor.
    /// </summary>
    public Task<string> LoadDriveStartPageTokenAsync(CancellationToken CancellationToken)
    {
        return Task.FromResult(fDriveStartPageToken);
    }

    /// <summary>
    /// Saves the remote cursor.
    /// </summary>
    public Task SaveDriveStartPageTokenAsync(string PageToken, CancellationToken CancellationToken)
    {
        fDriveStartPageToken = PageToken ?? string.Empty;
        return Task.CompletedTask;
    }
}
