// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Defines the executor-facing intent for a synchronization execution request.
/// </summary>
public enum SyncExecutionIntentKind
{
    /// <summary>
    /// The request cannot be translated to an executable intent.
    /// </summary>
    Invalid,

    /// <summary>
    /// Uploads local state to remote storage.
    /// </summary>
    UploadToRemote,

    /// <summary>
    /// Downloads remote state to local storage.
    /// </summary>
    DownloadToLocal,

    /// <summary>
    /// Applies a remote namespace change to the local filesystem.
    /// </summary>
    ApplyRemoteNamespaceToLocal,

    /// <summary>
    /// Propagates local deletion to remote storage.
    /// </summary>
    PropagateLocalDelete,

    /// <summary>
    /// Propagates remote deletion to the local filesystem.
    /// </summary>
    PropagateRemoteDelete,

    /// <summary>
    /// Requires conflict resolution before execution.
    /// </summary>
    ResolveConflict,

    /// <summary>
    /// Is blocked until an external condition is resolved.
    /// </summary>
    Blocked
}
