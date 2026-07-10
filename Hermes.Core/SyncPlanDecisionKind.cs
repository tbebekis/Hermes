// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Defines the synchronization planner decision for a tracked item.
/// </summary>
public enum SyncPlanDecisionKind
{
    /// <summary>
    /// No action is required.
    /// </summary>
    None,

    /// <summary>
    /// The local change should be uploaded to remote storage.
    /// </summary>
    UploadToRemote,

    /// <summary>
    /// The remote change should be downloaded locally.
    /// </summary>
    DownloadToLocal,

    /// <summary>
    /// The remote namespace change should be applied to the local filesystem.
    /// </summary>
    ApplyRemoteNamespaceToLocal,

    /// <summary>
    /// The local namespace change should be applied to remote storage.
    /// </summary>
    ApplyLocalNamespaceToRemote,

    /// <summary>
    /// The local deletion should be propagated to remote storage.
    /// </summary>
    PropagateLocalDelete,

    /// <summary>
    /// The remote deletion should be propagated locally.
    /// </summary>
    PropagateRemoteDelete,

    /// <summary>
    /// The base snapshot can be advanced without endpoint operations.
    /// </summary>
    CommitBase,

    /// <summary>
    /// User or policy conflict resolution is required.
    /// </summary>
    Conflict,

    /// <summary>
    /// Planning is blocked until an external condition is resolved.
    /// </summary>
    Blocked
}
