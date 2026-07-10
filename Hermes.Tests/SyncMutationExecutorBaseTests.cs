// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests synchronization mutation executor base behavior.
/// </summary>
public class SyncMutationExecutorBaseTests
{
    // ● private

    /// <summary>
    /// Test local mutation endpoint.
    /// </summary>
    class TestLocalEndpoint : ILocalSyncMutationEndpoint
    {
        // ● public

        /// <inheritdoc/>
        public string ResolvePath(string LocalRelativePath) => "/local/" + LocalRelativePath;

        /// <inheritdoc/>
        public Task<Result> EnsureParentDirectoryAsync(string LocalRelativePath, CancellationToken CancellationToken)
        {
            return Task.FromResult(Result.Success());
        }

        /// <inheritdoc/>
        public Task<Result> CreateDirectoryAsync(string LocalRelativePath, CancellationToken CancellationToken)
        {
            return Task.FromResult(Result.Success());
        }

        /// <inheritdoc/>
        public Task<Result> DeleteItemAsync(string LocalRelativePath, CancellationToken CancellationToken)
        {
            return Task.FromResult(Result.Success());
        }
    }

    /// <summary>
    /// Test remote mutation endpoint.
    /// </summary>
    class TestRemoteEndpoint : IRemoteSyncMutationEndpoint
    {
        // ● private

        static StorageItem Item() => new("remote-1", "remote-root", "File.txt", "/File.txt", StorageItemKind.File);

        // ● public

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> CreateFolderAsync(string Name, string ParentId, CancellationToken CancellationToken)
        {
            return Task.FromResult(StorageResult<StorageItem>.Success(Item()));
        }

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> UploadFileAsync(string LocalFilePath, string ParentId, CancellationToken CancellationToken)
        {
            return Task.FromResult(StorageResult<StorageItem>.Success(Item()));
        }

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> UpdateFileContentAsync(string RemoteItemId, string LocalFilePath, CancellationToken CancellationToken)
        {
            return Task.FromResult(StorageResult<StorageItem>.Success(Item()));
        }

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> DownloadFileAsync(string RemoteItemId, string LocalFilePath, CancellationToken CancellationToken)
        {
            return Task.FromResult(StorageResult<StorageItem>.Success(Item()));
        }

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> DeleteItemAsync(string RemoteItemId, CancellationToken CancellationToken)
        {
            return Task.FromResult(StorageResult<StorageItem>.Success(Item()));
        }
    }

    /// <summary>
    /// Recording mutation executor.
    /// </summary>
    class RecordingMutationExecutor : SyncMutationExecutorBase
    {
        // ● protected

        /// <inheritdoc/>
        protected override Task<SyncExecutionResult> ExecuteUploadToRemoteAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
        {
            ExecutedIntentKind = Intent.IntentKind;
            return Task.FromResult(SyncExecutionResultFactory.Completed(Intent.Request));
        }

        /// <inheritdoc/>
        protected override Task<SyncExecutionResult> ExecuteDownloadToLocalAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
        {
            ExecutedIntentKind = Intent.IntentKind;
            return Task.FromResult(SyncExecutionResultFactory.Completed(Intent.Request));
        }

        /// <inheritdoc/>
        protected override Task<SyncExecutionResult> ExecutePropagateLocalDeleteAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
        {
            ExecutedIntentKind = Intent.IntentKind;
            return Task.FromResult(SyncExecutionResultFactory.Completed(Intent.Request));
        }

        /// <inheritdoc/>
        protected override Task<SyncExecutionResult> ExecutePropagateRemoteDeleteAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
        {
            ExecutedIntentKind = Intent.IntentKind;
            return Task.FromResult(SyncExecutionResultFactory.Completed(Intent.Request));
        }

        // ● constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingMutationExecutor"/> class.
        /// </summary>
        public RecordingMutationExecutor()
            : base(new TestLocalEndpoint(), new TestRemoteEndpoint())
        {
        }

        // ● properties

        /// <summary>
        /// Gets the executed intent kind.
        /// </summary>
        public SyncExecutionIntentKind ExecutedIntentKind { get; private set; }
    }

    static SyncPlanDecision Decision(SyncPlanDecisionKind DecisionKind) => new("item-1", SyncDiffKind.LocalChanged, DecisionKind);
    static TrackedItemRecord TrackedItem() => new()
    {
        Id = "item-1",
        SyncRootId = "root-1",
        RemoteItemId = "remote-1",
        LocalKey = "File.txt",
        ItemType = "File",
    };
    static LocalObservedSnapshotRecord LocalObservation() => new()
    {
        TrackedItemId = "item-1",
        ExistsFlag = true,
        RelativePath = "File.txt",
        Name = "File.txt",
        ItemType = "File",
        Size = 42,
        ContentHash = "hash-local",
        ObservedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
    };
    static RemoteObservedSnapshotRecord RemoteObservation() => new()
    {
        TrackedItemId = "item-1",
        RemoteItemId = "remote-1",
        ExistsFlag = true,
        Removed = false,
        Name = "File.txt",
        RemoteParentId = "remote-root",
        ItemType = "File",
        Size = 42,
        ContentHash = "hash-remote",
        ProviderVersion = 2,
        Trashed = false,
        ObservedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
    };
    static BaseSnapshotRecord BaseSnapshot() => new()
    {
        TrackedItemId = "item-1",
        ExistsFlag = true,
        ItemType = "File",
        Name = "File.txt",
        LocalRelativePath = "File.txt",
        RemoteParentId = "remote-root",
        Size = 42,
        ContentHash = "hash-base",
        ProviderVersion = 1,
        Trashed = false,
        CommittedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
    };
    static SyncExecutionRequest Request(SyncPlanDecisionKind DecisionKind) => new()
    {
        Decision = Decision(DecisionKind),
        TrackedItem = TrackedItem(),
        BaseSnapshot = BaseSnapshot(),
        LocalObservation = LocalObservation(),
        RemoteObservation = RemoteObservation(),
    };

    // ● public

    /// <summary>
    /// Verifies upload intents are dispatched to upload execution.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncDispatchesUpload()
    {
        RecordingMutationExecutor Executor = new();

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.UploadToRemote)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        Assert.Equal(SyncExecutionIntentKind.UploadToRemote, Executor.ExecutedIntentKind);
    }

    /// <summary>
    /// Verifies download intents are dispatched to download execution.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncDispatchesDownload()
    {
        RecordingMutationExecutor Executor = new();

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.DownloadToLocal)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        Assert.Equal(SyncExecutionIntentKind.DownloadToLocal, Executor.ExecutedIntentKind);
    }

    /// <summary>
    /// Verifies default mutation executor blocks unimplemented executable intents.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncBlocksDefaultImplementation()
    {
        SyncMutationExecutorBase Executor = new(new TestLocalEndpoint(), new TestRemoteEndpoint());

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.UploadToRemote)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.Blocked, Results[0].ResultKind);
        Assert.Contains("Upload execution is not implemented.", Results[0].Message);
    }
}
