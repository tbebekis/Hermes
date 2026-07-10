// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Executes synchronization requests produced by a metadata synchronization session.
/// </summary>
public interface ISyncExecutor
{
    // ● public

    /// <summary>
    /// Executes synchronization requests and returns their execution results.
    /// </summary>
    Task<IReadOnlyList<SyncExecutionResult>> ExecuteAsync(IEnumerable<SyncExecutionRequest> Requests, CancellationToken CancellationToken);
}
