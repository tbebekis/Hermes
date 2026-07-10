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
    Task<StorageResult<string>> GetStartPageTokenAsync(CancellationToken CancellationToken);

    /// <summary>
    /// Gets a remote item.
    /// </summary>
    Task<StorageResult<StorageItem>> GetItemAsync(string ItemId, CancellationToken CancellationToken);

    /// <summary>
    /// Lists immediate child items under the specified folder.
    /// </summary>
    Task<StorageResult<IReadOnlyList<StorageItem>>> ListFolderAsync(string FolderId, CancellationToken CancellationToken);

    /// <summary>
    /// Lists remote changes after the specified page token.
    /// </summary>
    Task<StorageResult<StorageChangeListResult>> ListChangesAsync(string PageToken, CancellationToken CancellationToken);
}
