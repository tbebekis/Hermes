// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests metadata synchronization session behavior.
/// </summary>
public class MetadataSyncSessionTests
{
    // ● private

    /// <summary>
    /// Provides an isolated SQLite database for metadata sync session tests.
    /// </summary>
    sealed class TestDatabase : IDisposable
    {
        // ● fields

        readonly string fFolder;
        readonly string fDatabasePath;
        bool fDisposed;

        // ● private

        void ConfigureApplication()
        {
            SysConfig.ApplicationMode = ApplicationMode.Service;
            SysConfig.MainAssembly = typeof(MetadataSyncSessionTests).Assembly;
            SysConfig.AppFolderPath = fFolder;
            SysConfig.AppDataFolderPath = fFolder;
            SysConfig.AppTempFolderPath = fFolder;

            DbConfig.DefaultConnectionName = Sys.DEFAULT;
            Db.Connections.List.Clear();
            Db.Connections.List.Add(new DbConnectionInfo()
            {
                Name = Sys.DEFAULT,
                DbServerType = DbServerType.Sqlite,
                ConnectionString = $@"Data Source=""{fDatabasePath}""",
            });
        }
        void CreateDatabase()
        {
            DbConnectionInfo ConnectionInfo = Db.GetDefaultConnectionInfo();
            ConnectionInfo.GetSqlProvider().CreateDatabase(ConnectionInfo.ConnectionString);

            Registry.RegisterSchemas();
            Schemas.Execute();
            Store = SqlStores.CreateSqlStore(ConnectionInfo);
        }

        // ● constructor

        public TestDatabase()
        {
            fFolder = Path.Combine(Path.GetTempPath(), "hermes-metadata-session-tests", Sys.GenId());
            fDatabasePath = Path.Combine(fFolder, "Hermes.db3");

            Directory.CreateDirectory(fFolder);
            ConfigureApplication();
            CreateDatabase();
        }

        // ● public

        public void Dispose()
        {
            if (fDisposed)
                return;

            System.Data.SQLite.SQLiteConnection.ClearAllPools();

            if (Directory.Exists(fFolder))
                Directory.Delete(fFolder, true);

            fDisposed = true;
        }

        // ● properties

        public SqlStore Store { get; private set; }
    }
    /// <summary>
    /// Executes requests in tests by invoking a supplied callback.
    /// </summary>
    sealed class FakeSyncExecutor : SyncExecutorBase
    {
        // ● fields

        readonly Action<SyncExecutionRequest> fExecuteRequest;
        readonly SyncExecutionResultKind fResultKind;

        // ● constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeSyncExecutor"/> class.
        /// </summary>
        public FakeSyncExecutor(Action<SyncExecutionRequest> ExecuteRequest)
            : this(ExecuteRequest, SyncExecutionResultKind.CompletedAndVerified)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FakeSyncExecutor"/> class.
        /// </summary>
        public FakeSyncExecutor(Action<SyncExecutionRequest> ExecuteRequest, SyncExecutionResultKind ResultKind)
        {
            fExecuteRequest = ExecuteRequest ?? throw new ArgumentNullException(nameof(ExecuteRequest));
            fResultKind = ResultKind;
        }

        // ● protected

        /// <summary>
        /// Executes a synchronization intent by invoking the configured callback.
        /// </summary>
        protected override Task<SyncExecutionResult> ExecuteIntentAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
        {
            Intents.Add(Intent);
            Requests.Add(Intent.Request);
            fExecuteRequest(Intent.Request);

            return Task.FromResult(new SyncExecutionResult()
            {
                Request = Intent.Request,
                ResultKind = fResultKind,
            });
        }

        // ● properties

        /// <summary>
        /// Gets the intents created by the fake executor.
        /// </summary>
        public List<SyncExecutionIntent> Intents { get; } = new();

