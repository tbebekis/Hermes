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

        /// <inheritdoc/>
        public Task<Result> MoveFileAsync(string SourceRelativePath, string TargetRelativePath, CancellationToken CancellationToken)
        {
            MovedFiles.Add((SourceRelativePath, TargetRelativePath));
            return Task.FromResult(Result.Success());
        }
        /// <inheritdoc/>
        public Task<Result> MoveDirectoryAsync(string SourceRelativePath, string TargetRelativePath, CancellationToken CancellationToken)
        {
            MovedDirectories.Add((SourceRelativePath, TargetRelativePath));
            return Task.FromResult(Result.Success());
        }

        // ● properties

        /// <summary>
        /// Gets moved files.
        /// </summary>
        public List<(string SourceRelativePath, string TargetRelativePath)> MovedFiles { get; } = new();
        /// <summary>
        /// Gets moved directories.
        /// </summary>
        public List<(string SourceRelativePath, string TargetRelativePath)> MovedDirectories { get; } = new();
    }

    /// <summary>
    /// Test remote mutation endpoint.
    /// </summary>
    class TestRemoteEndpoint : IRemoteSyncMutationEndpoint
    {
        // ● private

        readonly StorageResult<StorageItem> fDownloadResult;
        readonly StorageResult<StorageItem> fDeleteResult;

        static StorageItem Item() => new("remote-1", "remote-root", "File.txt", "/File.txt", StorageItemKind.File);

        // ● constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRemoteEndpoint"/> class.
        /// </summary>
        public TestRemoteEndpoint()
            : this(StorageResult<StorageItem>.Success(Item()), StorageResult<StorageItem>.Success(Item()))
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TestRemoteEndpoint"/> class.
        /// </summary>
        public TestRemoteEndpoint(StorageResult<StorageItem> DeleteResult)
            : this(StorageResult<StorageItem>.Success(Item()), DeleteResult)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TestRemoteEndpoint"/> class.
        /// </summary>
        public TestRemoteEndpoint(StorageResult<StorageItem> DownloadResult, StorageResult<StorageItem> DeleteResult)
        {
            fDownloadResult = Guard.NotNull(DownloadResult, nameof(DownloadResult));
            fDeleteResult = Guard.NotNull(DeleteResult, nameof(DeleteResult));
        }

        // ● public

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> CreateFolderAsync(string Name, string ParentId, CancellationToken CancellationToken)
        {
            CreatedFolderNames.Add(Name);
            CreatedFolderParentIds.Add(ParentId);
            return Task.FromResult(CreateFolderResult);
        }

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> UploadFileAsync(string LocalFilePath, string ParentId, CancellationToken CancellationToken)
        {
            UploadedLocalFilePaths.Add(LocalFilePath);
            UploadedParentIds.Add(ParentId);
            return Task.FromResult(UploadFileResult);
        }

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> UpdateFileContentAsync(string RemoteItemId, string LocalFilePath, CancellationToken CancellationToken)
        {
            UpdatedItemIds.Add(RemoteItemId);
            UpdatedLocalFilePaths.Add(LocalFilePath);
            return Task.FromResult(UpdateFileContentResult);
        }

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> DownloadFileAsync(string RemoteItemId, string LocalFilePath, CancellationToken CancellationToken)
        {
            DownloadedItemIds.Add(RemoteItemId);
            DownloadedLocalFilePaths.Add(LocalFilePath);
            return Task.FromResult(fDownloadResult);
        }

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> RenameItemAsync(string RemoteItemId, string Name, CancellationToken CancellationToken)
        {
            RenamedItemIds.Add(RemoteItemId);
            RenamedNames.Add(Name);
            return Task.FromResult(RenameItemResult);
        }

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> MoveItemAsync(string RemoteItemId, string OldParentId, string NewParentId, CancellationToken CancellationToken)
        {
            MovedItemIds.Add(RemoteItemId);
            MovedOldParentIds.Add(OldParentId);
            MovedNewParentIds.Add(NewParentId);
            return Task.FromResult(MoveItemResult);
        }

        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> DeleteItemAsync(string RemoteItemId, CancellationToken CancellationToken)
        {
            DeletedItemIds.Add(RemoteItemId);
            return Task.FromResult(fDeleteResult);
        }

        // ● properties

        /// <summary>
        /// Gets the created folder names.
        /// </summary>
        public List<string> CreatedFolderNames { get; } = [];
        /// <summary>
        /// Gets the created folder parent ids.
        /// </summary>
        public List<string> CreatedFolderParentIds { get; } = [];
        /// <summary>
        /// Gets the uploaded local file paths.
        /// </summary>
        public List<string> UploadedLocalFilePaths { get; } = [];
        /// <summary>
        /// Gets the uploaded parent ids.
        /// </summary>
        public List<string> UploadedParentIds { get; } = [];
        /// <summary>
        /// Gets the updated item ids.
        /// </summary>
        public List<string> UpdatedItemIds { get; } = [];
        /// <summary>
        /// Gets the updated local file paths.
        /// </summary>
        public List<string> UpdatedLocalFilePaths { get; } = [];
        /// <summary>
        /// Gets the downloaded item ids.
        /// </summary>
        public List<string> DownloadedItemIds { get; } = [];
        /// <summary>
        /// Gets the downloaded local file paths.
        /// </summary>
        public List<string> DownloadedLocalFilePaths { get; } = [];
        /// <summary>
        /// Gets the renamed item ids.
        /// </summary>
        public List<string> RenamedItemIds { get; } = [];
        /// <summary>
        /// Gets the renamed item names.
        /// </summary>
        public List<string> RenamedNames { get; } = [];
        /// <summary>
        /// Gets the moved item ids.
        /// </summary>
        public List<string> MovedItemIds { get; } = [];
        /// <summary>
        /// Gets the moved source parent ids.
        /// </summary>
        public List<string> MovedOldParentIds { get; } = [];
        /// <summary>
        /// Gets the moved target parent ids.
        /// </summary>
        public List<string> MovedNewParentIds { get; } = [];
        /// <summary>
        /// Gets the deleted item ids.
        /// </summary>
        public List<string> DeletedItemIds { get; } = [];
        /// <summary>
        /// Gets or sets the create folder result.
        /// </summary>
        public StorageResult<StorageItem> CreateFolderResult { get; set; } = StorageResult<StorageItem>.Success(Item());
        /// <summary>
        /// Gets or sets the upload file result.
        /// </summary>
        public StorageResult<StorageItem> UploadFileResult { get; set; } = StorageResult<StorageItem>.Success(Item());
        /// <summary>
        /// Gets or sets the update file content result.
        /// </summary>
        public StorageResult<StorageItem> UpdateFileContentResult { get; set; } = StorageResult<StorageItem>.Success(Item());
        /// <summary>
        /// Gets or sets the rename item result.
        /// </summary>
        public StorageResult<StorageItem> RenameItemResult { get; set; } = StorageResult<StorageItem>.Success(Item());
        /// <summary>
        /// Gets or sets the move item result.
        /// </summary>
        public StorageResult<StorageItem> MoveItemResult { get; set; } = StorageResult<StorageItem>.Success(Item());
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
        protected override Task<SyncExecutionResult> ExecuteApplyRemoteNamespaceToLocalAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
        {
            ExecutedIntentKind = Intent.IntentKind;
            return Task.FromResult(SyncExecutionResultFactory.Completed(Intent.Request, Intent.LocalRelativePath));
        }

        /// <inheritdoc/>
        protected override Task<SyncExecutionResult> ExecuteApplyLocalNamespaceToRemoteAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
        {
            ExecutedIntentKind = Intent.IntentKind;
            return Task.FromResult(SyncExecutionResultFactory.Completed(Intent.Request, Intent.LocalRelativePath));
        }

        /// <inheritdoc/>
        protected override Task<SyncExecutionResult> ExecutePropagateLocalDeleteAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
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
    static SyncRootRecord SyncRoot() => new()
    {
        Id = "root-1",
        ProviderName = "GoogleDrive",
        LocalRootPath = "/local",
        RemoteRootItemId = "remote-root",
        IsEnabled = true,
        CreatedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
    };
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
        SyncRoot = SyncRoot(),
        TrackedItem = TrackedItem(),
        BaseSnapshot = BaseSnapshot(),
        LocalObservation = LocalObservation(),
        RemoteObservation = RemoteObservation(),
    };
    static SyncExecutionRequest LocalRenameRequest() => new()
    {
        Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalNamespaceChanged, SyncPlanDecisionKind.ApplyLocalNamespaceToRemote),
        SyncRoot = SyncRoot(),
        TrackedItem = TrackedItem(),
        BaseSnapshot = BaseSnapshot(),
        LocalObservation = new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "Renamed.txt",
            Name = "Renamed.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ObservedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
        },
        RemoteObservation = RemoteObservation(),
    };
    static SyncExecutionRequest LocalMoveRequest() => new()
    {
        Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalNamespaceChanged, SyncPlanDecisionKind.ApplyLocalNamespaceToRemote),
        SyncRoot = SyncRoot(),
        TrackedItem = TrackedItem(),
        BaseSnapshot = BaseSnapshot(),
        LocalObservation = new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "Folder/File.txt",
            Name = "File.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ObservedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
        },
        RemoteObservation = RemoteObservation(),
        LocalParentRemoteItemId = "remote-folder",
    };
    static SyncExecutionRequest LocalRenameAndMoveRequest() => new()
    {
        Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalNamespaceChanged, SyncPlanDecisionKind.ApplyLocalNamespaceToRemote),
        SyncRoot = SyncRoot(),
        TrackedItem = TrackedItem(),
        BaseSnapshot = BaseSnapshot(),
        LocalObservation = new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "Folder/Renamed.txt",
            Name = "Renamed.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ObservedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
        },
        RemoteObservation = RemoteObservation(),
        LocalParentRemoteItemId = "remote-folder",
    };
    static SyncExecutionRequest FolderRequest(SyncPlanDecisionKind DecisionKind) => new()
    {
        Decision = new SyncPlanDecision("folder-1", SyncDiffKind.LocalChanged, DecisionKind),
        SyncRoot = SyncRoot(),
        TrackedItem = new TrackedItemRecord()
        {
            Id = "folder-1",
            SyncRootId = "root-1",
            RemoteItemId = "remote-folder-1",
            LocalKey = "Folder",
            ItemType = "Folder",
        },
        BaseSnapshot = new BaseSnapshotRecord()
        {
            TrackedItemId = "folder-1",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Folder",
            LocalRelativePath = "Folder",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
        },
        LocalObservation = new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "folder-1",
            ExistsFlag = true,
            RelativePath = "Folder",
            Name = "Folder",
            ItemType = "Folder",
            ObservedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
        },
        RemoteObservation = new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-1",
            RemoteItemId = "remote-folder-1",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            Trashed = false,
            ObservedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
        },
    };
    static SyncExecutionRequest NewFileUploadRequest() => new()
    {
        Decision = Decision(SyncPlanDecisionKind.UploadToRemote),
        TrackedItem = new TrackedItemRecord()
        {
            Id = "new-file-1",
            SyncRootId = "root-1",
            RemoteItemId = string.Empty,
            LocalKey = "NewFile.txt",
            ItemType = "File",
        },
        BaseSnapshot = new BaseSnapshotRecord()
        {
            TrackedItemId = "new-file-1",
            ExistsFlag = false,
            ItemType = "File",
            Name = "NewFile.txt",
            LocalRelativePath = "NewFile.txt",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
        },
        LocalObservation = new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "new-file-1",
            ExistsFlag = true,
            RelativePath = "NewFile.txt",
            Name = "NewFile.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ObservedTime = new DateTime(2026, 7, 11, 10, 20, 0, DateTimeKind.Utc),
        },
        RemoteObservation = null,
    };

    /// <summary>
    /// Provides a temporary folder for mutation executor tests.
    /// </summary>
    sealed class TempFolder : IDisposable
    {
        // ● fields

        bool fDisposed;

        // ● constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TempFolder"/> class.
        /// </summary>
        public TempFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hermes-mutation-executor-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        // ● public

        /// <summary>
        /// Deletes the temporary folder.
        /// </summary>
        public void Dispose()
        {
            if (fDisposed)
                return;

            if (Directory.Exists(Path))
                Directory.Delete(Path, true);

            fDisposed = true;
        }

        // ● properties

        /// <summary>
        /// Gets the temporary folder path.
        /// </summary>
        public string Path { get; }
    }

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
    /// Verifies remote namespace intents are dispatched to local namespace execution.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncDispatchesRemoteNamespaceToLocal()
    {
        RecordingMutationExecutor Executor = new();

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.ApplyRemoteNamespaceToLocal)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        Assert.Equal(SyncExecutionIntentKind.ApplyRemoteNamespaceToLocal, Executor.ExecutedIntentKind);
    }

    /// <summary>
    /// Verifies local namespace intents are dispatched to remote namespace execution.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncDispatchesLocalNamespaceToRemote()
    {
        RecordingMutationExecutor Executor = new();

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [LocalRenameRequest()],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        Assert.Equal(SyncExecutionIntentKind.ApplyLocalNamespaceToRemote, Executor.ExecutedIntentKind);
    }

    /// <summary>
    /// Verifies local file rename intents rename remote items.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncAppliesLocalFileRenameToRemoteItem()
    {
        TestRemoteEndpoint RemoteEndpoint = new();
        SyncMutationExecutorBase Executor = new(new TestLocalEndpoint(), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [LocalRenameRequest()],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        Assert.Equal("remote-1", Assert.Single(RemoteEndpoint.RenamedItemIds));
        Assert.Equal("Renamed.txt", Assert.Single(RemoteEndpoint.RenamedNames));
    }

    /// <summary>
    /// Verifies local file move intents move remote items.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncAppliesLocalFileMoveToRemoteItem()
    {
        TestRemoteEndpoint RemoteEndpoint = new();
        SyncMutationExecutorBase Executor = new(new TestLocalEndpoint(), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [LocalMoveRequest()],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        Assert.Equal("remote-1", Assert.Single(RemoteEndpoint.MovedItemIds));
        Assert.Equal("remote-root", Assert.Single(RemoteEndpoint.MovedOldParentIds));
        Assert.Equal("remote-folder", Assert.Single(RemoteEndpoint.MovedNewParentIds));
    }

    /// <summary>
    /// Verifies combined local file rename and move intents rename then move remote items.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncAppliesLocalFileRenameAndMoveToRemoteItem()
    {
        TestRemoteEndpoint RemoteEndpoint = new();
        SyncMutationExecutorBase Executor = new(new TestLocalEndpoint(), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [LocalRenameAndMoveRequest()],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        Assert.Equal("remote-1", Assert.Single(RemoteEndpoint.RenamedItemIds));
        Assert.Equal("Renamed.txt", Assert.Single(RemoteEndpoint.RenamedNames));
        Assert.Equal("remote-1", Assert.Single(RemoteEndpoint.MovedItemIds));
        Assert.Equal("remote-root", Assert.Single(RemoteEndpoint.MovedOldParentIds));
        Assert.Equal("remote-folder", Assert.Single(RemoteEndpoint.MovedNewParentIds));
    }

    /// <summary>
    /// Verifies remote folder namespace intents move local directories.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncAppliesRemoteFolderNamespaceToLocalDirectory()
    {
        TestLocalEndpoint LocalEndpoint = new();
        SyncMutationExecutorBase Executor = new(LocalEndpoint, new TestRemoteEndpoint());
        SyncExecutionRequest Request = FolderRequest(SyncPlanDecisionKind.ApplyRemoteNamespaceToLocal);
        Request.RemoteObservation.Name = "RenamedFolder";

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        (string SourceRelativePath, string TargetRelativePath) Move = Assert.Single(LocalEndpoint.MovedDirectories);
        Assert.Equal("Folder", Move.SourceRelativePath);
        Assert.Equal("RenamedFolder", Move.TargetRelativePath);
    }

    /// <summary>
    /// Verifies upload propagation updates an existing remote file.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncUpdatesExistingRemoteFile()
    {
        TestRemoteEndpoint RemoteEndpoint = new();
        SyncMutationExecutorBase Executor = new(new TestLocalEndpoint(), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.UploadToRemote)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        string UpdatedItemId = Assert.Single(RemoteEndpoint.UpdatedItemIds);
        string UpdatedLocalFilePath = Assert.Single(RemoteEndpoint.UpdatedLocalFilePaths);
        Assert.Equal("remote-1", UpdatedItemId);
        Assert.Equal("/local/File.txt", UpdatedLocalFilePath);
    }

    /// <summary>
    /// Verifies upload propagation creates a new remote file when no remote item id exists.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncUploadsNewRemoteFile()
    {
        TestRemoteEndpoint RemoteEndpoint = new();
        SyncMutationExecutorBase Executor = new(new TestLocalEndpoint(), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [NewFileUploadRequest()],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        string UploadedLocalFilePath = Assert.Single(RemoteEndpoint.UploadedLocalFilePaths);
        string UploadedParentId = Assert.Single(RemoteEndpoint.UploadedParentIds);
        Assert.Equal("/local/NewFile.txt", UploadedLocalFilePath);
        Assert.Equal("remote-root", UploadedParentId);
    }

    /// <summary>
    /// Verifies upload propagation creates a remote folder.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncCreatesRemoteFolder()
    {
        TestRemoteEndpoint RemoteEndpoint = new();
        SyncMutationExecutorBase Executor = new(new TestLocalEndpoint(), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [FolderRequest(SyncPlanDecisionKind.UploadToRemote)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        string CreatedFolderName = Assert.Single(RemoteEndpoint.CreatedFolderNames);
        string CreatedFolderParentId = Assert.Single(RemoteEndpoint.CreatedFolderParentIds);
        Assert.Equal("Folder", CreatedFolderName);
        Assert.Equal("remote-root", CreatedFolderParentId);
    }

    /// <summary>
    /// Verifies upload propagation maps remote storage failures.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncMapsRemoteUploadFailure()
    {
        StorageError Error = new(StorageErrorKind.PermissionDenied, "denied");
        TestRemoteEndpoint RemoteEndpoint = new()
        {
            UpdateFileContentResult = StorageResult<StorageItem>.Failure(Error),
        };
        SyncMutationExecutorBase Executor = new(new TestLocalEndpoint(), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.UploadToRemote)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.Blocked, Results[0].ResultKind);
        Assert.Same(Error, Results[0].Error);
        Assert.Equal("denied", Results[0].Message);
    }

    /// <summary>
    /// Verifies download propagation downloads the remote file to the resolved local path.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncDownloadsRemoteFileToLocal()
    {
        using TempFolder Folder = new();
        TestRemoteEndpoint RemoteEndpoint = new();
        SyncMutationExecutorBase Executor = new(new LocalSyncMutationEndpoint(Folder.Path), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.DownloadToLocal)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        string DownloadedItemId = Assert.Single(RemoteEndpoint.DownloadedItemIds);
        string DownloadedLocalFilePath = Assert.Single(RemoteEndpoint.DownloadedLocalFilePaths);
        Assert.Equal("remote-1", DownloadedItemId);
        Assert.Equal(System.IO.Path.Combine(Folder.Path, "File.txt"), DownloadedLocalFilePath);
    }

    /// <summary>
    /// Verifies remote folder download creates the local directory.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncCreatesLocalFolderForRemoteFolderDownload()
    {
        using TempFolder Folder = new();
        TestRemoteEndpoint RemoteEndpoint = new();
        SyncMutationExecutorBase Executor = new(new LocalSyncMutationEndpoint(Folder.Path), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [FolderRequest(SyncPlanDecisionKind.DownloadToLocal)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        Assert.True(Directory.Exists(System.IO.Path.Combine(Folder.Path, "Folder")));
        Assert.Empty(RemoteEndpoint.DownloadedItemIds);
    }

    /// <summary>
    /// Verifies download propagation maps remote storage failures.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncMapsRemoteDownloadFailure()
    {
        StorageError Error = new(StorageErrorKind.RateLimited, "rate limited");
        TestRemoteEndpoint RemoteEndpoint = new(StorageResult<StorageItem>.Failure(Error), StorageResult<StorageItem>.Success(new StorageItem("delete-1", "remote-root", "File.txt", "/File.txt", StorageItemKind.File)));
        SyncMutationExecutorBase Executor = new(new TestLocalEndpoint(), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.DownloadToLocal)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.FailedRetryable, Results[0].ResultKind);
        Assert.Same(Error, Results[0].Error);
        Assert.Equal("rate limited", Results[0].Message);
    }

    /// <summary>
    /// Verifies remote delete propagation deletes the local item.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncPropagatesRemoteDeleteToLocal()
    {
        using TempFolder Folder = new();
        string FilePath = System.IO.Path.Combine(Folder.Path, "File.txt");
        await System.IO.File.WriteAllTextAsync(FilePath, "content");
        SyncMutationExecutorBase Executor = new(new LocalSyncMutationEndpoint(Folder.Path), new TestRemoteEndpoint());

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.PropagateRemoteDelete)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        Assert.False(System.IO.File.Exists(FilePath));
    }

    /// <summary>
    /// Verifies local delete propagation deletes the remote item.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncPropagatesLocalDeleteToRemote()
    {
        TestRemoteEndpoint RemoteEndpoint = new();
        SyncMutationExecutorBase Executor = new(new TestLocalEndpoint(), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.PropagateLocalDelete)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.CompletedAndVerified, Results[0].ResultKind);
        string DeletedItemId = Assert.Single(RemoteEndpoint.DeletedItemIds);
        Assert.Equal("remote-1", DeletedItemId);
    }

    /// <summary>
    /// Verifies local delete propagation maps remote storage failures.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncMapsRemoteDeleteFailure()
    {
        StorageError Error = new(StorageErrorKind.PermissionDenied, "denied");
        TestRemoteEndpoint RemoteEndpoint = new(StorageResult<StorageItem>.Failure(Error));
        SyncMutationExecutorBase Executor = new(new TestLocalEndpoint(), RemoteEndpoint);

        IReadOnlyList<SyncExecutionResult> Results = await Executor.ExecuteAsync(
            [Request(SyncPlanDecisionKind.PropagateLocalDelete)],
            CancellationToken.None);

        Assert.Single(Results);
        Assert.Equal(SyncExecutionResultKind.Blocked, Results[0].ResultKind);
        Assert.Same(Error, Results[0].Error);
        Assert.Equal("denied", Results[0].Message);
    }
}
