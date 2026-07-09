// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Describes a storage file.
/// </summary>
public interface IStorageFile : IStorageItem
{
    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    long Size { get; }

    /// <summary>
    /// Gets the provider checksum.
    /// </summary>
    string Checksum { get; }
}