        /// <summary>
        /// Gets the requests received by the fake executor.
        /// </summary>
        public List<SyncExecutionRequest> Requests { get; } = new();
    }
    static SyncRootRecord CreateSyncRoot() => new()
    {
        Id = "root-1",
        ProviderName = "GoogleDrive",
        ConnectionId = "account-1",
        LocalRootPath = "/tmp/hermes",
        RemoteRootItemId = "remote-root",
        IsEnabled = true,
        CreatedTime = new DateTime(2026, 7, 11, 6, 0, 0, DateTimeKind.Utc),
    };
    static TrackedItemRecord CreateTrackedItem(string Id, string RemoteItemId, string LocalKey) => new()
    {
        Id = Id,
        SyncRootId = "root-1",
        RemoteItemId = RemoteItemId,
        LocalKey = LocalKey,
        ItemType = "File",
    };
    static void AddObservedItem(SqlMetadataStore Store, string TrackedItemId, string RemoteItemId, string Name, string Hash, DateTime Time)
    {
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = TrackedItemId,
            ExistsFlag = true,
            RelativePath = Name,
            Name = Name,
            ItemType = "File",
            Size = 42,
            ContentHash = Hash,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = TrackedItemId,
            RemoteItemId = RemoteItemId,
            ExistsFlag = true,
            Removed = false,
            Name = Name,
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = Hash,
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
    }
    static void AddBaseSnapshot(SqlMetadataStore Store, string TrackedItemId, string Name, string Hash, DateTime Time)
    {
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = TrackedItemId,
            ExistsFlag = true,
            ItemType = "File",
            Name = Name,
            LocalRelativePath = Name,
            RemoteParentId = "remote-root",
            Size = 42,
            ContentHash = Hash,
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
    }
    static RemoteCheckpointRecord CreateCheckpoint(string Token, DateTime Time) => new()
    {
        SyncRootId = "root-1",
        ProviderName = "GoogleDrive",
        ConnectionId = "account-1",
        StartPageToken = Token,
        UpdatedTime = Time,
    };
    static LocalScanItem CreateLocalScanItem(string Name, string Hash, DateTime Time) => new()
    {
        RelativePath = Name,
        Name = Name,
        ItemType = "File",
        Size = 42,
        ContentHash = Hash,
        ModifiedTime = Time,
    };
    static StorageItem CreateStorageItem(string RemoteItemId, string Name, string Hash, long Version) => new(
        RemoteItemId,
        "remote-root",
        Name,
        "/" + Name,
        StorageItemKind.File,
        "text/plain",
        42,
        Hash,
        default,
        default,
        Version,
        false);
    static LocalScanItem CreateLocalFolderScanItem(string Name, DateTime Time) => new()
    {
        RelativePath = Name,
        Name = Name,
        ItemType = "Folder",
        ModifiedTime = Time,
    };
    static LocalScanItem CreateNestedLocalScanItem(string RelativePath, string Name, string ParentRelativePath, string Hash, DateTime Time) => new()
    {
        RelativePath = RelativePath,
        Name = Name,
        ParentRelativePath = ParentRelativePath,
        ItemType = "File",
        Size = 5,
        ContentHash = Hash,
        ModifiedTime = Time,
    };
    static StorageItem CreateStorageFolder(string RemoteItemId, string Name, long Version) => new(
        RemoteItemId,
        "remote-root",
        Name,
        "/" + Name,
        StorageItemKind.Folder,
        "application/vnd.google-apps.folder",
        0,
        string.Empty,
        default,
        default,
        Version,
        false);
    static SyncExecutionRequest CreateExecutionRequest(SyncPlanDecisionKind DecisionKind) => new()
    {
        Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalChanged, DecisionKind),
        TrackedItem = CreateTrackedItem("item-1", "remote-1", "File1.txt"),
        BaseSnapshot = new BaseSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            ItemType = "File",
            Name = "File1.txt",
            LocalRelativePath = "File1.txt",
            RemoteParentId = "remote-root",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = new DateTime(2026, 7, 11, 8, 35, 0, DateTimeKind.Utc),
        },
        LocalObservation = new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "File1.txt",
            Name = "File1.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ObservedTime = new DateTime(2026, 7, 11, 8, 35, 0, DateTimeKind.Utc),
        },
        RemoteObservation = new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-remote",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = new DateTime(2026, 7, 11, 8, 35, 0, DateTimeKind.Utc),
        },
    };

    // ● public

    /// <summary>
    /// Verifies metadata sync session imports local scan items.
    /// </summary>
    [Fact]
    public void ImportLocalScanCreatesTrackedItemsAndObservations()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 10, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());

        LocalScanImportResult Result = Session.ImportLocalScan(
            "root-1",
            [
                new LocalScanItem()
                {
                    RelativePath = "Local.txt",
                    Name = "Local.txt",
                    ItemType = "File",
                    Size = 42,
                    ContentHash = "hash-local",
                    ModifiedTime = Time,
                },
            ],
            Time,
            "scan-1");
        TrackedItemRecord TrackedItem = Store.GetTrackedItemByLocalKey("root-1", "Local.txt");

        Assert.Single(Result.CreatedTrackedItems);
        Assert.NotNull(TrackedItem);
        Assert.Equal("Local.txt", Store.GetLocalObservation(TrackedItem.Id).RelativePath);
    }
    /// <summary>
    /// Verifies local scan import repairs a missing tracked item local key from existing observations.
    /// </summary>
    [Fact]
    public void ImportLocalScanRepairsMissingTrackedLocalKey()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 12, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", null));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "RemoteOnly.txt",
            Name = "RemoteOnly.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-remote",
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            ItemType = "File",
            Name = "RemoteOnly.txt",
            LocalRelativePath = "RemoteOnly.txt",
            RemoteParentId = "remote-root",
            Size = 42,
            ContentHash = "hash-remote",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });

        LocalScanImportResult Result = Session.ImportLocalScan(
            "root-1",
            [CreateLocalScanItem("RemoteOnly.txt", "hash-remote", Time)],
            Time,
            "scan-repair");

        Assert.Empty(Result.CreatedTrackedItems);
        Assert.Single(Store.GetTrackedItems("root-1"));
        Assert.Equal("RemoteOnly.txt", Store.GetTrackedItem("item-1").LocalKey);
        Assert.Equal("scan-repair", Store.GetLocalObservation("item-1").ScanId);
    }
    /// <summary>
    /// Verifies local scan import keeps identity when a tracked file is renamed locally.
    /// </summary>
    [Fact]
    public void ImportLocalScanAdoptsTrackedFileRename()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 14, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-1", Time);
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-1", Time);

        LocalScanImportResult ImportResult = Session.ImportLocalScan(
            "root-1",
            [CreateLocalScanItem("Renamed.txt", "hash-1", Time)],
            Time,
            "scan-rename");
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);
        SyncExecutionRequest Request = SessionResult.PendingExecutionRequests.Single(Item => Item.Decision.TrackedItemId == "item-1");

        Assert.Empty(ImportResult.CreatedTrackedItems);
        Assert.Single(Store.GetTrackedItems("root-1"));
        Assert.Equal("Renamed.txt", Store.GetTrackedItem("item-1").LocalKey);
        Assert.Equal("Renamed.txt", Store.GetLocalObservation("item-1").RelativePath);
        Assert.Equal(SyncPlanDecisionKind.ApplyLocalNamespaceToRemote, Request.Decision.DecisionKind);
    }
    /// <summary>
    /// Verifies local scan import keeps identity when a tracked file is moved locally.
    /// </summary>
    [Fact]
    public void ImportLocalScanAdoptsTrackedFileMove()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 14, 15, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "folder-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-folder",
            LocalKey = "Folder",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            RelativePath = "Folder",
            Name = "Folder",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Folder",
            LocalRelativePath = "Folder",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = Time,
        });
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-1", Time);
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-1", Time);

        LocalScanImportResult ImportResult = Session.ImportLocalScan(
            "root-1",
            [
                CreateLocalFolderScanItem("Folder", Time),
                new LocalScanItem()
                {
                    RelativePath = "Folder/File1.txt",
                    Name = "File1.txt",
                    ParentRelativePath = "Folder",
                    ItemType = "File",
                    Size = 42,
                    ContentHash = "hash-1",
                    ModifiedTime = Time,
                },
            ],
            Time,
            "scan-move");
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);
        SyncExecutionRequest Request = SessionResult.PendingExecutionRequests.Single(Item => Item.Decision.TrackedItemId == "item-1");

        Assert.Empty(ImportResult.CreatedTrackedItems);
        Assert.Equal(2, Store.GetTrackedItems("root-1").Count);
        Assert.Equal("Folder/File1.txt", Store.GetTrackedItem("item-1").LocalKey);
        Assert.Equal("Folder/File1.txt", Store.GetLocalObservation("item-1").RelativePath);
        Assert.Equal("remote-folder", Request.LocalParentRemoteItemId);
        Assert.Equal(SyncPlanDecisionKind.ApplyLocalNamespaceToRemote, Request.Decision.DecisionKind);
    }
    /// <summary>
    /// Verifies local file rename execution updates remote observation before base commit.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresLocalRenameRemoteItemBeforeBaseCommit()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 14, 30, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-1", Time);
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-1", Time);
        Session.ImportLocalScan(
            "root-1",
            [CreateLocalScanItem("Renamed.txt", "hash-1", Time)],
            Time,
            "scan-rename");
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);
        SyncExecutionRequest Request = SessionResult.PendingExecutionRequests.Single(Item => Item.Decision.TrackedItemId == "item-1");

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = Request,
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    RemoteItem = new StorageItem(
                        "remote-1",
                        "remote-root",
                        "Renamed.txt",
                        "/Renamed.txt",
                        StorageItemKind.File,
                        "text/plain",
                        42,
                        "hash-1",
                        default,
                        default,
                        2,
                        false),
                    LocalRelativePath = "Renamed.txt",
                },
            ],
            Time);

        Assert.Single(Result.CommittedResults);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.Equal("Renamed.txt", Store.GetRemoteObservation("item-1").Name);
        Assert.Equal("Renamed.txt", Store.GetBaseSnapshot("item-1").Name);
        Assert.Equal("Renamed.txt", Store.GetBaseSnapshot("item-1").LocalRelativePath);
    }
    /// <summary>
    /// Verifies local file move execution updates remote parent before base commit.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresLocalMoveRemoteItemBeforeBaseCommit()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 14, 45, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "folder-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-folder",
            LocalKey = "Folder",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            RelativePath = "Folder",
            Name = "Folder",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Folder",
            LocalRelativePath = "Folder",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = Time,
        });
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-1", Time);
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-1", Time);
        Session.ImportLocalScan(
            "root-1",
            [
                CreateLocalFolderScanItem("Folder", Time),
                new LocalScanItem()
                {
                    RelativePath = "Folder/File1.txt",
                    Name = "File1.txt",
                    ParentRelativePath = "Folder",
                    ItemType = "File",
                    Size = 42,
                    ContentHash = "hash-1",
                    ModifiedTime = Time,
                },
            ],
            Time,
            "scan-move");
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);
        SyncExecutionRequest Request = SessionResult.PendingExecutionRequests.Single(Item => Item.Decision.TrackedItemId == "item-1");

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = Request,
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    RemoteItem = new StorageItem(
                        "remote-1",
                        "remote-folder",
                        "File1.txt",
                        "/Folder/File1.txt",
                        StorageItemKind.File,
                        "text/plain",
                        42,
                        "hash-1",
                        default,
                        default,
                        2,
                        false),
                    LocalRelativePath = "Folder/File1.txt",
                },
            ],
            Time);

        Assert.Single(Result.CommittedResults);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.Equal("remote-folder", Store.GetRemoteObservation("item-1").RemoteParentId);
        Assert.Equal("remote-folder", Store.GetBaseSnapshot("item-1").RemoteParentId);
        Assert.Equal("Folder/File1.txt", Store.GetBaseSnapshot("item-1").LocalRelativePath);
    }
    /// <summary>
    /// Verifies metadata sync session imports remote snapshots.
    /// </summary>
    [Fact]
    public void ImportRemoteSnapshotStoresItemsAndCheckpoint()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 15, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());

        RemoteBootstrapResult Result = Session.ImportRemoteSnapshot(
            "root-1",
            [
                new StorageItem(
                    "remote-1",
                    "remote-root",
                    "Remote.txt",
                    "/Remote.txt",
                    StorageItemKind.File,
                    "text/plain",
                    42,
                    "hash-remote",
                    default,
                    default,
                    1,
                    false),
            ],
            new RemoteCheckpointRecord()
            {
                SyncRootId = "root-1",
                ProviderName = "GoogleDrive",
                ConnectionId = "account-1",
                StartPageToken = "token-1",
                UpdatedTime = Time,
            },
            Time);
        TrackedItemRecord TrackedItem = Store.GetTrackedItemByRemoteId("root-1", "remote-1");

        Assert.Single(Result.CreatedTrackedItems);
        Assert.NotNull(TrackedItem);
        Assert.Equal("Remote.txt", Store.GetRemoteObservation(TrackedItem.Id).Name);
        Assert.Equal("token-1", Store.GetRemoteCheckpoint("root-1").StartPageToken);
    }
    /// <summary>
    /// Verifies metadata sync session imports remote changes.
    /// </summary>
    [Fact]
    public void ImportRemoteChangesStoresImportableChangesAndCheckpoint()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 20, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());

        RemoteChangeImportResult Result = Session.ImportRemoteChanges(
            "root-1",
            [
                new StorageChange(
                    "remote-1",
                    false,
                    new DateTimeOffset(Time),
                    new StorageItem(
                        "remote-1",
                        "remote-root",
                        "RemoteChange.txt",
                        "/RemoteChange.txt",
                        StorageItemKind.File,
                        "text/plain",
                        42,
                        "hash-change",
                        default,
                        default,
                        1,
                        false)),
            ],
            new RemoteCheckpointRecord()
            {
                SyncRootId = "root-1",
                ProviderName = "GoogleDrive",
                ConnectionId = "account-1",
                StartPageToken = "token-2",
                UpdatedTime = Time,
            },
            Time);
        TrackedItemRecord TrackedItem = Store.GetTrackedItemByRemoteId("root-1", "remote-1");

        Assert.Single(Result.CreatedTrackedItems);
        Assert.Empty(Result.UntrackedChanges);
        Assert.NotNull(TrackedItem);
        Assert.Equal("RemoteChange.txt", Store.GetRemoteObservation(TrackedItem.Id).Name);
        Assert.Equal("token-2", Store.GetRemoteCheckpoint("root-1").StartPageToken);
    }
    /// <summary>
    /// Verifies metadata sync session classifies tracked sync root items.
    /// </summary>
    [Fact]
    public void ClassifySyncRootReturnsTrackedItemDiffs()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 25, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base-1", Time);
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-1", Time);

        IReadOnlyList<TrackedItemDiffRecord> Diffs = Session.ClassifySyncRoot("root-1");

        Assert.Single(Diffs);
        Assert.Equal("item-1", Diffs[0].TrackedItemId);
        Assert.Equal(SyncDiffKind.BothChangedCompatible, Diffs[0].DiffKind);
    }
    /// <summary>
    /// Verifies local missing versus remote modified state becomes a conflict decision.
    /// </summary>
    [Fact]
    public void CreatePlanDecisionsReturnsConflictWhenLocalMissingAndRemoteModified()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 26, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-remote",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = Time,
        });

        SyncPlanDecision Decision = Session.CreatePlanDecisions("root-1").Single();

        Assert.Equal(SyncDiffKind.Conflict, Decision.DiffKind);
        Assert.Equal(SyncPlanDecisionKind.Conflict, Decision.DecisionKind);
    }
    /// <summary>
    /// Verifies remote permanent delete versus local modified state becomes a conflict decision.
    /// </summary>
    [Fact]
    public void CreatePlanDecisionsReturnsConflictWhenRemoteRemovedAndLocalModified()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 26, 30, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "File1.txt",
            Name = "File1.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = false,
            Removed = true,
            ObservedTime = Time,
        });

        SyncPlanDecision Decision = Session.CreatePlanDecisions("root-1").Single();

        Assert.Equal(SyncDiffKind.Conflict, Decision.DiffKind);
        Assert.Equal(SyncPlanDecisionKind.Conflict, Decision.DecisionKind);
    }
    /// <summary>
    /// Verifies remote trash versus local modified state becomes a conflict decision.
    /// </summary>
    [Fact]
    public void CreatePlanDecisionsReturnsConflictWhenRemoteTrashedAndLocalModified()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 26, 40, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "File1.txt",
            Name = "File1.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 2,
            Trashed = true,
            ObservedTime = Time,
        });

        SyncPlanDecision Decision = Session.CreatePlanDecisions("root-1").Single();

        Assert.Equal(SyncDiffKind.Conflict, Decision.DiffKind);
        Assert.Equal(SyncPlanDecisionKind.Conflict, Decision.DecisionKind);
    }
    /// <summary>
    /// Verifies local missing and remote permanent delete tombstone commit missing base without executor work.
    /// </summary>
    [Fact]
    public void AdvanceMetadataOnlyCommitsBaseWhenLocalMissingAndRemoteRemoved()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 26, 43, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = false,
            Removed = true,
            ObservedTime = Time,
        });

        MetadataSyncSessionResult Result = Session.AdvanceMetadataOnly("root-1", Time);

        Assert.Single(Result.Decisions);
        Assert.Equal(SyncDiffKind.BothChangedCompatible, Result.Decisions[0].DiffKind);
        Assert.Equal(SyncPlanDecisionKind.CommitBase, Result.Decisions[0].DecisionKind);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.Empty(Result.PendingExecutorDecisions);
        Assert.Empty(Result.PendingExecutionRequests);
        Assert.False(Store.GetBaseSnapshot("item-1").ExistsFlag);
    }
    /// <summary>
    /// Verifies remote change processing commits compatible local missing and remote tombstone state without executor work.
    /// </summary>
    [Fact]
    public void AdvanceWithRemoteChangesCommitsCompatibleEndpointRemoval()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 26, 44, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-base", Time);

        MetadataSyncSessionResult Result = Session.AdvanceWithRemoteChanges(
            "root-1",
            [],
            [new StorageChange("remote-1", true, new DateTimeOffset(Time), null)],
            CreateCheckpoint("token-2", Time),
            Time,
            Time,
            Time,
            "scan-delete");

        Assert.Single(Result.Decisions);
        Assert.Equal(SyncDiffKind.BothChangedCompatible, Result.Decisions[0].DiffKind);
        Assert.Equal(SyncPlanDecisionKind.CommitBase, Result.Decisions[0].DecisionKind);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.Empty(Result.PendingExecutorDecisions);
        Assert.Empty(Result.PendingExecutionRequests);
        Assert.False(Store.GetBaseSnapshot("item-1").ExistsFlag);
        Assert.Equal("token-2", Store.GetRemoteCheckpoint("root-1").StartPageToken);
    }
    /// <summary>
    /// Verifies matching local and remote rename observations can advance the base snapshot.
    /// </summary>
    [Fact]
    public void CreatePlanDecisionsCommitsBaseWhenBothSidesRenameToSameName()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 26, 45, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "Renamed.txt",
            Name = "Renamed.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "Renamed.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = Time,
        });

        SyncPlanDecision Decision = Session.CreatePlanDecisions("root-1").Single();

        Assert.Equal(SyncDiffKind.BothChangedCompatible, Decision.DiffKind);
        Assert.Equal(SyncPlanDecisionKind.CommitBase, Decision.DecisionKind);
    }
    /// <summary>
    /// Verifies matching local and remote move observations can advance the base snapshot.
    /// </summary>
    [Fact]
    public void CreatePlanDecisionsCommitsBaseWhenBothSidesMoveToSamePath()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 26, 50, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "target-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-target",
            LocalKey = "Target",
            ItemType = "Folder",
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "target-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Target",
            LocalRelativePath = "Target",
            RemoteParentId = "remote-root",
            CommittedTime = Time,
        });
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "Folder/File1.txt"));
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            ItemType = "File",
            Name = "File1.txt",
            LocalRelativePath = "Folder/File1.txt",
            RemoteParentId = "remote-folder",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "Target/File1.txt",
            Name = "File1.txt",
            ParentRelativePath = "Target",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-target",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = Time,
        });

        SyncPlanDecision Decision = Session.CreatePlanDecisions("root-1")
            .Single(Item => string.Equals(Item.TrackedItemId, "item-1", StringComparison.Ordinal));

        Assert.Equal(SyncDiffKind.BothChangedCompatible, Decision.DiffKind);
        Assert.Equal(SyncPlanDecisionKind.CommitBase, Decision.DecisionKind);
    }
    /// <summary>
    /// Verifies matching local and remote rename plus move observations can advance the base snapshot.
    /// </summary>
    [Fact]
    public void CreatePlanDecisionsCommitsBaseWhenBothSidesRenameAndMoveToSamePath()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 26, 55, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "target-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-target",
            LocalKey = "Target",
            ItemType = "Folder",
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "target-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Target",
            LocalRelativePath = "Target",
            RemoteParentId = "remote-root",
            CommittedTime = Time,
        });
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "Folder/File1.txt"));
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            ItemType = "File",
            Name = "File1.txt",
            LocalRelativePath = "Folder/File1.txt",
            RemoteParentId = "remote-folder",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "Target/Renamed.txt",
            Name = "Renamed.txt",
            ParentRelativePath = "Target",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "Renamed.txt",
            RemoteParentId = "remote-target",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = Time,
        });

        SyncPlanDecision Decision = Session.CreatePlanDecisions("root-1")
            .Single(Item => string.Equals(Item.TrackedItemId, "item-1", StringComparison.Ordinal));

        Assert.Equal(SyncDiffKind.BothChangedCompatible, Decision.DiffKind);
        Assert.Equal(SyncPlanDecisionKind.CommitBase, Decision.DecisionKind);
    }
    /// <summary>
    /// Verifies local rename plus move and remote rename-only to the same name becomes a conflict decision.
    /// </summary>
    [Fact]
    public void CreatePlanDecisionsReturnsConflictWhenLocalMovesButRemoteOnlyRenamesToSameName()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 26, 58, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "Target/Renamed.txt",
            Name = "Renamed.txt",
            ParentRelativePath = "Target",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "Renamed.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = Time,
        });

        SyncPlanDecision Decision = Session.CreatePlanDecisions("root-1").Single();

        Assert.Equal(SyncDiffKind.Conflict, Decision.DiffKind);
        Assert.Equal(SyncPlanDecisionKind.Conflict, Decision.DecisionKind);
    }
    /// <summary>
    /// Verifies metadata sync session creates planner inputs from tracked item diffs.
    /// </summary>
    [Fact]
    public void CreatePlanInputsReturnsTrackedItemInputs()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 27, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base-1", Time);
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-1", Time);

        IReadOnlyList<SyncPlanInput> Inputs = Session.CreatePlanInputs("root-1");

        Assert.Single(Inputs);
        Assert.Equal("item-1", Inputs[0].TrackedItemId);
        Assert.Equal(SyncDiffKind.BothChangedCompatible, Inputs[0].DiffKind);
    }
    /// <summary>
    /// Verifies metadata sync session creates decisions for a sync root.
    /// </summary>
    [Fact]
    public void CreatePlanDecisionsReturnsSyncRootDecisions()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 6, 30, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base-1", Time);
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-1", Time);

        IReadOnlyList<SyncPlanDecision> Decisions = Session.CreatePlanDecisions("root-1");

        Assert.Single(Decisions);
        Assert.Equal("item-1", Decisions[0].TrackedItemId);
        Assert.Equal(SyncPlanDecisionKind.CommitBase, Decisions[0].DecisionKind);
    }
    /// <summary>
    /// Verifies metadata sync session commits only CommitBase decisions.
    /// </summary>
    [Fact]
    public void CommitBaseDecisionsCommitsOnlyMetadataOnlyItems()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 7, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        Store.InsertTrackedItem(CreateTrackedItem("item-2", "remote-2", "File2.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base-1", Time);
        AddBaseSnapshot(Store, "item-2", "File2.txt", "hash-base-2", Time);
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-1", Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-2",
            ExistsFlag = true,
            RelativePath = "File2.txt",
            Name = "File2.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base-2",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-2",
            RemoteItemId = "remote-2",
            ExistsFlag = true,
            Removed = false,
            Name = "File2.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-remote-2",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });

        IReadOnlyList<BaseSnapshotRecord> Committed = Session.CommitBaseDecisions("root-1", Time);

        Assert.Single(Committed);
        Assert.NotNull(Store.GetBaseSnapshot("item-1"));
        Assert.Equal("hash-base-2", Store.GetBaseSnapshot("item-2").ContentHash);
    }
    /// <summary>
    /// Verifies metadata-only advancement returns decisions and committed base snapshots.
    /// </summary>
    [Fact]
    public void AdvanceMetadataOnlyReturnsDecisionsAndCommittedSnapshots()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 7, 30, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        Store.InsertTrackedItem(CreateTrackedItem("item-2", "remote-2", "File2.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base-1", Time);
        AddBaseSnapshot(Store, "item-2", "File2.txt", "hash-base-2", Time);
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-1", Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-2",
            ExistsFlag = true,
            RelativePath = "File2.txt",
            Name = "File2.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local-2",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-2",
            RemoteItemId = "remote-2",
            ExistsFlag = true,
            Removed = false,
            Name = "File2.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base-2",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });

        MetadataSyncSessionResult Result = Session.AdvanceMetadataOnly("root-1", Time);

        Assert.Equal(2, Result.Decisions.Count);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.Single(Result.PendingExecutorDecisions);
        Assert.Single(Result.PendingExecutionRequests);
        Assert.Contains(Result.Decisions, Item => Item.DecisionKind == SyncPlanDecisionKind.CommitBase);
        Assert.Contains(Result.Decisions, Item => Item.DecisionKind == SyncPlanDecisionKind.UploadToRemote);
        Assert.Equal(SyncPlanDecisionKind.UploadToRemote, Result.PendingExecutorDecisions[0].DecisionKind);
        Assert.Equal(SyncPlanDecisionKind.UploadToRemote, Result.PendingExecutionRequests[0].Decision.DecisionKind);
        Assert.Equal("item-2", Result.PendingExecutionRequests[0].TrackedItem.Id);
        Assert.Equal("hash-base-2", Result.PendingExecutionRequests[0].BaseSnapshot.ContentHash);
        Assert.Equal("hash-local-2", Result.PendingExecutionRequests[0].LocalObservation.ContentHash);
        Assert.Equal("hash-base-2", Result.PendingExecutionRequests[0].RemoteObservation.ContentHash);
    }
    /// <summary>
    /// Verifies metadata-only advancement does not return no-op decisions as pending executor work.
    /// </summary>
    [Fact]
    public void AdvanceMetadataOnlyDoesNotReturnNoChangeAsPending()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 7, 35, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-1", Time);
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-1", Time);

        MetadataSyncSessionResult Result = Session.AdvanceMetadataOnly("root-1", Time);

        Assert.Single(Result.Decisions);
        Assert.Equal(SyncPlanDecisionKind.None, Result.Decisions[0].DecisionKind);
        Assert.Empty(Result.CommittedBaseSnapshots);
        Assert.Empty(Result.PendingExecutorDecisions);
        Assert.Empty(Result.PendingExecutionRequests);
    }
    /// <summary>
    /// Verifies a full snapshot session imports observations and commits compatible endpoint changes.
    /// </summary>
    [Fact]
    public void AdvanceWithRemoteSnapshotImportsAndCommitsMetadataOnlyChanges()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 7, 40, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);

        MetadataSyncSessionResult Result = Session.AdvanceWithRemoteSnapshot(
            "root-1",
            [CreateLocalScanItem("File1.txt", "hash-1", Time)],
            [CreateStorageItem("remote-1", "File1.txt", "hash-1", 1)],
            CreateCheckpoint("token-1", Time),
            Time,
            Time,
            Time,
            "scan-1");

        Assert.Single(Result.Decisions);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.Empty(Result.PendingExecutorDecisions);
        Assert.Empty(Result.PendingExecutionRequests);
        Assert.Empty(Result.UntrackedRemoteChanges);
        Assert.Equal(SyncPlanDecisionKind.CommitBase, Result.Decisions[0].DecisionKind);
        Assert.Equal("hash-1", Store.GetBaseSnapshot("item-1").ContentHash);
        Assert.Equal("token-1", Store.GetRemoteCheckpoint("root-1").StartPageToken);
    }
    /// <summary>
    /// Verifies bootstrap adopts matching local and remote root items into the same tracked identities.
    /// </summary>
    [Fact]
    public void AdvanceWithRemoteSnapshotAdoptsMatchingBootstrapItems()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 7, 42, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());

        MetadataSyncSessionResult Result = Session.AdvanceWithRemoteSnapshot(
            "root-1",
            [
                CreateLocalScanItem("LocalFile01.txt", "hash-file", Time),
                CreateLocalFolderScanItem("LocalFolder01", Time),
                CreateNestedLocalScanItem("LocalFolder01/NestedLocal01.txt", "NestedLocal01.txt", "LocalFolder01", "hash-nested", Time),
            ],
            [
                CreateStorageItem("remote-file", "LocalFile01.txt", "hash-file", 1),
                CreateStorageFolder("remote-folder", "LocalFolder01", 1),
            ],
            CreateCheckpoint("token-bootstrap", Time),
            Time,
            Time,
            Time,
            "scan-bootstrap");
        TrackedItemRecord File = Store.GetTrackedItemByLocalKey("root-1", "LocalFile01.txt");
        TrackedItemRecord Folder = Store.GetTrackedItemByLocalKey("root-1", "LocalFolder01");
        TrackedItemRecord Nested = Store.GetTrackedItemByLocalKey("root-1", "LocalFolder01/NestedLocal01.txt");
        SyncExecutionRequest NestedRequest = Result.PendingExecutionRequests.Single();

        Assert.Equal(3, Store.GetTrackedItems("root-1").Count);
        Assert.Equal("remote-file", File.RemoteItemId);
        Assert.Equal("remote-folder", Folder.RemoteItemId);
        Assert.Null(Nested.RemoteItemId);
        Assert.NotNull(Store.GetBaseSnapshot(File.Id));
        Assert.NotNull(Store.GetBaseSnapshot(Folder.Id));
        Assert.Null(Store.GetBaseSnapshot(Nested.Id));
        Assert.Single(Result.PendingExecutorDecisions);
        Assert.Equal(SyncPlanDecisionKind.UploadToRemote, NestedRequest.Decision.DecisionKind);
        Assert.Equal("remote-folder", NestedRequest.LocalParentRemoteItemId);
    }
    /// <summary>
    /// Verifies an incremental changes session returns executor work after importing changes.
    /// </summary>
    [Fact]
    public void AdvanceWithRemoteChangesImportsAndReturnsPendingExecutorDecisions()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 7, 45, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);

        MetadataSyncSessionResult Result = Session.AdvanceWithRemoteChanges(
            "root-1",
            [CreateLocalScanItem("File1.txt", "hash-base", Time)],
            [
                new StorageChange(
                    "remote-1",
                    false,
                    new DateTimeOffset(Time),
                    CreateStorageItem("remote-1", "File1.txt", "hash-remote", 2)),
            ],
            CreateCheckpoint("token-2", Time),
            Time,
            Time,
            Time,
            "scan-2");

        Assert.Single(Result.Decisions);
        Assert.Single(Result.PendingExecutorDecisions);
        Assert.Single(Result.PendingExecutionRequests);
        Assert.Empty(Result.CommittedBaseSnapshots);
        Assert.Empty(Result.UntrackedRemoteChanges);
        Assert.Equal(SyncPlanDecisionKind.DownloadToLocal, Result.PendingExecutorDecisions[0].DecisionKind);
        Assert.Equal(SyncPlanDecisionKind.DownloadToLocal, Result.PendingExecutionRequests[0].Decision.DecisionKind);
        Assert.Equal("item-1", Result.PendingExecutionRequests[0].TrackedItem.Id);
        Assert.Equal("hash-base", Result.PendingExecutionRequests[0].LocalObservation.ContentHash);
        Assert.Equal("hash-remote", Result.PendingExecutionRequests[0].RemoteObservation.ContentHash);
        Assert.Equal("hash-base", Store.GetBaseSnapshot("item-1").ContentHash);
        Assert.Equal("token-2", Store.GetRemoteCheckpoint("root-1").StartPageToken);
    }
    /// <summary>
    /// Verifies an incremental changes session stops before local import and planning when remote changes cannot be tracked.
    /// </summary>
    [Fact]
    public void AdvanceWithRemoteChangesStopsBeforePlanningWhenUntrackedChangesRemain()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 7, 50, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());

        MetadataSyncSessionResult Result = Session.AdvanceWithRemoteChanges(
            "root-1",
            [CreateLocalScanItem("Local.txt", "hash-local", Time)],
            [new StorageChange("remote-missing", true, new DateTimeOffset(Time), null)],
            CreateCheckpoint("token-3", Time),
            Time,
            Time,
            Time,
            "scan-3");

        Assert.Empty(Result.Decisions);
        Assert.Empty(Result.PendingExecutorDecisions);
        Assert.Empty(Result.PendingExecutionRequests);
        Assert.Empty(Result.CommittedBaseSnapshots);
        Assert.Single(Result.UntrackedRemoteChanges);
        Assert.Null(Store.GetRemoteCheckpoint("root-1"));
        Assert.Null(Store.GetTrackedItemByLocalKey("root-1", "Local.txt"));
    }
    /// <summary>
    /// Verifies verified execution results commit their current observations as base snapshots.
    /// </summary>
    [Fact]
    public void CommitVerifiedExecutionResultsCommitsCompletedAndVerifiedItems()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        MetadataSyncSessionResult SessionResult = Session.AdvanceWithRemoteChanges(
            "root-1",
            [CreateLocalScanItem("File1.txt", "hash-base", Time)],
            [
                new StorageChange(
                    "remote-1",
                    false,
                    new DateTimeOffset(Time),
                    CreateStorageItem("remote-1", "File1.txt", "hash-remote", 2)),
            ],
            CreateCheckpoint("token-4", Time),
            Time,
            Time,
            Time,
            "scan-4");

        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "File1.txt",
            Name = "File1.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-remote",
            ObservedTime = Time,
        });

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = SessionResult.PendingExecutionRequests[0],
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                },
            ],
            Time);

        Assert.Single(Result.CommittedResults);
        Assert.Empty(Result.UncommittedResults);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.Equal("hash-remote", Store.GetBaseSnapshot("item-1").ContentHash);
    }
    /// <summary>
    /// Verifies successful uploads persist the returned remote item before committing base state.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresUploadedRemoteItemBeforeBaseCommit()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 3, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", null, "LocalOnly.txt"));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "LocalOnly.txt",
            Name = "LocalOnly.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ObservedTime = Time,
        });
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = SessionResult.PendingExecutionRequests[0],
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    RemoteItem = CreateStorageItem("remote-uploaded", "LocalOnly.txt", "hash-local", 1),
                },
            ],
            Time);

        Assert.Single(Result.CommittedResults);
        Assert.Empty(Result.UncommittedResults);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.Equal("remote-uploaded", Store.GetTrackedItem("item-1").RemoteItemId);
        Assert.Equal("remote-uploaded", Store.GetRemoteObservation("item-1").RemoteItemId);
        Assert.Equal("hash-local", Store.GetBaseSnapshot("item-1").ContentHash);
    }
    /// <summary>
    /// Verifies successful downloads persist the affected local path before committing base state.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresDownloadedLocalObservationBeforeBaseCommit()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 4, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", null));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "RemoteOnly.txt",
            Name = "RemoteOnly.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-old-local",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "RemoteOnly.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-remote",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = SessionResult.PendingExecutionRequests[0],
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    RemoteItem = CreateStorageItem("remote-1", "RemoteOnly.txt", "hash-remote", 1),
                    LocalRelativePath = "RemoteOnly.txt",
                },
            ],
            Time);

        Assert.Single(Result.CommittedResults);
        Assert.Empty(Result.UncommittedResults);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.Equal("RemoteOnly.txt", Store.GetTrackedItem("item-1").LocalKey);
        Assert.Equal("RemoteOnly.txt", Store.GetLocalObservation("item-1").RelativePath);
        Assert.Equal("hash-remote", Store.GetLocalObservation("item-1").ContentHash);
        Assert.Equal("hash-remote", Store.GetBaseSnapshot("item-1").ContentHash);
    }
    /// <summary>
    /// Verifies successful remote delete propagation stores local missing state before base commit.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresRemoteDeleteLocalMissingBeforeBaseCommit()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 4, 15, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "RemoteOnly.txt"));
        AddBaseSnapshot(Store, "item-1", "RemoteOnly.txt", "hash-base", Time);
        AddObservedItem(Store, "item-1", "remote-1", "RemoteOnly.txt", "hash-base", Time);
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "RemoteOnly.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 2,
            Trashed = true,
            ObservedTime = Time,
        });
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = SessionResult.PendingExecutionRequests[0],
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                },
            ],
            Time);

        Assert.Single(Result.CommittedResults);
        Assert.Empty(Result.UncommittedResults);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.False(Store.GetLocalObservation("item-1").ExistsFlag);
        Assert.False(Store.GetBaseSnapshot("item-1").ExistsFlag);
    }
    /// <summary>
    /// Verifies permanent remote delete tombstones store local missing state before base commit.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresRemotePermanentDeleteLocalMissingBeforeBaseCommit()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 4, 18, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "RemoteOnly.txt"));
        AddBaseSnapshot(Store, "item-1", "RemoteOnly.txt", "hash-base", Time);
        AddObservedItem(Store, "item-1", "remote-1", "RemoteOnly.txt", "hash-base", Time);
        MetadataSyncSessionResult SessionResult = Session.AdvanceWithRemoteChanges(
            "root-1",
            [CreateLocalScanItem("RemoteOnly.txt", "hash-base", Time)],
            [new StorageChange("remote-1", true, new DateTimeOffset(Time), null)],
            CreateCheckpoint("token-permanent-delete", Time),
            Time,
            Time,
            Time,
            "scan-permanent-delete");

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = SessionResult.PendingExecutionRequests[0],
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                },
            ],
            Time);

        Assert.Single(Result.CommittedResults);
        Assert.Empty(Result.UncommittedResults);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.Equal(SyncPlanDecisionKind.PropagateRemoteDelete, SessionResult.PendingExecutionRequests[0].Decision.DecisionKind);
        Assert.False(Store.GetRemoteObservation("item-1").ExistsFlag);
        Assert.True(Store.GetRemoteObservation("item-1").Removed);
        Assert.False(Store.GetLocalObservation("item-1").ExistsFlag);
        Assert.False(Store.GetBaseSnapshot("item-1").ExistsFlag);
    }
    /// <summary>
    /// Verifies permanent remote folder delete tombstones commit descendant items as missing.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresRemotePermanentFolderDeleteDescendantsAsMissing()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 4, 19, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "folder-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-folder",
            LocalKey = "Folder",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(CreateTrackedItem("file-item", "remote-file", "Folder/File1.txt"));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            RelativePath = "Folder",
            Name = "Folder",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            RelativePath = "Folder/File1.txt",
            Name = "File1.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            RemoteItemId = "remote-file",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Folder",
            LocalRelativePath = "Folder",
            RemoteParentId = "remote-root",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            ItemType = "File",
            Name = "File1.txt",
            LocalRelativePath = "Folder/File1.txt",
            RemoteParentId = "remote-folder",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        MetadataSyncSessionResult SessionResult = Session.AdvanceWithRemoteChanges(
            "root-1",
            [
                CreateLocalFolderScanItem("Folder", Time),
                new LocalScanItem()
                {
                    RelativePath = "Folder/File1.txt",
                    Name = "File1.txt",
                    ParentRelativePath = "Folder",
                    ItemType = "File",
                    Size = 42,
                    ContentHash = "hash-base",
                    ModifiedTime = Time,
                },
            ],
            [new StorageChange("remote-folder", true, new DateTimeOffset(Time), null)],
            CreateCheckpoint("token-permanent-folder-delete", Time),
            Time,
            Time,
            Time,
            "scan-permanent-folder-delete");

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = SessionResult.PendingExecutionRequests.Single(),
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                },
            ],
            Time);

        Assert.Single(Result.CommittedResults);
        Assert.Empty(Result.UncommittedResults);
        Assert.Equal(2, Result.CommittedBaseSnapshots.Count);
        Assert.False(Store.GetBaseSnapshot("folder-item").ExistsFlag);
        Assert.False(Store.GetBaseSnapshot("file-item").ExistsFlag);
        Assert.False(Store.GetLocalObservation("file-item").ExistsFlag);
        Assert.False(Store.GetRemoteObservation("file-item").ExistsFlag);
        Assert.True(Store.GetRemoteObservation("file-item").Removed);
        Assert.All(Session.ClassifySyncRoot("root-1"), Item => Assert.Equal(SyncDiffKind.NoChange, Item.DiffKind));
    }
    /// <summary>
    /// Verifies remote folder trash commits descendant items as missing without marking them permanently removed.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresRemoteFolderTrashDescendantsAsTrashed()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 4, 19, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "folder-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-folder",
            LocalKey = "Folder",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(CreateTrackedItem("file-item", "remote-file", "Folder/File1.txt"));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            RelativePath = "Folder",
            Name = "Folder",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            RelativePath = "Folder/File1.txt",
            Name = "File1.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            RemoteItemId = "remote-file",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Folder",
            LocalRelativePath = "Folder",
            RemoteParentId = "remote-root",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            ItemType = "File",
            Name = "File1.txt",
            LocalRelativePath = "Folder/File1.txt",
            RemoteParentId = "remote-folder",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 2,
            Trashed = true,
            ObservedTime = Time,
        });
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = SessionResult.PendingExecutionRequests.Single(),
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                },
            ],
            Time);

        Assert.Single(Result.CommittedResults);
        Assert.Empty(Result.UncommittedResults);
        Assert.Equal(2, Result.CommittedBaseSnapshots.Count);
        Assert.False(Store.GetBaseSnapshot("folder-item").ExistsFlag);
        Assert.False(Store.GetBaseSnapshot("file-item").ExistsFlag);
        Assert.False(Store.GetLocalObservation("file-item").ExistsFlag);
        Assert.True(Store.GetRemoteObservation("file-item").ExistsFlag);
        Assert.False(Store.GetRemoteObservation("file-item").Removed);
        Assert.True(Store.GetRemoteObservation("file-item").Trashed);
        Assert.All(Session.ClassifySyncRoot("root-1"), Item => Assert.Equal(SyncDiffKind.NoChange, Item.DiffKind));
    }
    /// <summary>
    /// Verifies remote folder namespace changes update descendant local metadata before base commit.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresFolderNamespaceDescendantPathsBeforeBaseCommit()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 4, 20, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "folder-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-folder",
            LocalKey = "Folder",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(CreateTrackedItem("file-item", "remote-file", "Folder/Nested.txt"));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            RelativePath = "Folder",
            Name = "Folder",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            RelativePath = "Folder/Nested.txt",
            Name = "Nested.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-nested",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "RenamedFolder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            RemoteItemId = "remote-file",
            ExistsFlag = true,
            Removed = false,
            Name = "Nested.txt",
            RemoteParentId = "remote-folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-nested",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Folder",
            LocalRelativePath = "Folder",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            ItemType = "File",
            Name = "Nested.txt",
            LocalRelativePath = "Folder/Nested.txt",
            RemoteParentId = "remote-folder",
            Size = 42,
            ContentHash = "hash-nested",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = SessionResult.PendingExecutionRequests.Single(),
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    LocalRelativePath = "RenamedFolder",
                },
            ],
            Time);

        Assert.Single(Result.CommittedResults);
        Assert.Equal(2, Result.CommittedBaseSnapshots.Count);
        Assert.Equal("RenamedFolder", Store.GetTrackedItem("folder-item").LocalKey);
        Assert.Equal("RenamedFolder/Nested.txt", Store.GetTrackedItem("file-item").LocalKey);
        Assert.Equal("RenamedFolder/Nested.txt", Store.GetLocalObservation("file-item").RelativePath);
        Assert.Equal("RenamedFolder/Nested.txt", Store.GetBaseSnapshot("file-item").LocalRelativePath);
    }
    /// <summary>
    /// Verifies local folder rename execution updates descendant local metadata before base commit.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresLocalFolderRenameDescendantPathsBeforeBaseCommit()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 4, 40, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "folder-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-folder",
            LocalKey = "Folder",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(CreateTrackedItem("file-item", "remote-file", "Folder/Nested.txt"));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            RelativePath = "Folder",
            Name = "Folder",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            RelativePath = "Folder/Nested.txt",
            Name = "Nested.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-nested",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            RemoteItemId = "remote-file",
            ExistsFlag = true,
            Removed = false,
            Name = "Nested.txt",
            RemoteParentId = "remote-folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-nested",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Folder",
            LocalRelativePath = "Folder",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            ItemType = "File",
            Name = "Nested.txt",
            LocalRelativePath = "Folder/Nested.txt",
            RemoteParentId = "remote-folder",
            Size = 42,
            ContentHash = "hash-nested",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        LocalScanImportResult ImportResult = Session.ImportLocalScan(
            "root-1",
            [
                CreateLocalFolderScanItem("RenamedFolder", Time),
                new LocalScanItem()
                {
                    RelativePath = "RenamedFolder/Nested.txt",
                    Name = "Nested.txt",
                    ParentRelativePath = "RenamedFolder",
                    ItemType = "File",
                    Size = 42,
                    ContentHash = "hash-nested",
                    ModifiedTime = Time,
                },
            ],
            Time,
            "scan-folder-rename");
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);
        SyncExecutionRequest Request = SessionResult.PendingExecutionRequests.Single(Item => Item.Decision.TrackedItemId == "folder-item");

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = Request,
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    RemoteItem = new StorageItem(
                        "remote-folder",
                        "remote-root",
                        "RenamedFolder",
                        "/RenamedFolder",
                        StorageItemKind.Folder,
                        "application/vnd.google-apps.folder",
                        0,
                        string.Empty,
                        default,
                        default,
                        2,
                        false),
                    LocalRelativePath = "RenamedFolder",
                },
            ],
            Time);

        Assert.Empty(ImportResult.CreatedTrackedItems);
        Assert.Single(Result.CommittedResults);
        Assert.Equal(2, Result.CommittedBaseSnapshots.Count);
        Assert.Equal("RenamedFolder", Store.GetTrackedItem("folder-item").LocalKey);
        Assert.Equal("RenamedFolder/Nested.txt", Store.GetTrackedItem("file-item").LocalKey);
        Assert.Equal("RenamedFolder/Nested.txt", Store.GetLocalObservation("file-item").RelativePath);
        Assert.Equal("RenamedFolder/Nested.txt", Store.GetBaseSnapshot("file-item").LocalRelativePath);
        Assert.Equal("remote-folder", Store.GetBaseSnapshot("file-item").RemoteParentId);
    }
    /// <summary>
    /// Verifies local folder move execution updates descendant local metadata before base commit.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresLocalFolderMoveDescendantPathsBeforeBaseCommit()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 4, 50, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "parent-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-parent",
            LocalKey = "Parent",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "folder-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-folder",
            LocalKey = "Folder",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(CreateTrackedItem("file-item", "remote-file", "Folder/Nested.txt"));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "parent-item",
            ExistsFlag = true,
            RelativePath = "Parent",
            Name = "Parent",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            RelativePath = "Folder",
            Name = "Folder",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            RelativePath = "Folder/Nested.txt",
            Name = "Nested.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-nested",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "parent-item",
            RemoteItemId = "remote-parent",
            ExistsFlag = true,
            Removed = false,
            Name = "Parent",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            RemoteItemId = "remote-file",
            ExistsFlag = true,
            Removed = false,
            Name = "Nested.txt",
            RemoteParentId = "remote-folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-nested",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "parent-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Parent",
            LocalRelativePath = "Parent",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Folder",
            LocalRelativePath = "Folder",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            ItemType = "File",
            Name = "Nested.txt",
            LocalRelativePath = "Folder/Nested.txt",
            RemoteParentId = "remote-folder",
            Size = 42,
            ContentHash = "hash-nested",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        LocalScanImportResult ImportResult = Session.ImportLocalScan(
            "root-1",
            [
                CreateLocalFolderScanItem("Parent", Time),
                new LocalScanItem()
                {
                    RelativePath = "Parent/Folder",
                    Name = "Folder",
                    ParentRelativePath = "Parent",
                    ItemType = "Folder",
                    ModifiedTime = Time,
                },
                new LocalScanItem()
                {
                    RelativePath = "Parent/Folder/Nested.txt",
                    Name = "Nested.txt",
                    ParentRelativePath = "Parent/Folder",
                    ItemType = "File",
                    Size = 42,
                    ContentHash = "hash-nested",
                    ModifiedTime = Time,
                },
            ],
            Time,
            "scan-folder-move");
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);
        SyncExecutionRequest Request = SessionResult.PendingExecutionRequests.Single(Item => Item.Decision.TrackedItemId == "folder-item");

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = Request,
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    RemoteItem = new StorageItem(
                        "remote-folder",
                        "remote-parent",
                        "Folder",
                        "/Parent/Folder",
                        StorageItemKind.Folder,
                        "application/vnd.google-apps.folder",
                        0,
                        string.Empty,
                        default,
                        default,
                        2,
                        false),
                    LocalRelativePath = "Parent/Folder",
                },
            ],
            Time);

        Assert.Empty(ImportResult.CreatedTrackedItems);
        Assert.Single(Result.CommittedResults);
        Assert.Equal(2, Result.CommittedBaseSnapshots.Count);
        Assert.Equal("Parent/Folder", Store.GetTrackedItem("folder-item").LocalKey);
        Assert.Equal("Parent/Folder/Nested.txt", Store.GetTrackedItem("file-item").LocalKey);
        Assert.Equal("Parent/Folder/Nested.txt", Store.GetLocalObservation("file-item").RelativePath);
        Assert.Equal("Parent/Folder/Nested.txt", Store.GetBaseSnapshot("file-item").LocalRelativePath);
        Assert.Equal("remote-parent", Store.GetBaseSnapshot("folder-item").RemoteParentId);
        Assert.Equal("remote-folder", Store.GetBaseSnapshot("file-item").RemoteParentId);
    }
    /// <summary>
    /// Verifies combined local folder rename and move updates descendant local metadata before base commit.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresLocalFolderRenameAndMoveDescendantPathsBeforeBaseCommit()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 4, 55, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "parent-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-parent",
            LocalKey = "Parent",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "folder-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-folder",
            LocalKey = "Folder",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(CreateTrackedItem("file-item", "remote-file", "Folder/Nested.txt"));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "parent-item",
            ExistsFlag = true,
            RelativePath = "Parent",
            Name = "Parent",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            RelativePath = "Folder",
            Name = "Folder",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            RelativePath = "Folder/Nested.txt",
            Name = "Nested.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-nested",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "parent-item",
            RemoteItemId = "remote-parent",
            ExistsFlag = true,
            Removed = false,
            Name = "Parent",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            RemoteItemId = "remote-file",
            ExistsFlag = true,
            Removed = false,
            Name = "Nested.txt",
            RemoteParentId = "remote-folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-nested",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "parent-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Parent",
            LocalRelativePath = "Parent",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Folder",
            LocalRelativePath = "Folder",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            ItemType = "File",
            Name = "Nested.txt",
            LocalRelativePath = "Folder/Nested.txt",
            RemoteParentId = "remote-folder",
            Size = 42,
            ContentHash = "hash-nested",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        LocalScanImportResult ImportResult = Session.ImportLocalScan(
            "root-1",
            [
                CreateLocalFolderScanItem("Parent", Time),
                new LocalScanItem()
                {
                    RelativePath = "Parent/RenamedFolder",
                    Name = "RenamedFolder",
                    ParentRelativePath = "Parent",
                    ItemType = "Folder",
                    ModifiedTime = Time,
                },
                new LocalScanItem()
                {
                    RelativePath = "Parent/RenamedFolder/Nested.txt",
                    Name = "Nested.txt",
                    ParentRelativePath = "Parent/RenamedFolder",
                    ItemType = "File",
                    Size = 42,
                    ContentHash = "hash-nested",
                    ModifiedTime = Time,
                },
            ],
            Time,
            "scan-folder-rename-move");
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);
        SyncExecutionRequest Request = SessionResult.PendingExecutionRequests.Single(Item => Item.Decision.TrackedItemId == "folder-item");

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = Request,
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    RemoteItem = new StorageItem(
                        "remote-folder",
                        "remote-parent",
                        "RenamedFolder",
                        "/Parent/RenamedFolder",
                        StorageItemKind.Folder,
                        "application/vnd.google-apps.folder",
                        0,
                        string.Empty,
                        default,
                        default,
                        2,
                        false),
                    LocalRelativePath = "Parent/RenamedFolder",
                },
            ],
            Time);

        Assert.Empty(ImportResult.CreatedTrackedItems);
        Assert.Single(Result.CommittedResults);
        Assert.Equal(2, Result.CommittedBaseSnapshots.Count);
        Assert.Equal("Parent/RenamedFolder", Store.GetTrackedItem("folder-item").LocalKey);
        Assert.Equal("Parent/RenamedFolder/Nested.txt", Store.GetTrackedItem("file-item").LocalKey);
        Assert.Equal("Parent/RenamedFolder/Nested.txt", Store.GetLocalObservation("file-item").RelativePath);
        Assert.Equal("Parent/RenamedFolder/Nested.txt", Store.GetBaseSnapshot("file-item").LocalRelativePath);
        Assert.Equal("RenamedFolder", Store.GetBaseSnapshot("folder-item").Name);
        Assert.Equal("remote-parent", Store.GetBaseSnapshot("folder-item").RemoteParentId);
        Assert.Equal("remote-folder", Store.GetBaseSnapshot("file-item").RemoteParentId);
    }
    /// <summary>
    /// Verifies remote folder moves update descendant local metadata before base commit.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsStoresFolderMoveDescendantPathsBeforeBaseCommit()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 9, 10, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "parent-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-parent",
            LocalKey = "Target",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "folder-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-folder",
            LocalKey = "Folder",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(CreateTrackedItem("file-item", "remote-file", "Folder/Nested.txt"));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "parent-item",
            ExistsFlag = true,
            RelativePath = "Target",
            Name = "Target",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            RelativePath = "Folder",
            Name = "Folder",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            RelativePath = "Folder/Nested.txt",
            Name = "Nested.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-nested",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "parent-item",
            RemoteItemId = "remote-parent",
            ExistsFlag = true,
            Removed = false,
            Name = "Target",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-parent",
            ItemType = "Folder",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            RemoteItemId = "remote-file",
            ExistsFlag = true,
            Removed = false,
            Name = "Nested.txt",
            RemoteParentId = "remote-folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-nested",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "parent-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Target",
            LocalRelativePath = "Target",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Folder",
            LocalRelativePath = "Folder",
            RemoteParentId = "remote-root",
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            ItemType = "File",
            Name = "Nested.txt",
            LocalRelativePath = "Folder/Nested.txt",
            RemoteParentId = "remote-folder",
            Size = 42,
            ContentHash = "hash-nested",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);
        SyncExecutionRequest Request = SessionResult.PendingExecutionRequests.Single();

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = Request,
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    LocalRelativePath = "Target/Folder",
                },
            ],
            Time);

        Assert.Equal(SyncPlanDecisionKind.ApplyRemoteNamespaceToLocal, Request.Decision.DecisionKind);
        Assert.Single(Result.CommittedResults);
        Assert.Equal(2, Result.CommittedBaseSnapshots.Count);
        Assert.Equal("Target/Folder", Store.GetTrackedItem("folder-item").LocalKey);
        Assert.Equal("Target/Folder/Nested.txt", Store.GetTrackedItem("file-item").LocalKey);
        Assert.Equal("Target/Folder/Nested.txt", Store.GetLocalObservation("file-item").RelativePath);
        Assert.Equal("Target/Folder/Nested.txt", Store.GetBaseSnapshot("file-item").LocalRelativePath);
        Assert.Equal("remote-folder", Store.GetBaseSnapshot("file-item").RemoteParentId);
    }
    /// <summary>
    /// Verifies nested remote download requests resolve their parent local path from metadata.
    /// </summary>
    [Fact]
    public void AdvanceMetadataOnlyAddsRemoteParentLocalPathToNestedDownloadRequests()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 4, 30, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("folder-item", "remote-folder", "Folder"));
        Store.InsertTrackedItem(CreateTrackedItem("file-item", "remote-file", null));
        AddObservedItem(Store, "folder-item", "remote-folder", "Folder", string.Empty, Time);
        AddBaseSnapshot(Store, "folder-item", "Folder", string.Empty, Time);
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            RemoteItemId = "remote-file",
            ExistsFlag = true,
            Removed = false,
            Name = "Nested.txt",
            RemoteParentId = "remote-folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-remote",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });

        MetadataSyncSessionResult Result = Session.AdvanceMetadataOnly("root-1", Time);
        SyncExecutionRequest Request = Result.PendingExecutionRequests.Single(Item => Item.Decision.TrackedItemId == "file-item");
        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(Request);

        Assert.Equal("Folder", Request.RemoteParentLocalRelativePath);
        Assert.Equal("Folder/Nested.txt", Intent.LocalRelativePath);
        Assert.True(Intent.CanExecute);
    }
    /// <summary>
    /// Verifies nested local upload requests resolve their remote parent id from local parent metadata.
    /// </summary>
    [Fact]
    public void AdvanceMetadataOnlyAddsLocalParentRemoteIdToNestedUploadRequests()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 4, 45, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("folder-item", "remote-folder", "Folder"));
        Store.InsertTrackedItem(CreateTrackedItem("file-item", null, "Folder/Nested.txt"));
        AddObservedItem(Store, "folder-item", "remote-folder", "Folder", string.Empty, Time);
        AddBaseSnapshot(Store, "folder-item", "Folder", string.Empty, Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            RelativePath = "Folder/Nested.txt",
            Name = "Nested.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ObservedTime = Time,
        });

        MetadataSyncSessionResult Result = Session.AdvanceMetadataOnly("root-1", Time);
        SyncExecutionRequest Request = Result.PendingExecutionRequests.Single(Item => Item.Decision.TrackedItemId == "file-item");
        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(Request);

        Assert.Equal("remote-folder", Request.LocalParentRemoteItemId);
        Assert.Equal("remote-folder", Intent.RemoteParentId);
        Assert.True(Intent.CanExecute);
    }
    /// <summary>
    /// Verifies failed execution results do not commit base snapshots.
    /// </summary>
    [Fact]
    public void CommitVerifiedExecutionResultsIgnoresFailedItems()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 5, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "File1.txt",
            Name = "File1.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = SessionResult.PendingExecutionRequests[0],
                    ResultKind = SyncExecutionResultKind.FailedRetryable,
                },
            ],
            Time);

        Assert.Empty(Result.CommittedResults);
        Assert.Single(Result.UncommittedResults);
        Assert.Empty(Result.CommittedBaseSnapshots);
        Assert.Equal("hash-base", Store.GetBaseSnapshot("item-1").ContentHash);
    }
    /// <summary>
    /// Verifies the verified execution commit helper returns committed base snapshots.
    /// </summary>
    [Fact]
    public void CommitVerifiedExecutionResultsReturnsCommittedBaseSnapshots()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 10, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "File1.txt",
            Name = "File1.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);

        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = Time,
        });

        IReadOnlyList<BaseSnapshotRecord> Committed = Session.CommitVerifiedExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = SessionResult.PendingExecutionRequests[0],
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                },
            ],
            Time);

        Assert.Single(Committed);
        Assert.Equal("hash-local", Store.GetBaseSnapshot("item-1").ContentHash);
    }
    /// <summary>
    /// Verifies pending requests can be executed and applied through the sync session.
    /// </summary>
    [Fact]
    public async Task ExecutePendingRequestsAsyncExecutesAndAppliesVerifiedResults()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 15, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        MetadataSyncSessionResult SessionResult = Session.AdvanceWithRemoteChanges(
            "root-1",
            [CreateLocalScanItem("File1.txt", "hash-base", Time)],
            [
                new StorageChange(
                    "remote-1",
                    false,
                    new DateTimeOffset(Time),
                    CreateStorageItem("remote-1", "File1.txt", "hash-remote", 2)),
            ],
            CreateCheckpoint("token-5", Time),
            Time,
            Time,
            Time,
            "scan-5");
        FakeSyncExecutor Executor = new(Request =>
        {
            Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
            {
                TrackedItemId = Request.Decision.TrackedItemId,
                ExistsFlag = true,
                RelativePath = "File1.txt",
                Name = "File1.txt",
                ItemType = "File",
                Size = 42,
                ContentHash = "hash-remote",
                ObservedTime = Time,
            });
        });

        SyncExecutionApplyResult Result = await Session.ExecutePendingRequestsAsync(
            SessionResult,
            Executor,
            Time,
            CancellationToken.None);

        Assert.Single(Executor.Requests);
        Assert.Single(Result.CommittedResults);
        Assert.Empty(Result.UncommittedResults);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.Equal("hash-remote", Store.GetBaseSnapshot("item-1").ContentHash);
    }
    /// <summary>
    /// Verifies pending requests are applied between executions so restored child downloads can resolve restored parent paths.
    /// </summary>
    [Fact]
    public async Task ExecutePendingRequestsAsyncRefreshesRequestsAfterEachCommittedResult()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 16, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("folder-item", "remote-folder", "Folder"));
        Store.InsertTrackedItem(CreateTrackedItem("file-item", "remote-file", "Folder/Nested.txt"));
        Store.UpsertLocalObservation(LocalObservationMapper.Missing("folder-item", Time, "scan-restore"));
        Store.UpsertLocalObservation(LocalObservationMapper.Missing("file-item", Time, "scan-restore"));
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord() { TrackedItemId = "folder-item", ExistsFlag = false, CommittedTime = Time });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord() { TrackedItemId = "file-item", ExistsFlag = false, CommittedTime = Time });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            RemoteItemId = "remote-file",
            ExistsFlag = true,
            Removed = false,
            Name = "Nested.txt",
            RemoteParentId = "remote-folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-nested",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = Time,
        });
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);
        FakeSyncExecutor Executor = new(Request =>
        {
            SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(Request);
            Request.LocalObservation = new LocalObservedSnapshotRecord()
            {
                TrackedItemId = Request.Decision.TrackedItemId,
                ExistsFlag = true,
                RelativePath = Intent.LocalRelativePath,
                Name = Intent.Name,
                ItemType = Intent.ItemType,
                Size = Intent.Size,
                ContentHash = Intent.ContentHash,
                ObservedTime = Time,
            };
            Store.UpsertLocalObservation(Request.LocalObservation);
        });

        SyncExecutionApplyResult Result = await Session.ExecutePendingRequestsAsync(
            SessionResult,
            Executor,
            Time,
            CancellationToken.None);

        Assert.Equal(2, Result.CommittedResults.Count);
        Assert.Empty(Result.UncommittedResults);
        Assert.Equal("folder-item", Executor.Requests[0].Decision.TrackedItemId);
        Assert.Equal("file-item", Executor.Requests[1].Decision.TrackedItemId);
        Assert.Equal("Folder/Nested.txt", Executor.Intents[1].LocalRelativePath);
        Assert.Equal("Folder/Nested.txt", Store.GetBaseSnapshot("file-item").LocalRelativePath);
    }
    /// <summary>
    /// Verifies failed executor results are applied without committing base snapshots.
    /// </summary>
    [Fact]
    public async Task ExecutePendingRequestsAsyncKeepsFailedResultsUncommitted()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 20, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        MetadataSyncSessionResult SessionResult = Session.AdvanceWithRemoteChanges(
            "root-1",
            [CreateLocalScanItem("File1.txt", "hash-base", Time)],
            [
                new StorageChange(
                    "remote-1",
                    false,
                    new DateTimeOffset(Time),
                    CreateStorageItem("remote-1", "File1.txt", "hash-remote", 2)),
            ],
            CreateCheckpoint("token-6", Time),
            Time,
            Time,
            Time,
            "scan-6");
        FakeSyncExecutor Executor = new(_ => { }, SyncExecutionResultKind.FailedRetryable);

        SyncExecutionApplyResult Result = await Session.ExecutePendingRequestsAsync(
            SessionResult,
            Executor,
            Time,
            CancellationToken.None);

        Assert.Single(Executor.Requests);
        Assert.Empty(Result.CommittedResults);
        Assert.Single(Result.UncommittedResults);
        Assert.Empty(Result.CommittedBaseSnapshots);
        Assert.Equal("hash-base", Store.GetBaseSnapshot("item-1").ContentHash);
    }
    /// <summary>
    /// Verifies a remote changes pass can advance metadata, execute pending work, and apply verified results.
    /// </summary>
    [Fact]
    public async Task AdvanceWithRemoteChangesAndExecuteAsyncRunsPendingWork()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 25, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        FakeSyncExecutor Executor = new(Request =>
        {
            Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
            {
                TrackedItemId = Request.Decision.TrackedItemId,
                ExistsFlag = true,
                RelativePath = "File1.txt",
                Name = "File1.txt",
                ItemType = "File",
                Size = 42,
                ContentHash = "hash-remote",
                ObservedTime = Time,
            });
        });

        MetadataSyncRunResult Result = await Session.AdvanceWithRemoteChangesAndExecuteAsync(
            "root-1",
            [CreateLocalScanItem("File1.txt", "hash-base", Time)],
            [
                new StorageChange(
                    "remote-1",
                    false,
                    new DateTimeOffset(Time),
                    CreateStorageItem("remote-1", "File1.txt", "hash-remote", 2)),
            ],
            CreateCheckpoint("token-7", Time),
            Time,
            Time,
            Time,
            "scan-7",
            Executor,
            CancellationToken.None);

        Assert.Single(Result.SessionResult.PendingExecutionRequests);
        Assert.Single(Executor.Requests);
        Assert.Single(Result.ExecutionApplyResult.CommittedResults);
        Assert.Equal("hash-remote", Store.GetBaseSnapshot("item-1").ContentHash);
    }
    /// <summary>
    /// Verifies a remote changes pass with untracked changes does not call the executor.
    /// </summary>
    [Fact]
    public async Task AdvanceWithRemoteChangesAndExecuteAsyncStopsBeforeExecutorForUntrackedChanges()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 30, 0, DateTimeKind.Utc);
        FakeSyncExecutor Executor = new(_ => { });

        Store.InsertSyncRoot(CreateSyncRoot());

        MetadataSyncRunResult Result = await Session.AdvanceWithRemoteChangesAndExecuteAsync(
            "root-1",
            [CreateLocalScanItem("Local.txt", "hash-local", Time)],
            [new StorageChange("remote-missing", true, new DateTimeOffset(Time), null)],
            CreateCheckpoint("token-8", Time),
            Time,
            Time,
            Time,
            "scan-8",
            Executor,
            CancellationToken.None);

        Assert.Single(Result.SessionResult.UntrackedRemoteChanges);
        Assert.Empty(Result.SessionResult.PendingExecutionRequests);
        Assert.Empty(Executor.Requests);
        Assert.Empty(Result.ExecutionApplyResult.CommittedResults);
        Assert.Null(Store.GetTrackedItemByLocalKey("root-1", "Local.txt"));
    }
    /// <summary>
    /// Verifies the unsupported executor blocks pending work without committing base snapshots.
    /// </summary>
    [Fact]
    public async Task AdvanceWithRemoteChangesAndExecuteAsyncBlocksUnsupportedExecution()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 32, 0, DateTimeKind.Utc);
        UnsupportedSyncExecutor Executor = new();

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);

        MetadataSyncRunResult Result = await Session.AdvanceWithRemoteChangesAndExecuteAsync(
            "root-1",
            [CreateLocalScanItem("File1.txt", "hash-base", Time)],
            [
                new StorageChange(
                    "remote-1",
                    false,
                    new DateTimeOffset(Time),
                    CreateStorageItem("remote-1", "File1.txt", "hash-remote", 2)),
            ],
            CreateCheckpoint("token-9", Time),
            Time,
            Time,
            Time,
            "scan-9",
            Executor,
            CancellationToken.None);

        Assert.Single(Result.SessionResult.PendingExecutionRequests);
        Assert.Empty(Result.ExecutionApplyResult.CommittedResults);
        Assert.Single(Result.ExecutionApplyResult.UncommittedResults);
        Assert.Equal(SyncExecutionResultKind.Blocked, Result.ExecutionApplyResult.UncommittedResults[0].ResultKind);
        Assert.Equal("hash-base", Store.GetBaseSnapshot("item-1").ContentHash);
    }
    /// <summary>
    /// Verifies the intent validating fake executor does not execute invalid requests.
    /// </summary>
    [Fact]
    public async Task IntentValidatingFakeExecutorRejectsInvalidRequests()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 35, 0, DateTimeKind.Utc);
        SyncExecutionRequest Request = CreateExecutionRequest(SyncPlanDecisionKind.PropagateLocalDelete);
        Request.TrackedItem.RemoteItemId = string.Empty;
        Request.RemoteObservation.RemoteItemId = string.Empty;
        FakeSyncExecutor Executor = new(_ => throw new InvalidOperationException("Invalid requests must not execute."));

        SyncExecutionApplyResult Result = await Session.ExecutePendingRequestsAsync(
            [Request],
            Executor,
            Time,
            CancellationToken.None);

        Assert.Empty(Executor.Intents);
        Assert.Empty(Executor.Requests);
        Assert.Empty(Result.CommittedResults);
        Assert.Single(Result.UncommittedResults);
        Assert.Equal(SyncExecutionResultKind.FailedPermanent, Result.UncommittedResults[0].ResultKind);
        Assert.Contains("Remote item id is required.", Result.UncommittedResults[0].Message);
    }
    /// <summary>
    /// Verifies local delete propagation commits the base snapshot as missing after remote trash succeeds.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsCommitsLocalDeletePropagationAsMissing()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 38, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddObservedItem(Store, "item-1", "remote-1", "File1.txt", "hash-base", Time);
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        Store.UpsertLocalObservation(LocalObservationMapper.Missing("item-1", Time, "scan-delete"));
        SyncExecutionRequest Request = Session.AdvanceMetadataOnly("root-1", Time).PendingExecutionRequests.Single();

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = Request,
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    RemoteItem = new StorageItem(
                        "remote-1",
                        "remote-root",
                        "File1.txt",
                        "/File1.txt",
                        StorageItemKind.File,
                        "text/plain",
                        42,
                        "hash-base",
                        default,
                        default,
                        2,
                        true),
                },
            ],
            Time);

        Assert.Single(Result.CommittedResults);
        Assert.Single(Result.CommittedBaseSnapshots);
        Assert.False(Store.GetBaseSnapshot("item-1").ExistsFlag);
        Assert.False(Store.GetLocalObservation("item-1").ExistsFlag);
        Assert.True(Store.GetRemoteObservation("item-1").Trashed);
        Assert.Equal(SyncDiffKind.NoChange, Session.ClassifySyncRoot("root-1").Single().DiffKind);
    }
    /// <summary>
    /// Verifies local folder delete propagation commits the folder subtree as missing after remote trash succeeds.
    /// </summary>
    [Fact]
    public void ApplyExecutionResultsCommitsLocalFolderDeleteSubtreeAsMissing()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 39, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(new TrackedItemRecord()
        {
            Id = "folder-item",
            SyncRootId = "root-1",
            RemoteItemId = "remote-folder",
            LocalKey = "Folder",
            ItemType = "Folder",
        });
        Store.InsertTrackedItem(CreateTrackedItem("file-item", "remote-file", "Folder/File1.txt"));
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            RelativePath = "Folder",
            Name = "Folder",
            ItemType = "Folder",
            ObservedTime = Time,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            RelativePath = "Folder/File1.txt",
            Name = "File1.txt",
            ParentRelativePath = "Folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            RemoteItemId = "remote-folder",
            ExistsFlag = true,
            Removed = false,
            Name = "Folder",
            RemoteParentId = "remote-root",
            ItemType = "Folder",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "file-item",
            RemoteItemId = "remote-file",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-folder",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "folder-item",
            ExistsFlag = true,
            ItemType = "Folder",
            Name = "Folder",
            LocalRelativePath = "Folder",
            RemoteParentId = "remote-root",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "file-item",
            ExistsFlag = true,
            ItemType = "File",
            Name = "File1.txt",
            LocalRelativePath = "Folder/File1.txt",
            RemoteParentId = "remote-folder",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = Time,
        });
        Store.UpsertLocalObservation(LocalObservationMapper.Missing("folder-item", Time, "scan-delete"));
        Store.UpsertLocalObservation(LocalObservationMapper.Missing("file-item", Time, "scan-delete"));
        Dictionary<string, SyncExecutionRequest> Requests = Session.AdvanceMetadataOnly("root-1", Time)
            .PendingExecutionRequests
            .ToDictionary(Item => Item.Decision.TrackedItemId);

        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = Requests["folder-item"],
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    RemoteItem = new StorageItem(
                        "remote-folder",
                        "remote-root",
                        "Folder",
                        "/Folder",
                        StorageItemKind.Folder,
                        "application/vnd.google-apps.folder",
                        0,
                        string.Empty,
                        default,
                        default,
                        2,
                        true),
                },
                new SyncExecutionResult()
                {
                    Request = Requests["file-item"],
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    RemoteItem = new StorageItem(
                        "remote-file",
                        "remote-folder",
                        "File1.txt",
                        "/Folder/File1.txt",
                        StorageItemKind.File,
                        "text/plain",
                        42,
                        "hash-base",
                        default,
                        default,
                        2,
                        true),
                },
            ],
            Time);

        Assert.Equal(2, Result.CommittedResults.Count);
        Assert.Equal(2, Result.CommittedBaseSnapshots.Count);
        Assert.False(Store.GetBaseSnapshot("folder-item").ExistsFlag);
        Assert.False(Store.GetBaseSnapshot("file-item").ExistsFlag);
        Assert.True(Store.GetRemoteObservation("folder-item").Trashed);
        Assert.True(Store.GetRemoteObservation("file-item").Trashed);
        Assert.All(Session.ClassifySyncRoot("root-1"), Item => Assert.Equal(SyncDiffKind.NoChange, Item.DiffKind));
    }
    /// <summary>
    /// Verifies a remote restore of a committed missing item creates a download and recommits the base as active.
    /// </summary>
    [Fact]
    public void RemoteRestoreOfCommittedMissingItemDownloadsAndCommitsActiveBase()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 41, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        Store.UpsertLocalObservation(LocalObservationMapper.Missing("item-1", Time, "scan-missing"));
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-base",
            ProviderVersion = 1,
            Trashed = true,
            ObservedTime = Time,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = false,
            CommittedTime = Time,
        });
        RemoteChangeImportResult ImportResult = Session.ImportRemoteChanges(
            "root-1",
            [
                new StorageChange(
                    "remote-1",
                    false,
                    new DateTimeOffset(Time),
                    CreateStorageItem("remote-1", "File1.txt", "hash-base", 2)),
            ],
            CreateCheckpoint("token-restore", Time),
            Time);

        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);
        SyncExecutionRequest Request = SessionResult.PendingExecutionRequests.Single();
        SyncExecutionApplyResult Result = Session.ApplyExecutionResults(
            [
                new SyncExecutionResult()
                {
                    Request = Request,
                    ResultKind = SyncExecutionResultKind.CompletedAndVerified,
                    RemoteItem = CreateStorageItem("remote-1", "File1.txt", "hash-base", 2),
                    LocalRelativePath = "File1.txt",
                },
            ],
            Time);

        Assert.Single(ImportResult.Observations);
        Assert.Equal(SyncPlanDecisionKind.DownloadToLocal, Request.Decision.DecisionKind);
        Assert.Single(Result.CommittedResults);
        Assert.True(Store.GetBaseSnapshot("item-1").ExistsFlag);
        Assert.True(Store.GetLocalObservation("item-1").ExistsFlag);
        Assert.False(Store.GetRemoteObservation("item-1").Trashed);
        Assert.Equal("File1.txt", Store.GetBaseSnapshot("item-1").LocalRelativePath);
        Assert.Equal(SyncDiffKind.NoChange, Session.ClassifySyncRoot("root-1").Single().DiffKind);
    }
    /// <summary>
    /// Verifies the intent validating fake executor returns conflict results without normal execution.
    /// </summary>
    [Fact]
    public async Task IntentValidatingFakeExecutorReturnsConflictResults()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 40, 0, DateTimeKind.Utc);
        SyncExecutionRequest Request = CreateExecutionRequest(SyncPlanDecisionKind.Conflict);
        FakeSyncExecutor Executor = new(_ => throw new InvalidOperationException("Conflict requests must not execute."));

        SyncExecutionApplyResult Result = await Session.ExecutePendingRequestsAsync(
            [Request],
            Executor,
            Time,
            CancellationToken.None);

        Assert.Empty(Executor.Intents);
        Assert.Empty(Executor.Requests);
        Assert.Empty(Result.CommittedResults);
        Assert.Single(Result.UncommittedResults);
        Assert.Equal(SyncExecutionResultKind.Conflict, Result.UncommittedResults[0].ResultKind);
        Assert.Contains("Conflict resolution is required.", Result.UncommittedResults[0].Message);
    }
    /// <summary>
    /// Verifies delete-modify conflicts are not passed to normal execution.
    /// </summary>
    [Fact]
    public async Task ExecutePendingRequestsAsyncDoesNotExecuteDeleteModifyConflicts()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        DateTime Time = new(2026, 7, 11, 8, 45, 0, DateTimeKind.Utc);
        FakeSyncExecutor Executor = new(_ => throw new InvalidOperationException("Delete-modify conflicts must not execute."));

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem("item-1", "remote-1", "File1.txt"));
        AddBaseSnapshot(Store, "item-1", "File1.txt", "hash-base", Time);
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = false,
            ObservedTime = Time,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "File1.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-remote",
            ProviderVersion = 2,
            Trashed = false,
            ObservedTime = Time,
        });
        MetadataSyncSessionResult SessionResult = Session.AdvanceMetadataOnly("root-1", Time);

        SyncExecutionApplyResult Result = await Session.ExecutePendingRequestsAsync(
            SessionResult.PendingExecutionRequests,
            Executor,
            Time,
            CancellationToken.None);

        Assert.Empty(Executor.Intents);
        Assert.Empty(Executor.Requests);
        Assert.Single(SessionResult.PendingExecutionRequests);
        Assert.Equal(SyncPlanDecisionKind.Conflict, SessionResult.PendingExecutionRequests[0].Decision.DecisionKind);
        Assert.Empty(Result.CommittedResults);
        Assert.Single(Result.UncommittedResults);
        Assert.Equal(SyncExecutionResultKind.Conflict, Result.UncommittedResults[0].ResultKind);
        Assert.Contains("Conflict resolution is required.", Result.UncommittedResults[0].Message);
    }
}
