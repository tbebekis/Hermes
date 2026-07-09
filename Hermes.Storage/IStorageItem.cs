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
}
