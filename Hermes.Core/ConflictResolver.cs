// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Detects and resolves synchronization conflicts.
/// </summary>
public class ConflictResolver
{
    // ● public

    /// <summary>
    /// Determines whether a conflict exists.
    /// </summary>
    public bool HasConflict(bool LocalChanged, bool RemoteChanged)
    {
        return LocalChanged && RemoteChanged;
    }
}
