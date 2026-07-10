// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Contains the result of importing local scan items into tracked items.
/// </summary>
public class LocalScanImportResult
{
    // ● properties

    /// <summary>
    /// Gets the tracked items created for previously unknown local items.
    /// </summary>
    public List<TrackedItemRecord> CreatedTrackedItems { get; } = new();

    /// <summary>
    /// Gets the local observations created or updated during import.
    /// </summary>
    public List<LocalObservedSnapshotRecord> Observations { get; } = new();
}
