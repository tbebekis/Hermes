// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests metadata store persistence.
/// </summary>
public class MetadataStoreTests
{
    // ● private

    /// <summary>
    /// Provides an isolated SQLite database for metadata store tests.
    /// </summary>
    sealed class TestDatabase : IDisposable
    {
        // ● private

        readonly string fFolder;
        readonly string fDatabasePath;
        bool fDisposed;
        void ConfigureApplication()
        {
            SysConfig.ApplicationMode = ApplicationMode.Service;
            SysConfig.MainAssembly = typeof(MetadataStoreTests).Assembly;
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
            fFolder = Path.Combine(Path.GetTempPath(), "hermes-metadata-tests", Sys.GenId());
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
        CreatedTime = new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc),
    };
    static TrackedItemRecord CreateTrackedItem() => new()
    {
        Id = "item-1",
        SyncRootId = "root-1",
        RemoteItemId = "remote-1",
        LocalKey = "local-1",
        ItemType = "File",
    };
    static TrackedItemRecord CreateTrackedItem2() => new()
    {
        Id = "item-2",
        SyncRootId = "root-1",
        RemoteItemId = "remote-2",
        LocalKey = "local-2",
        ItemType = "File",
    };
    static SyncRootRecord CreateSyncRoot2() => new()
    {
        Id = "root-2",
        ProviderName = "GoogleDrive",
        ConnectionId = "account-1",
        LocalRootPath = "/tmp/hermes-2",
        RemoteRootItemId = "remote-root-2",
        IsEnabled = true,
        CreatedTime = new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc),
    };

    // ● public

    /// <summary>
    /// Verifies sync root insert and update behavior.
    /// </summary>
    [Fact]
    public void SyncRootCanBeInsertedAndUpdated()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        SyncRootRecord Root = CreateSyncRoot();

        Store.InsertSyncRoot(Root);
        Root.IsEnabled = false;
        Root.LocalRootPath = "/tmp/hermes-renamed";
        Store.UpdateSyncRoot(Root);

        SyncRootRecord Loaded = Store.GetSyncRoot("root-1");

        Assert.NotNull(Loaded);
        Assert.False(Loaded.IsEnabled);
        Assert.Equal("/tmp/hermes-renamed", Loaded.LocalRootPath);
        Assert.Equal("remote-root", Loaded.RemoteRootItemId);
    }
    /// <summary>
    /// Verifies tracked item insert behavior.
    /// </summary>
    [Fact]
    public void TrackedItemCanBeInserted()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());

        TrackedItemRecord Loaded = Store.GetTrackedItem("item-1");

        Assert.NotNull(Loaded);
        Assert.Equal("root-1", Loaded.SyncRootId);
        Assert.Equal("remote-1", Loaded.RemoteItemId);
    }
    /// <summary>
    /// Verifies tracked items can be found by remote id in a sync root.
    /// </summary>
    [Fact]
    public void TrackedItemCanBeFoundByRemoteId()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());

        TrackedItemRecord Loaded = Store.GetTrackedItemByRemoteId("root-1", "remote-1");

        Assert.NotNull(Loaded);
        Assert.Equal("item-1", Loaded.Id);
    }
    /// <summary>
    /// Verifies tracked items can be found by local key in a sync root.
    /// </summary>
    [Fact]
    public void TrackedItemCanBeFoundByLocalKey()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());

        TrackedItemRecord Loaded = Store.GetTrackedItemByLocalKey("root-1", "local-1");

        Assert.NotNull(Loaded);
        Assert.Equal("item-1", Loaded.Id);
    }
    /// <summary>
    /// Verifies tracked item identity lookups are scoped by sync root.
    /// </summary>
    [Fact]
    public void TrackedItemIdentityLookupIsScopedBySyncRoot()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        TrackedItemRecord Item2 = new()
        {
            Id = "item-3",
            SyncRootId = "root-2",
            RemoteItemId = "remote-1",
            LocalKey = "local-1",
            ItemType = "File",
        };

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertSyncRoot(CreateSyncRoot2());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.InsertTrackedItem(Item2);

        Assert.Equal("item-1", Store.GetTrackedItemByRemoteId("root-1", "remote-1").Id);
        Assert.Equal("item-3", Store.GetTrackedItemByRemoteId("root-2", "remote-1").Id);
        Assert.Equal("item-1", Store.GetTrackedItemByLocalKey("root-1", "local-1").Id);
        Assert.Equal("item-3", Store.GetTrackedItemByLocalKey("root-2", "local-1").Id);
    }
    /// <summary>
    /// Verifies observation, base snapshot, and checkpoint upserts.
    /// </summary>
    [Fact]
    public void SnapshotsAndCheckpointCanBeUpserted()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime ObservedTime = new(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            ItemType = "File",
            Name = "Report.txt",
            LocalRelativePath = "Report.txt",
            RemoteParentId = "remote-root",
            Size = 42,
            ContentHash = "hash-1",
            CreatedTime = ObservedTime,
            ModifiedTime = ObservedTime,
            ProviderVersion = 7,
            Trashed = false,
            CommittedTime = ObservedTime,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = false,
            ObservedTime = ObservedTime,
            ScanId = "scan-1",
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "Report.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            MimeType = "text/plain",
            Size = 42,
            ContentHash = "hash-1",
            ProviderVersion = 7,
            Trashed = false,
            ProviderChangeTime = ObservedTime,
            ObservedTime = ObservedTime,
        });
        Store.UpsertRemoteCheckpoint(new RemoteCheckpointRecord()
        {
            SyncRootId = "root-1",
            ProviderName = "GoogleDrive",
            ConnectionId = "account-1",
            StartPageToken = "10",
            UpdatedTime = ObservedTime,
        });

        Assert.Equal("Report.txt", Store.GetBaseSnapshot("item-1").Name);
        Assert.False(Store.GetLocalObservation("item-1").ExistsFlag);
        Assert.Equal("text/plain", Store.GetRemoteObservation("item-1").MimeType);
        Assert.Equal("10", Store.GetRemoteCheckpoint("root-1").StartPageToken);
    }
    /// <summary>
    /// Verifies permanent delete tombstone persistence.
    /// </summary>
    [Fact]
    public void RemotePermanentDeleteTombstoneCanBeStored()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = false,
            Removed = true,
            ObservedTime = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc),
        });

        RemoteObservedSnapshotRecord Loaded = Store.GetRemoteObservation("item-1");

        Assert.NotNull(Loaded);
        Assert.Equal("remote-1", Loaded.RemoteItemId);
        Assert.False(Loaded.ExistsFlag);
        Assert.True(Loaded.Removed);
        Assert.Null(Loaded.Name);
    }
    /// <summary>
    /// Verifies remote observations and checkpoint are stored in one transaction.
    /// </summary>
    [Fact]
    public void RemoteObservationBatchAndCheckpointCanBeSavedAtomically()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime ObservedTime = new(2026, 7, 10, 13, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.SaveRemoteObservationsWithCheckpoint(
        [
            new RemoteObservedSnapshotRecord()
            {
                TrackedItemId = "item-1",
                RemoteItemId = "remote-1",
                ExistsFlag = true,
                Removed = false,
                Name = "Remote.txt",
                ObservedTime = ObservedTime,
            },
        ],
        new RemoteCheckpointRecord()
        {
            SyncRootId = "root-1",
            ProviderName = "GoogleDrive",
            ConnectionId = "account-1",
            StartPageToken = "20",
            UpdatedTime = ObservedTime,
        });

        Assert.Equal("Remote.txt", Store.GetRemoteObservation("item-1").Name);
        Assert.Equal("20", Store.GetRemoteCheckpoint("root-1").StartPageToken);
    }
    /// <summary>
    /// Verifies classifier input can be loaded from persisted snapshots.
    /// </summary>
    [Fact]
    public void DiffInputCanBeLoadedFromPersistedSnapshots()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime ObservedTime = new(2026, 7, 10, 14, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            ItemType = "File",
            Name = "Report.txt",
            LocalRelativePath = "Report.txt",
            RemoteParentId = "remote-root",
            Size = 42,
            ContentHash = "hash-1",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = ObservedTime,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "Report.txt",
            Name = "Report.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ObservedTime = ObservedTime,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "Report.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-1",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = ObservedTime,
        });

        SyncDiffClassifier Classifier = new();
        SyncDiffInput Input = Store.GetDiffInput("item-1");

        Assert.Equal(SyncDiffKind.LocalChanged, Classifier.Classify(Input));
    }
    /// <summary>
    /// Verifies tracked items can be batch classified for a sync root.
    /// </summary>
    [Fact]
    public void SyncRootTrackedItemsCanBeClassified()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime ObservedTime = new(2026, 7, 10, 15, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.InsertTrackedItem(CreateTrackedItem2());
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            ItemType = "File",
            Name = "Report1.txt",
            LocalRelativePath = "Report1.txt",
            RemoteParentId = "remote-root",
            Size = 42,
            ContentHash = "hash-1",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = ObservedTime,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "Report1.txt",
            Name = "Report1.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ObservedTime = ObservedTime,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "Report1.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-1",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = ObservedTime,
        });
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "item-2",
            ExistsFlag = true,
            ItemType = "File",
            Name = "Report2.txt",
            LocalRelativePath = "Report2.txt",
            RemoteParentId = "remote-root",
            Size = 42,
            ContentHash = "hash-1",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = ObservedTime,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-2",
            ExistsFlag = true,
            RelativePath = "Report2.txt",
            Name = "Report2.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-1",
            ObservedTime = ObservedTime,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-2",
            RemoteItemId = "remote-2",
            ExistsFlag = true,
            Removed = false,
            Name = "Report2.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-remote",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = ObservedTime,
        });

        IReadOnlyList<TrackedItemDiffRecord> Diffs = Store.ClassifySyncRoot("root-1");

        Assert.Equal(2, Diffs.Count);
        Assert.Equal("item-1", Diffs[0].TrackedItemId);
        Assert.Equal(SyncDiffKind.LocalChanged, Diffs[0].DiffKind);
        Assert.Equal("item-2", Diffs[1].TrackedItemId);
        Assert.Equal(SyncDiffKind.RemoteChanged, Diffs[1].DiffKind);
    }
    /// <summary>
    /// Verifies persisted classifications can be passed to the planner.
    /// </summary>
    [Fact]
    public void SyncRootPlanInputsCanBePlanned()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        SyncPlanner Planner = new();
        DateTime ObservedTime = new(2026, 7, 10, 16, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.UpsertBaseSnapshot(new BaseSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            ItemType = "File",
            Name = "Report.txt",
            LocalRelativePath = "Report.txt",
            RemoteParentId = "remote-root",
            Size = 42,
            ContentHash = "hash-1",
            ProviderVersion = 1,
            Trashed = false,
            CommittedTime = ObservedTime,
        });
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "Report.txt",
            Name = "Report.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-1",
            ObservedTime = ObservedTime,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "Report.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-remote",
            ProviderVersion = 1,
            Trashed = false,
            ObservedTime = ObservedTime,
        });

        IReadOnlyList<SyncPlanInput> Inputs = Store.CreatePlanInputs("root-1");
        IReadOnlyList<SyncPlanDecision> Decisions = Planner.CreateDecisions(Inputs);

        Assert.Single(Inputs);
        Assert.Single(Decisions);
        Assert.Equal("item-1", Decisions[0].TrackedItemId);
        Assert.Equal(SyncPlanDecisionKind.DownloadToLocal, Decisions[0].DecisionKind);
    }
    /// <summary>
    /// Verifies duplicate remote siblings are detected as namespace collisions.
    /// </summary>
    [Fact]
    public void DuplicateRemoteSiblingsAreDetectedAsNamespaceCollision()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime ObservedTime = new(2026, 7, 10, 17, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.InsertTrackedItem(CreateTrackedItem2());
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "DuplicateName.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            ObservedTime = ObservedTime,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-2",
            RemoteItemId = "remote-2",
            ExistsFlag = true,
            Removed = false,
            Name = "DuplicateName.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            ObservedTime = ObservedTime,
        });

        IReadOnlyList<NamespaceCollisionRecord> Collisions = Store.FindRemoteNamespaceCollisions("root-1");
        IReadOnlyList<TrackedItemDiffRecord> Diffs = Store.ClassifySyncRoot("root-1");

        Assert.Single(Collisions);
        Assert.Equal("remote-root", Collisions[0].RemoteParentId);
        Assert.Equal("DuplicateName.txt", Collisions[0].Name);
        Assert.Contains("item-1", Collisions[0].TrackedItemIds);
        Assert.Contains("item-2", Collisions[0].TrackedItemIds);
        Assert.Equal(SyncDiffKind.NamespaceCollision, Diffs[0].DiffKind);
        Assert.Equal(SyncDiffKind.NamespaceCollision, Diffs[1].DiffKind);
    }
    /// <summary>
    /// Verifies duplicate remote siblings block planner decisions.
    /// </summary>
    [Fact]
    public void DuplicateRemoteSiblingsCreateBlockedPlannerDecisions()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        SyncPlanner Planner = new();
        DateTime ObservedTime = new(2026, 7, 10, 18, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.InsertTrackedItem(CreateTrackedItem2());
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            RemoteItemId = "remote-1",
            ExistsFlag = true,
            Removed = false,
            Name = "DuplicateName.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            ObservedTime = ObservedTime,
        });
        Store.UpsertRemoteObservation(new RemoteObservedSnapshotRecord()
        {
            TrackedItemId = "item-2",
            RemoteItemId = "remote-2",
            ExistsFlag = true,
            Removed = false,
            Name = "DuplicateName.txt",
            RemoteParentId = "remote-root",
            ItemType = "File",
            ObservedTime = ObservedTime,
        });

        IReadOnlyList<SyncPlanDecision> Decisions = Planner.CreateDecisions(Store.CreatePlanInputs("root-1"));

        Assert.Equal(2, Decisions.Count);
        Assert.All(Decisions, Item => Assert.Equal(SyncPlanDecisionKind.Blocked, Item.DecisionKind));
    }
    /// <summary>
    /// Verifies a local-only item without base state plans as upload.
    /// </summary>
    [Fact]
    public void LocalOnlyNewItemPlansAsUpload()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        SyncPlanner Planner = new();
        DateTime ObservedTime = new(2026, 7, 10, 19, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.UpsertLocalObservation(new LocalObservedSnapshotRecord()
        {
            TrackedItemId = "item-1",
            ExistsFlag = true,
            RelativePath = "LocalOnly.txt",
            Name = "LocalOnly.txt",
            ItemType = "File",
            Size = 42,
            ContentHash = "hash-local",
            ObservedTime = ObservedTime,
        });

        IReadOnlyList<SyncPlanDecision> Decisions = Planner.CreateDecisions(Store.CreatePlanInputs("root-1"));

        Assert.Single(Decisions);
        Assert.Equal(SyncPlanDecisionKind.UploadToRemote, Decisions[0].DecisionKind);
    }
    /// <summary>
    /// Verifies a remote-only item without base state plans as download.
    /// </summary>
    [Fact]
    public void RemoteOnlyNewItemPlansAsDownload()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        SyncPlanner Planner = new();
        DateTime ObservedTime = new(2026, 7, 10, 20, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
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
            ObservedTime = ObservedTime,
        });

        IReadOnlyList<SyncPlanDecision> Decisions = Planner.CreateDecisions(Store.CreatePlanInputs("root-1"));

        Assert.Single(Decisions);
        Assert.Equal(SyncPlanDecisionKind.DownloadToLocal, Decisions[0].DecisionKind);
    }
    /// <summary>
    /// Verifies local observations can be saved in a batch.
    /// </summary>
    [Fact]
    public void LocalObservationsCanBeSavedInBatch()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime ObservedTime = new(2026, 7, 10, 21, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.InsertTrackedItem(CreateTrackedItem2());
        Store.SaveLocalObservations(
        [
            LocalObservationMapper.FromScanItem(
                new LocalScanItem()
                {
                    RelativePath = "File1.txt",
                    Name = "File1.txt",
                    ItemType = "File",
                    Size = 5,
                    ModifiedTime = ObservedTime,
                    ContentHash = "hash-1",
                },
                "item-1",
                ObservedTime,
                "scan-1"),
            LocalObservationMapper.Missing("item-2", ObservedTime, "scan-1"),
        ]);

        LocalObservedSnapshotRecord Existing = Store.GetLocalObservation("item-1");
        LocalObservedSnapshotRecord Missing = Store.GetLocalObservation("item-2");

        Assert.True(Existing.ExistsFlag);
        Assert.Equal("File1.txt", Existing.RelativePath);
        Assert.Equal("hash-1", Existing.ContentHash);
        Assert.False(Missing.ExistsFlag);
        Assert.Null(Missing.RelativePath);
        Assert.Equal("scan-1", Missing.ScanId);
    }
    /// <summary>
    /// Verifies remote observations can be saved in a batch.
    /// </summary>
    [Fact]
    public void RemoteObservationsCanBeSavedInBatch()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime ObservedTime = new(2026, 7, 10, 22, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());
        Store.InsertTrackedItem(CreateTrackedItem2());
        Store.SaveRemoteObservations(
        [
            RemoteObservationMapper.FromStorageItem(
                new StorageItem(
                    "remote-1",
                    "remote-root",
                    "Remote1.txt",
                    "/Remote1.txt",
                    StorageItemKind.File,
                    "text/plain",
                    42,
                    "hash-1",
                    default,
                    default,
                    1,
                    false),
                "item-1",
                ObservedTime),
            RemoteObservationMapper.FromChange(
                new StorageChange("remote-2", true, new DateTimeOffset(ObservedTime), null),
                "item-2",
                ObservedTime),
        ]);

        RemoteObservedSnapshotRecord Existing = Store.GetRemoteObservation("item-1");
        RemoteObservedSnapshotRecord Removed = Store.GetRemoteObservation("item-2");

        Assert.True(Existing.ExistsFlag);
        Assert.Equal("Remote1.txt", Existing.Name);
        Assert.Equal("hash-1", Existing.ContentHash);
        Assert.False(Removed.ExistsFlag);
        Assert.True(Removed.Removed);
        Assert.Null(Removed.Name);
    }
    /// <summary>
    /// Verifies known provider changes can be imported as remote observations.
    /// </summary>
    [Fact]
    public void KnownRemoteChangesCanBeImported()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime ObservedTime = new(2026, 7, 10, 23, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());

        RemoteChangeImportResult Result = Store.ImportKnownRemoteChanges(
            "root-1",
            [
                new StorageChange(
                    "remote-1",
                    false,
                    new DateTimeOffset(ObservedTime),
                    new StorageItem(
                        "remote-1",
                        "remote-root",
                        "Changed.txt",
                        "/Changed.txt",
                        StorageItemKind.File,
                        "text/plain",
                        42,
                        "hash-1",
                        default,
                        default,
                        3,
                        false)),
            ],
            ObservedTime);
        RemoteObservedSnapshotRecord Observation = Store.GetRemoteObservation("item-1");

        Assert.Single(Result.Observations);
        Assert.Empty(Result.UntrackedChanges);
        Assert.NotNull(Observation);
        Assert.Equal("Changed.txt", Observation.Name);
        Assert.Equal("remote-1", Observation.RemoteItemId);
        Assert.Equal(3, Observation.ProviderVersion);
    }
    /// <summary>
    /// Verifies unknown provider changes are returned without creating observations.
    /// </summary>
    [Fact]
    public void UnknownRemoteChangesAreReturnedAsUntracked()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime ObservedTime = new(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());

        RemoteChangeImportResult Result = Store.ImportKnownRemoteChanges(
            "root-1",
            [
                new StorageChange("remote-unknown", true, new DateTimeOffset(ObservedTime), null),
            ],
            ObservedTime);

        Assert.Empty(Result.Observations);
        Assert.Single(Result.UntrackedChanges);
        Assert.Equal("remote-unknown", Result.UntrackedChanges[0].ItemId);
    }
    /// <summary>
    /// Verifies remote items can be bootstrapped as tracked items.
    /// </summary>
    [Fact]
    public void RemoteItemsCanBeBootstrappedAsTrackedItems()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime ObservedTime = new(2026, 7, 11, 1, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());

        RemoteBootstrapResult Result = Store.BootstrapRemoteItems(
            "root-1",
            [
                new StorageItem(
                    "remote-new",
                    "remote-root",
                    "RemoteNew.txt",
                    "/RemoteNew.txt",
                    StorageItemKind.File,
                    "text/plain",
                    42,
                    "hash-new",
                    default,
                    default,
                    1,
                    false),
            ],
            ObservedTime);
        TrackedItemRecord TrackedItem = Store.GetTrackedItemByRemoteId("root-1", "remote-new");
        RemoteObservedSnapshotRecord Observation = Store.GetRemoteObservation(TrackedItem.Id);

        Assert.Single(Result.CreatedTrackedItems);
        Assert.Single(Result.Observations);
        Assert.NotNull(TrackedItem);
        Assert.Equal("File", TrackedItem.ItemType);
        Assert.Null(TrackedItem.LocalKey);
        Assert.Equal("RemoteNew.txt", Observation.Name);
        Assert.Equal("hash-new", Observation.ContentHash);
    }
    /// <summary>
    /// Verifies bootstrapping an already tracked remote item only updates its observation.
    /// </summary>
    [Fact]
    public void RemoteBootstrapUpdatesExistingTrackedItems()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime ObservedTime = new(2026, 7, 11, 2, 0, 0, DateTimeKind.Utc);

        Store.InsertSyncRoot(CreateSyncRoot());
        Store.InsertTrackedItem(CreateTrackedItem());

        RemoteBootstrapResult Result = Store.BootstrapRemoteItems(
            "root-1",
            [
                new StorageItem(
                    "remote-1",
                    "remote-root",
                    "RemoteRenamed.txt",
                    "/RemoteRenamed.txt",
                    StorageItemKind.File,
                    "text/plain",
                    50,
                    "hash-renamed",
                    default,
                    default,
                    2,
                    false),
            ],
            ObservedTime);
        IReadOnlyList<TrackedItemRecord> Items = Store.GetTrackedItems("root-1");
        RemoteObservedSnapshotRecord Observation = Store.GetRemoteObservation("item-1");

        Assert.Empty(Result.CreatedTrackedItems);
        Assert.Single(Result.Observations);
        Assert.Single(Items);
        Assert.Equal("RemoteRenamed.txt", Observation.Name);
        Assert.Equal("hash-renamed", Observation.ContentHash);
        Assert.Equal(2, Observation.ProviderVersion);
    }
}
