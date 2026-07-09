// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Represents a storage item.
/// </summary>
public class StorageItem : IStorageItem
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageItem"/> class.
    /// </summary>
    public StorageItem(string Id, string ParentId, string Name, string Path, StorageItemKind Kind)
        : this(Id, ParentId, Name, Path, Kind, string.Empty, 0, string.Empty, default, default, 0, false)
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageItem"/> class.
    /// </summary>
    public StorageItem(
        string Id,
        string ParentId,
        string Name,
        string Path,
        StorageItemKind Kind,
        string MimeType,
        long Size,
        string Md5Hash,
        DateTimeOffset CreatedTime,
        DateTimeOffset ModifiedTime,
        long Version,
        bool Trashed)
    {
        this.Id = Guard.NotNullOrWhiteSpace(Id, nameof(Id));
        this.ParentId = ParentId ?? string.Empty;
        this.Name = Guard.NotNullOrWhiteSpace(Name, nameof(Name));
        this.Path = Guard.NotNullOrWhiteSpace(Path, nameof(Path));
        this.Kind = Kind;
        this.MimeType = MimeType ?? string.Empty;
        this.Size = Size;
        this.Md5Hash = Md5Hash ?? string.Empty;
        this.CreatedTime = CreatedTime;
        this.ModifiedTime = ModifiedTime;
        this.Version = Version;
        this.Trashed = Trashed;
    }

    // ● properties

    /// <inheritdoc/>
    public string Id { get; }
    /// <inheritdoc/>
    public string ParentId { get; }
    /// <inheritdoc/>
    public string Name { get; }
    /// <inheritdoc/>
    public string Path { get; }
    /// <inheritdoc/>
    public StorageItemKind Kind { get; }
    /// <inheritdoc/>
    public string MimeType { get; }
    /// <inheritdoc/>
    public bool IsFolder => Kind == StorageItemKind.Folder;
    /// <inheritdoc/>
    public long Size { get; }
    /// <inheritdoc/>
    public string Md5Hash { get; }
    /// <inheritdoc/>
    public DateTimeOffset CreatedTime { get; }
    /// <inheritdoc/>
    public DateTimeOffset ModifiedTime { get; }
    /// <inheritdoc/>
    public long Version { get; }
    /// <inheritdoc/>
    public bool Trashed { get; }
}
