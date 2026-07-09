// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Creates synchronization plans from local and remote changes.
/// </summary>
public class SyncPlanner
{
    // ● public

    /// <summary>
    /// Creates an empty initial synchronization plan.
    /// </summary>
    public IReadOnlyList<SyncOperation> CreateInitialPlan()
    {
        return Array.Empty<SyncOperation>();
    }
}
