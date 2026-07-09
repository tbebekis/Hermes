// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Represents a storage file.
/// </summary>
public class StorageFile : StorageItem, IStorageFile
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageFile"/> class.
    /// </summary>
    public StorageFile(string Id, string ParentId, string Name, string Path, long Size, string Checksum)
        : base(Id, ParentId, Name, Path, StorageItemKind.File)
    {
        this.Size = Size;
        this.Checksum = Checksum ?? string.Empty;
    }

    // ● properties

    /// <inheritdoc/>
    public long Size { get; }

    /// <inheritdoc/>
    public string Checksum { get; }
}
