// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Defines durable synchronization conflict states.
/// </summary>
public enum SyncConflictState
{
    /// <summary>
    /// Conflict blocks synchronization until it is resolved.
    /// </summary>
    Open = 0,

    /// <summary>
    /// Conflict no longer blocks synchronization.
    /// </summary>
    Resolved = 1,
}
