// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Provides common intent validation for synchronization executors.
/// </summary>
public abstract class SyncExecutorBase : ISyncExecutor
{
    // ● private

    static SyncExecutionResultKind RejectedResultKind(SyncExecutionIntent Intent)
    {
        return Intent.IntentKind switch
        {
            SyncExecutionIntentKind.ResolveConflict => SyncExecutionResultKind.Conflict,
            SyncExecutionIntentKind.Blocked => SyncExecutionResultKind.Blocked,
            _ => SyncExecutionResultKind.FailedPermanent,
        };
    }
    static SyncExecutionResult CreateRejectedResult(SyncExecutionIntent Intent)
    {
        return new SyncExecutionResult()
        {
            Request = Intent.Request,
            ResultKind = RejectedResultKind(Intent),
            Message = string.Join(Environment.NewLine, Intent.ValidationMessages),
        };
    }

    // ● protected

    /// <summary>
    /// Executes an intent that passed common validation.
    /// </summary>
    protected abstract Task<SyncExecutionResult> ExecuteIntentAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken);

    // ● public

    /// <summary>
    /// Executes synchronization requests after translating them to executor-facing intents.
    /// </summary>
    public async Task<IReadOnlyList<SyncExecutionResult>> ExecuteAsync(IEnumerable<SyncExecutionRequest> Requests, CancellationToken CancellationToken)
    {
        Guard.NotNull(Requests, nameof(Requests));

        List<SyncExecutionResult> Results = new();

        foreach (SyncExecutionRequest Request in Requests)
        {
            CancellationToken.ThrowIfCancellationRequested();

            SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(Request);
            if (!Intent.CanExecute)
            {
                Results.Add(CreateRejectedResult(Intent));
                continue;
            }

            SyncExecutionResult Result = await ExecuteIntentAsync(Intent, CancellationToken);
            Guard.NotNull(Result, nameof(Result));

            Result.Request ??= Request;
            Results.Add(Result);
        }

        return Results;
    }
}
