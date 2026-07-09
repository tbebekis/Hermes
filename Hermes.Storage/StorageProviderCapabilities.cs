// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Describes the operations supported by a storage provider.
/// </summary>
public class StorageProviderCapabilities
{
    // ● properties

    /// <summary>
    /// Gets or sets a value indicating whether the provider supports change tokens.
    /// </summary>
    public bool SupportsChangeTokens { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider supports resumable uploads.
    /// </summary>
    public bool SupportsResumableUpload { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider supports server-side trash.
    /// </summary>
    public bool SupportsTrash { get; set; }
}
