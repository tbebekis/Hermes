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
}
