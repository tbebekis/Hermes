// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests the hosted background worker wiring.
/// </summary>
public class WorkerTests
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

    static SyncSettings Settings() => new()
    {
        SyncRootId = "default",
        LocalRootPath = "/tmp/hermes",
        RemoteRootFolderId = "root",
        PollingIntervalSeconds = 60,
    };
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
    /// Verifies the hosted worker starts the metadata sync loop.
    /// </summary>
    [Fact]
    public async Task WorkerStartsMetadataSyncLoop()
    {
        ServiceCollection Services = new();
        SignalingRunner Runner = new();
        Services.AddLogging();
        Services.AddSingleton<IMetadataSyncRunner>(Runner);
        Services.AddSingleton(SyncRoot());
        Services.AddSingleton(Options.Create(Settings()));
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
