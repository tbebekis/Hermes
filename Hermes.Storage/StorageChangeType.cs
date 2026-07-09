// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Defines the type of a remote storage change.
/// </summary>
public enum StorageChangeType
{
    /// <summary>
    /// The item was created.
    /// </summary>
    Created,

    /// <summary>
    /// The item was updated.
    /// </summary>
    Updated,

    /// <summary>
    /// The item was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// The item was moved.
    /// </summary>
    Moved,

    /// <summary>
    /// The item was renamed.
    /// </summary>
    Renamed
}
