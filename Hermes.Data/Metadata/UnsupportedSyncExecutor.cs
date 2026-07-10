// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Executor placeholder used until endpoint mutation contracts are implemented.
/// </summary>
public class UnsupportedSyncExecutor : SyncExecutorBase
{
    // ● protected

    /// <summary>
    /// Returns a blocked result for executable intents because endpoint mutation is not implemented yet.
    /// </summary>
    protected override Task<SyncExecutionResult> ExecuteIntentAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
    {
        Guard.NotNull(Intent, nameof(Intent));

        return Task.FromResult(SyncExecutionResultFactory.Blocked(Intent.Request, "Synchronization execution is not implemented."));
    }
}
