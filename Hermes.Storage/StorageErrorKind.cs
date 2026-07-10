// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Defines provider-neutral storage error categories.
/// </summary>
public enum StorageErrorKind
{
    /// <summary>
    /// The error could not be classified.
    /// </summary>
    Unknown,

    /// <summary>
    /// The requested item was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The provider rejected an incremental synchronization checkpoint.
    /// </summary>
    CheckpointInvalid,

    /// <summary>
    /// The provider denied the requested operation.
    /// </summary>
    PermissionDenied,

    /// <summary>
    /// The provider rate-limited the requested operation.
    /// </summary>
    RateLimited,

    /// <summary>
    /// The provider or network is temporarily unavailable.
    /// </summary>
    TemporarilyUnavailable,

    /// <summary>
    /// The operation conflicted with provider state.
    /// </summary>
    Conflict,

    /// <summary>
    /// The provider rejected the request as invalid.
    /// </summary>
    InvalidRequest
}
