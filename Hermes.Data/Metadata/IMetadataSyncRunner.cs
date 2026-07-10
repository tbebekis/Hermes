// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Defines metadata synchronization runner behavior.
/// </summary>
public interface IMetadataSyncRunner
{
    // ● public

    /// <summary>
    /// Runs one metadata synchronization pass for a sync root.
    /// </summary>
    Task<Result<MetadataSyncRunResult>> RunOnceAsync(string SyncRootId, CancellationToken CancellationToken);
}
