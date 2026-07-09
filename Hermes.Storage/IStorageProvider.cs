// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Defines a cloud storage provider.
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the provider capabilities.
    /// </summary>
    StorageProviderCapabilities Capabilities { get; }

    /// <summary>
    /// Gets a remote sync cursor.
    /// </summary>
    Task<Result<string>> GetStartPageTokenAsync(CancellationToken CancellationToken);

    /// <summary>
    /// Lists items under the specified folder.
    /// </summary>
    Task<Result<IReadOnlyList<StorageItem>>> ListFilesAsync(string FolderId, CancellationToken CancellationToken);

    /// <summary>
    /// Lists remote changes after the specified page token.
    /// </summary>
    Task<Result<IReadOnlyList<StorageChange>>> ListChangesAsync(string PageToken, CancellationToken CancellationToken);
}
