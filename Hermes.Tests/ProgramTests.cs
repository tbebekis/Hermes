// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests the service application entry point composition.
/// </summary>
public class ProgramTests
{
    // ● private

    /// <summary>
    /// Metadata sync runner that signals when it is called.
    /// </summary>
    sealed class SignalingRunner : IMetadataSyncRunner
    {
        // ● fields

        readonly TaskCompletionSource<int> fCalled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // ● public

        /// <inheritdoc/>
        public Task<Result<MetadataSyncRunResult>> RunOnceAsync(string SyncRootId, CancellationToken CancellationToken)
        {
            Calls++;
            fCalled.TrySetResult(Calls);

            MetadataSyncRunResult RunResult = new()
            {
                Kind = MetadataSyncRunKind.Incremental,
                SessionResult = new MetadataSyncSessionResult(),
            };

            return Task.FromResult(Result<MetadataSyncRunResult>.Success(RunResult));
        }
        /// <summary>
        /// Waits until the runner is called.
        /// </summary>
        public async Task WaitUntilCalledAsync()
        {
            Task Completed = await Task.WhenAny(fCalled.Task, Task.Delay(TimeSpan.FromSeconds(3)));

            Assert.Same(fCalled.Task, Completed);
        }

        // ● properties

        /// <summary>
        /// Gets the runner call count.
        /// </summary>
        public int Calls { get; private set; }
    }

    static string[] Args() =>
    [
        "--Sync:SyncRootId=default",
        "--Sync:LocalRootPath=/tmp/hermes",
        "--Sync:RemoteRootFolderId=root",
        "--Sync:PollingIntervalSeconds=60",
    ];
    static SyncRootRecord SyncRoot() => new()
    {
        Id = "default",
        ProviderName = "Fake",
        ConnectionId = "account-1",
        LocalRootPath = "/tmp/hermes",
        RemoteRootItemId = "root",
        IsEnabled = true,
        CreatedTime = new DateTime(2026, 7, 10, 10, 0, 0),
    };

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
        SignalingRunner Runner = new();
        using IHost Host = Program.CreateHostBuilder(Args())
            .ConfigureServices((Context, Services) =>
            {
                Services.AddSingleton<IMetadataSyncRunner>(Runner);
                Services.AddSingleton(SyncRoot());
            })
            .Build();

        await Host.StartAsync();
        await Runner.WaitUntilCalledAsync();
        await Host.StopAsync();

        Assert.Equal(1, Runner.Calls);
    }
}
