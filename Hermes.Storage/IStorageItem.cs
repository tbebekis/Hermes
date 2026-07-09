// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Describes a storage item.
/// </summary>
public interface IStorageItem
{
    /// <summary>
    /// Gets the provider-specific item id.
    /// </summary>
    string Id { get; }
    /// <summary>
    /// Gets the provider-specific parent id.
    /// </summary>
    string ParentId { get; }
    /// <summary>
    /// Gets the display name.
    /// </summary>
    string Name { get; }
    /// <summary>
    /// Gets the logical full path.
    /// </summary>
    string Path { get; }
    /// <summary>
    /// Gets the storage item kind.
    /// </summary>
    StorageItemKind Kind { get; }
    /// <summary>
    /// Gets the provider MIME type.
    /// </summary>
    string MimeType { get; }
    /// <summary>
    /// Gets a value indicating whether this item is a folder.
    /// </summary>
    bool IsFolder { get; }
    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    long Size { get; }
    /// <summary>
    /// Gets the MD5 hash when supplied by the provider.
    /// </summary>
    string Md5Hash { get; }
    /// <summary>
    /// Gets the created timestamp.
    /// </summary>
    DateTimeOffset CreatedTime { get; }
    /// <summary>
    /// Gets the modified timestamp.
    /// </summary>
    DateTimeOffset ModifiedTime { get; }
    /// <summary>
    /// Gets the provider version.
    /// </summary>
    long Version { get; }
    /// <summary>
    /// Gets a value indicating whether this item is trashed.
    /// </summary>
    bool Trashed { get; }
}
