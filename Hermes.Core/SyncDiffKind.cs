// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Defines the classified synchronization difference for a tracked item.
/// </summary>
public enum SyncDiffKind
{
    /// <summary>
    /// No meaningful difference was detected.
    /// </summary>
    NoChange,

    /// <summary>
    /// Only the local side changed.
    /// </summary>
    LocalChanged,

    /// <summary>
    /// Only the remote side changed.
    /// </summary>
    RemoteChanged,

    /// <summary>
    /// Only the remote namespace changed.
    /// </summary>
    RemoteNamespaceChanged,

    /// <summary>
    /// Only the local namespace changed.
    /// </summary>
    LocalNamespaceChanged,

    /// <summary>
    /// Both sides changed to compatible state.
    /// </summary>
    BothChangedCompatible,

    /// <summary>
    /// Both sides changed incompatibly.
    /// </summary>
    Conflict,

    /// <summary>
    /// The item was not observed locally.
    /// </summary>
    LocalMissing,

    /// <summary>
    /// The item was not observed remotely.
    /// </summary>
    RemoteMissing,

    /// <summary>
    /// The item exists remotely but is trashed.
    /// </summary>
    RemoteTrashed,

    /// <summary>
    /// The provider reported permanent remote removal.
    /// </summary>
    RemoteRemoved,

    /// <summary>
    /// The item is blocked by a namespace collision.
    /// </summary>
    NamespaceCollision
}
