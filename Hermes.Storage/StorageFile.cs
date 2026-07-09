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
        : this(Id, ParentId, Name, Path, string.Empty, Size, Checksum, default, default, 0, false)
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageFile"/> class.
    /// </summary>
    public StorageFile(
        string Id,
        string ParentId,
        string Name,
        string Path,
        string MimeType,
        long Size,
        string Md5Hash,
        DateTimeOffset CreatedTime,
        DateTimeOffset ModifiedTime,
        long Version,
        bool Trashed)
        : base(Id, ParentId, Name, Path, StorageItemKind.File, MimeType, Size, Md5Hash, CreatedTime, ModifiedTime, Version, Trashed)
    {
        this.Checksum = Md5Hash ?? string.Empty;
    }

    // ● properties

    /// <inheritdoc/>
    public string Checksum { get; }
}
