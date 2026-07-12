// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Coordinates synchronization cycle execution so manual and scheduled cycles do not overlap.
/// </summary>
public class SyncCycleCoordinator
{
    // ● fields

    /// <summary>
    /// Message returned when a synchronization cycle is already running.
    /// </summary>
    public const string SyncAlreadyRunningMessage = "A synchronization cycle is already running.";

    readonly SemaphoreSlim fSemaphore = new(1, 1);
    readonly IMetadataSyncRunner fRunner;
    readonly SyncRootRecord fSyncRoot;

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncCycleCoordinator"/> class.
    /// </summary>
    public SyncCycleCoordinator(IMetadataSyncRunner Runner, SyncRootRecord SyncRoot)
    {
        fRunner = Guard.NotNull(Runner, nameof(Runner));
        fSyncRoot = Guard.NotNull(SyncRoot, nameof(SyncRoot));
    }

    // ● public

    /// <summary>
    /// Runs one synchronization cycle when no other cycle is currently running.
    /// </summary>
    public async Task<Result<MetadataSyncRunResult>> TryRunOnceAsync(CancellationToken CancellationToken)
    {
        if (!await fSemaphore.WaitAsync(0, CancellationToken))
            return Result<MetadataSyncRunResult>.Failure(SyncAlreadyRunningMessage);

        try
        {
            return await fRunner.RunOnceAsync(fSyncRoot.Id, CancellationToken);
        }
        finally
        {
            fSemaphore.Release();
        }
    }

    // ● properties

    /// <summary>
    /// Gets a value indicating whether a synchronization cycle is currently running.
    /// </summary>
    public bool IsRunning => fSemaphore.CurrentCount == 0;
}
