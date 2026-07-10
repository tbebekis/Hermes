// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests metadata synchronization service loop behavior.
/// </summary>
public class MetadataSyncLoopTests
{
    // ● private

    /// <summary>
    /// Fake metadata sync runner.
    /// </summary>
    sealed class FakeRunner : IMetadataSyncRunner
    {
        // ● fields

        readonly CancellationTokenSource fCancellation;
        readonly Func<Result<MetadataSyncRunResult>> fRun;

        // ● constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeRunner"/> class.
        /// </summary>
        public FakeRunner(CancellationTokenSource Cancellation, Func<Result<MetadataSyncRunResult>> Run)
        {
            fCancellation = Guard.NotNull(Cancellation, nameof(Cancellation));
            fRun = Guard.NotNull(Run, nameof(Run));
        }

        // ● public

        /// <inheritdoc/>
        public Task<Result<MetadataSyncRunResult>> RunOnceAsync(string SyncRootId, CancellationToken CancellationToken)
        {
            Calls++;
            fCancellation.Cancel();
            return Task.FromResult(fRun());
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
        PollingIntervalSeconds = 1,
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
    static MetadataSyncLoop Loop(IMetadataSyncRunner Runner)
    {
        return new MetadataSyncLoop(
            Runner,
            SyncRoot(),
            Options.Create(Settings()),
            NullLogger<MetadataSyncLoop>.Instance);
    }

    // ● public

    /// <summary>
    /// Verifies failed sync results do not terminate the loop before cancellation.
    /// </summary>
    [Fact]
    public async Task RunAsyncHandlesFailedResult()
    {
        using CancellationTokenSource Cancellation = new();
        FakeRunner Runner = new(Cancellation, () => Result<MetadataSyncRunResult>.Failure("failed"));

        await Loop(Runner).RunAsync(Cancellation.Token);

        Assert.Equal(1, Runner.Calls);
    }

    /// <summary>
    /// Verifies unexpected sync pass exceptions do not terminate the loop before cancellation.
    /// </summary>
    [Fact]
    public async Task RunAsyncHandlesUnexpectedException()
    {
        using CancellationTokenSource Cancellation = new();
        FakeRunner Runner = new(Cancellation, () => throw new InvalidOperationException("boom"));

        await Loop(Runner).RunAsync(Cancellation.Token);

        Assert.Equal(1, Runner.Calls);
    }
}
