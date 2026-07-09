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
    {
        this.Id = Guard.NotNullOrWhiteSpace(Id, nameof(Id));
        this.ParentId = ParentId ?? string.Empty;
        this.Name = Guard.NotNullOrWhiteSpace(Name, nameof(Name));
        this.Path = Guard.NotNullOrWhiteSpace(Path, nameof(Path));
        this.Kind = Kind;
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
}
