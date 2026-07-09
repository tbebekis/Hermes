// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Defines a planned synchronization operation type.
/// </summary>
public enum SyncOperationType
{
    /// <summary>
    /// Uploads a local item to remote storage.
    /// </summary>
    Upload,

    /// <summary>
    /// Downloads a remote item to local storage.
    /// </summary>
    Download,

    /// <summary>
    /// Deletes an item.
    /// </summary>
    Delete,

    /// <summary>
    /// Moves an item.
    /// </summary>
    Move,

    /// <summary>
    /// Renames an item.
    /// </summary>
    Rename,

    /// <summary>
    /// Creates a conflict copy.
    /// </summary>
    KeepBoth
}
