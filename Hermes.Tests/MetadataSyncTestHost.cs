// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Provides shared metadata synchronization host test helpers.
/// </summary>
static class MetadataSyncTestHost
{
    // ● public

    /// <summary>
    /// Creates test synchronization settings.
    /// </summary>
    static public SyncSettings CreateSettings() => new()
    {
        SyncRootId = "default",
        LocalRootPath = "/tmp/hermes",
        RemoteRootFolderId = "root",
        PollingIntervalSeconds = 60,
    };
    /// <summary>
    /// Creates a test synchronization root record.
    /// </summary>
    static public SyncRootRecord CreateSyncRoot() => new()
    {
        Id = "default",
        ProviderName = "Fake",
        ConnectionId = "account-1",
        LocalRootPath = "/tmp/hermes",
        RemoteRootItemId = "root",
        IsEnabled = true,
        CreatedTime = new DateTime(2026, 7, 10, 10, 0, 0),
    };
}

/// <summary>
/// Metadata sync runner that signals when it is called.
/// </summary>
public class SignalingMetadataSyncRunner : IMetadataSyncRunner
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
