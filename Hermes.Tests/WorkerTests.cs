// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests the hosted background worker wiring.
/// </summary>
public class WorkerTests
{
    // ● public

    /// <summary>
    /// Verifies the hosted worker starts the metadata sync loop.
    /// </summary>
    [Fact]
    public async Task WorkerStartsMetadataSyncLoop()
    {
        ServiceCollection Services = new();
        SignalingMetadataSyncRunner Runner = new();
        Services.AddLogging();
        Services.AddSingleton<IMetadataSyncRunner>(Runner);
        Services.AddSingleton(MetadataSyncTestHost.CreateSyncRoot());
        Services.AddSingleton(Options.Create(MetadataSyncTestHost.CreateSettings()));
        Services.AddSingleton<MetadataSyncLoop>();
        Services.AddHostedService<Worker>();

        using ServiceProvider Provider = Services.BuildServiceProvider();
        IHostedService Worker = Provider.GetRequiredService<IHostedService>();

        await Worker.StartAsync(CancellationToken.None);
        await Runner.WaitUntilCalledAsync();
        await Worker.StopAsync(CancellationToken.None);

        Assert.Equal(1, Runner.Calls);
    }
}
