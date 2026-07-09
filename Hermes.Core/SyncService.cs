// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Runs the synchronization loop used by the background service.
/// </summary>
public class SyncService
{
    // ● private

    private readonly SyncEngine fSyncEngine;
    private readonly ILogger<SyncService> fLogger;

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncService"/> class.
    /// </summary>
    public SyncService(SyncEngine SyncEngine, ILogger<SyncService> Logger)
    {
        fSyncEngine = Guard.NotNull(SyncEngine, nameof(SyncEngine));
        fLogger = Guard.NotNull(Logger, nameof(Logger));
    }

    // ● public

    /// <summary>
    /// Runs the synchronization loop.
    /// </summary>
    public async Task RunAsync(CancellationToken CancellationToken)
    {
        fLogger.LogInformation("Hermes sync service started.");

        while (!CancellationToken.IsCancellationRequested)
        {
            await fSyncEngine.RunOnceAsync(CancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(60), CancellationToken);
        }
    }
}
