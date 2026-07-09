// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Background worker hosted by the Linux service.
/// </summary>
public class Worker : BackgroundService
{
    // ● private

    private readonly SyncService fSyncService;

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    public Worker(SyncService SyncService)
    {
        fSyncService = Guard.NotNull(SyncService, nameof(SyncService));
    }

    // ● protected

    /// <inheritdoc/>
    protected override Task ExecuteAsync(CancellationToken StoppingToken)
    {
        return fSyncService.RunAsync(StoppingToken);
    }
}
