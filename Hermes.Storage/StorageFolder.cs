// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Represents a storage folder.
/// </summary>
public class StorageFolder : StorageItem, IStorageFolder
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageFolder"/> class.
    /// </summary>
    public StorageFolder(string Id, string ParentId, string Name, string Path)
        : this(Id, ParentId, Name, Path, string.Empty, default, default, 0, false)
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageFolder"/> class.
    /// </summary>
    public StorageFolder(
        string Id,
        string ParentId,
        string Name,
        string Path,
        string MimeType,
        DateTimeOffset CreatedTime,
        DateTimeOffset ModifiedTime,
        long Version,
        bool Trashed)
        : base(Id, ParentId, Name, Path, StorageItemKind.Folder, MimeType, 0, string.Empty, CreatedTime, ModifiedTime, Version, Trashed)
    {
    }
}
