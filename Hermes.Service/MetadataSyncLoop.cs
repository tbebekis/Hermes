// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Runs the metadata synchronization loop used by the background service.
/// </summary>
public class MetadataSyncLoop
{
    // ● fields

    readonly IMetadataSyncRunner fRunner;
    readonly SyncRootRecord fSyncRoot;
    readonly SyncSettings fSettings;
    readonly SyncActivityStore fActivityStore;
    readonly ILogger<MetadataSyncLoop> fLogger;

    // ● private

    static int GetPollingIntervalSeconds(SyncSettings Settings)
    {
        return Math.Max(1, Settings.PollingIntervalSeconds);
    }
    void LogSuccess(MetadataSyncRunResult Result)
    {
        fActivityStore.AddSuccess(fSyncRoot.Id, Result);
        fLogger.LogInformation(
            "Sync pass completed for root {SyncRootId}. Kind: {Kind}. Local items: {LocalObservedItemCount}. Remote items: {RemoteObservedItemCount}. Remote changes: {RemoteObservedChangeCount}. Decisions: {DecisionCount}. Pending executions: {PendingExecutionCount}. Open conflicts: {OpenConflictCount}. Pending summary: {PendingExecutionSummary}. Pending diffs: {PendingDiffSummary}. Namespace collisions: {NamespaceCollisionSummary}. Blocked items: {BlockedExecutionSummary}. Committed executions: {CommittedExecutionCount}. Uncommitted executions: {UncommittedExecutionCount}. Uncommitted summary: {UncommittedExecutionSummary}. Uncommitted messages: {UncommittedExecutionMessages}.",
            fSyncRoot.Id,
            Result.Kind,
            Result.LocalObservedItemCount,
            Result.RemoteObservedItemCount,
            Result.RemoteObservedChangeCount,
            Result.DecisionCount,
            Result.PendingExecutionCount,
            Result.OpenConflictCount,
            Result.PendingExecutionSummary,
            Result.PendingDiffSummary,
            Result.NamespaceCollisionSummary,
            Result.BlockedExecutionSummary,
            Result.CommittedExecutionCount,
            Result.UncommittedExecutionCount,
            Result.UncommittedExecutionSummary,
            Result.UncommittedExecutionMessages);
    }
    async Task RunPassAsync(CancellationToken CancellationToken)
    {
        try
        {
            Result<MetadataSyncRunResult> Result = await fRunner.RunOnceAsync(fSyncRoot.Id, CancellationToken);

            if (Result.Failed)
            {
                fActivityStore.AddFailure(fSyncRoot.Id, Result.ErrorText);
                fLogger.LogError("Sync pass failed for root {SyncRootId}. {Message}", fSyncRoot.Id, Result.ErrorText);
            }
            else
                LogSuccess(Result.Value);
        }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception Ex)
        {
            fActivityStore.AddFailure(fSyncRoot.Id, Ex.Message);
            fLogger.LogError(Ex, "Sync pass failed unexpectedly for root {SyncRootId}.", fSyncRoot.Id);
        }
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataSyncLoop"/> class.
    /// </summary>
    public MetadataSyncLoop(
        IMetadataSyncRunner Runner,
        SyncRootRecord SyncRoot,
        IOptions<SyncSettings> Settings,
        SyncActivityStore ActivityStore,
        ILogger<MetadataSyncLoop> Logger)
    {
        fRunner = Guard.NotNull(Runner, nameof(Runner));
        fSyncRoot = Guard.NotNull(SyncRoot, nameof(SyncRoot));
        fSettings = Guard.NotNull(Settings, nameof(Settings)).Value;
        fActivityStore = Guard.NotNull(ActivityStore, nameof(ActivityStore));
        fLogger = Guard.NotNull(Logger, nameof(Logger));
    }

    // ● public

    /// <summary>
    /// Runs the synchronization loop.
    /// </summary>
    public async Task RunAsync(CancellationToken CancellationToken)
    {
        fLogger.LogInformation(
            "Hermes metadata sync loop started for root {SyncRootId}. Mutations enabled: {EnableMutations}.",
            fSyncRoot.Id,
            fSettings.EnableMutations);

        try
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                await RunPassAsync(CancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(GetPollingIntervalSeconds(fSettings)), CancellationToken);
            }
        }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
        {
        }

        fLogger.LogInformation("Hermes metadata sync loop stopped for root {SyncRootId}.", fSyncRoot.Id);
    }
}
