// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Contains the result of importing provider changes into remote observations.
/// </summary>
public class RemoteChangeImportResult
{
    // ● properties

    /// <summary>
    /// Gets the observations created for already tracked remote items.
    /// </summary>
    public List<RemoteObservedSnapshotRecord> Observations { get; } = new();

    /// <summary>
    /// Gets the changes whose remote item id is not tracked yet.
    /// </summary>
    public List<StorageChange> UntrackedChanges { get; } = new();
}
