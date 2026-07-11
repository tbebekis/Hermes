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

    /// <summary>
    /// Captures log messages for assertions.
    /// </summary>
    sealed class TestLogger : ILogger<MetadataSyncLoop>
    {
        // ● private

        /// <summary>
        /// Empty logging scope.
        /// </summary>
        sealed class EmptyScope : IDisposable
        {
            // ● fields

            static readonly EmptyScope fInstance = new();

            // ● public

            /// <inheritdoc/>
            public void Dispose()
            {
            }

            // ● properties

            /// <summary>
            /// Gets the empty scope instance.
            /// </summary>
            static public EmptyScope Instance => fInstance;
        }

        // ● public

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState State) => EmptyScope.Instance;
        /// <inheritdoc/>
        public bool IsEnabled(LogLevel LogLevel) => true;
        /// <inheritdoc/>
        public void Log<TState>(LogLevel LogLevel, EventId EventId, TState State, Exception Exception, Func<TState, Exception, string> Formatter)
        {
            Entries.Add(Formatter(State, Exception));
        }

        // ● properties

        /// <summary>
        /// Gets captured log entries.
        /// </summary>
        public List<string> Entries { get; } = new();
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
    static MetadataSyncLoop Loop(IMetadataSyncRunner Runner, ILogger<MetadataSyncLoop> Logger = null)
    {
        return new MetadataSyncLoop(
            Runner,
            SyncRoot(),
            Options.Create(Settings()),
            Logger ?? NullLogger<MetadataSyncLoop>.Instance);
    }
    static MetadataSyncRunResult RunResult()
    {
        MetadataSyncSessionResult SessionResult = new();
        SessionResult.PendingExecutionRequests.Add(new SyncExecutionRequest()
        {
            Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalChanged, SyncPlanDecisionKind.UploadToRemote),
        });
        SessionResult.PendingExecutionRequests.Add(new SyncExecutionRequest()
        {
            Decision = new SyncPlanDecision("item-2", SyncDiffKind.RemoteChanged, SyncPlanDecisionKind.DownloadToLocal),
        });
        SessionResult.PendingExecutionRequests.Add(new SyncExecutionRequest()
        {
            Decision = new SyncPlanDecision("item-3", SyncDiffKind.LocalChanged, SyncPlanDecisionKind.UploadToRemote),
        });
        SessionResult.PendingExecutionRequests.Add(new SyncExecutionRequest()
        {
            Decision = new SyncPlanDecision("item-4", SyncDiffKind.NamespaceCollision, SyncPlanDecisionKind.Blocked),
            RemoteObservation = new RemoteObservedSnapshotRecord()
            {
                Name = "DuplicateName.txt",
                RemoteItemId = "remote-4",
            },
        });

        SyncExecutionApplyResult ApplyResult = new();
        ApplyResult.UncommittedResults.Add(new SyncExecutionResult()
        {
            Request = SessionResult.PendingExecutionRequests[3],
            ResultKind = SyncExecutionResultKind.Blocked,
            Message = "blocked",
        });

        return new MetadataSyncRunResult()
        {
            Kind = MetadataSyncRunKind.Incremental,
            LocalObservedItemCount = 2,
            RemoteObservedItemCount = 0,
            RemoteObservedChangeCount = 3,
            OpenConflictCount = 4,
            SessionResult = SessionResult,
            ExecutionApplyResult = ApplyResult,
        };
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

    /// <summary>
    /// Verifies successful sync results are logged with the run summary.
    /// </summary>
    [Fact]
    public async Task RunAsyncLogsSuccessfulRunSummary()
    {
        using CancellationTokenSource Cancellation = new();
        FakeRunner Runner = new(Cancellation, () => Result<MetadataSyncRunResult>.Success(RunResult()));
        TestLogger Logger = new();

        await Loop(Runner, Logger).RunAsync(Cancellation.Token);

        Assert.Contains(Logger.Entries, Item => Item.Contains("Mutations enabled: False."));
        Assert.Contains(Logger.Entries, Item => Item.Contains("Kind: Incremental.") && Item.Contains("Local items: 2.") && Item.Contains("Remote changes: 3.") && Item.Contains("Open conflicts: 4.") && Item.Contains("Pending summary: UploadToRemote=2, DownloadToLocal=1, Blocked=1.") && Item.Contains("Pending diffs: LocalChanged=2, RemoteChanged=1, NamespaceCollision=1.") && Item.Contains("Namespace collisions: DuplicateName.txt@=1.") && Item.Contains("Blocked items: NamespaceCollision:DuplicateName.txt#remote-4.") && Item.Contains("Uncommitted executions: 1.") && Item.Contains("Uncommitted summary: Blocked=1.") && Item.Contains("Uncommitted messages: Blocked:DuplicateName.txt#remote-4:blocked."));
    }
}
