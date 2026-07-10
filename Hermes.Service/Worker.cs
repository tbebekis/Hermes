// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Background worker hosted by the Linux service.
/// </summary>
public class Worker : BackgroundService
{
    // ● fields

    readonly MetadataSyncLoop fSyncLoop;

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    public Worker(MetadataSyncLoop SyncLoop)
    {
        fSyncLoop = Guard.NotNull(SyncLoop, nameof(SyncLoop));
    }

    // ● protected

    /// <inheritdoc/>
    protected override Task ExecuteAsync(CancellationToken StoppingToken)
    {
        return fSyncLoop.RunAsync(StoppingToken);
    }
}
