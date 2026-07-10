// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests the service application entry point composition.
/// </summary>
public class ProgramTests
{
    // ● private

    static string[] Args() =>
    [
        "--Sync:SyncRootId=default",
        "--Sync:LocalRootPath=/tmp/hermes",
        "--Sync:RemoteRootFolderId=root",
        "--Sync:PollingIntervalSeconds=60",
    ];

    // ● public

    /// <summary>
    /// Verifies the service host builder can bind synchronization settings.
    /// </summary>
    [Fact]
    public void CreateHostBuilderBuildsConfiguredHost()
    {
        using IHost Host = Program.CreateHostBuilder(Args()).Build();

        SyncSettings Settings = Host.Services.GetRequiredService<IOptions<SyncSettings>>().Value;

        Assert.Equal("default", Settings.SyncRootId);
        Assert.Equal("/tmp/hermes", Settings.LocalRootPath);
        Assert.Equal("root", Settings.RemoteRootFolderId);
        Assert.Equal(60, Settings.PollingIntervalSeconds);
    }
    /// <summary>
    /// Verifies the service host can start and stop with controlled synchronization dependencies.
    /// </summary>
    [Fact]
    public async Task CreateHostBuilderStartsAndStopsHostedWorker()
    {
        SignalingMetadataSyncRunner Runner = new();
        using IHost Host = Program.CreateHostBuilder(Args())
            .ConfigureServices((Context, Services) =>
            {
                Services.AddSingleton<IMetadataSyncRunner>(Runner);
                Services.AddSingleton(MetadataSyncTestHost.CreateSyncRoot());
            })
            .Build();

        await Host.StartAsync();
        await Runner.WaitUntilCalledAsync();
        await Host.StopAsync();

        Assert.Equal(1, Runner.Calls);
    }
}
