// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests metadata synchronization runner behavior.
/// </summary>
public class MetadataSyncRunnerTests
{
    // ● private

    /// <summary>
    /// Provides an isolated SQLite database for metadata sync runner tests.
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
            SysConfig.MainAssembly = typeof(MetadataSyncRunnerTests).Assembly;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDatabase"/> class.
        /// </summary>
        public TestDatabase()
        {
            fFolder = Path.Combine(Path.GetTempPath(), "hermes-metadata-runner-tests", Sys.GenId());
            fDatabasePath = Path.Combine(fFolder, "Hermes.db3");

            Directory.CreateDirectory(fFolder);
            ConfigureApplication();
            CreateDatabase();
        }

        // ● public

        /// <summary>
        /// Deletes the temporary database folder.
        /// </summary>
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

        /// <summary>
        /// Gets the SQL store.
        /// </summary>
        public SqlStore Store { get; private set; }
    }

    /// <summary>
    /// Provides a temporary local root folder for metadata sync runner tests.
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hermes-metadata-runner-local-tests", Guid.NewGuid().ToString("N"));
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

    /// <summary>
    /// Storage provider used by metadata sync runner tests.
    /// </summary>
    sealed class FakeStorageProvider : IStorageProvider
    {
        // ● public

        /// <inheritdoc/>
        public Task<StorageResult<string>> GetStartPageTokenAsync(CancellationToken CancellationToken)
        {
            StartPageTokenCalls++;
            return Task.FromResult(StorageResult<string>.Success(StartPageToken));
        }
        /// <inheritdoc/>
        public Task<StorageResult<StorageItem>> GetItemAsync(string ItemId, CancellationToken CancellationToken)
        {
            return Task.FromResult(StorageResult<StorageItem>.Success(ItemsById[ItemId]));
        }
        /// <inheritdoc/>
        public Task<StorageResult<IReadOnlyList<StorageItem>>> ListFolderAsync(string FolderId, CancellationToken CancellationToken)
        {
            ListedFolderIds.Add(FolderId);

            if (!FolderItems.TryGetValue(FolderId, out IReadOnlyList<StorageItem> Items))
                Items = [];

            return Task.FromResult(StorageResult<IReadOnlyList<StorageItem>>.Success(Items));
        }
        /// <inheritdoc/>
        public Task<StorageResult<StorageChangeListResult>> ListChangesAsync(string PageToken, CancellationToken CancellationToken)
        {
            ListChangesTokens.Add(PageToken);
            return Task.FromResult(StorageResult<StorageChangeListResult>.Success(ChangeListResult));
        }

        // ● properties

        /// <inheritdoc/>
        public string Name => "Fake";
        /// <inheritdoc/>
        public StorageProviderCapabilities Capabilities { get; } = new();
        /// <summary>
        /// Gets the items by id.
        /// </summary>
        public Dictionary<string, StorageItem> ItemsById { get; } = new();
        /// <summary>
        /// Gets the folder items by parent id.
        /// </summary>
        public Dictionary<string, IReadOnlyList<StorageItem>> FolderItems { get; } = new();
        /// <summary>
        /// Gets the listed folder ids.
        /// </summary>
        public List<string> ListedFolderIds { get; } = new();
        /// <summary>
        /// Gets the page tokens used for changes.
        /// </summary>
        public List<string> ListChangesTokens { get; } = new();
        /// <summary>
        /// Gets or sets the start page token returned by the provider.
        /// </summary>
        public string StartPageToken { get; set; } = "token-1";
        /// <summary>
        /// Gets or sets the change list result.
        /// </summary>
        public StorageChangeListResult ChangeListResult { get; set; } = new("token-1", "token-2", []);
        /// <summary>
        /// Gets the number of start page token calls.
        /// </summary>
        public int StartPageTokenCalls { get; private set; }
    }

    /// <summary>
    /// Completes all executable sync requests.
    /// </summary>
    sealed class CompletingExecutor : SyncExecutorBase
    {
        // ● protected

        /// <inheritdoc/>
        protected override Task<SyncExecutionResult> ExecuteIntentAsync(SyncExecutionIntent Intent, CancellationToken CancellationToken)
        {
            Requests.Add(Intent.Request);
            return Task.FromResult(SyncExecutionResultFactory.Completed(Intent.Request));
        }

        // ● properties

        /// <summary>
        /// Gets the execution requests.
        /// </summary>
        public List<SyncExecutionRequest> Requests { get; } = new();
    }

    static SyncRootRecord CreateSyncRoot(string LocalRootPath) => new()
    {
        Id = "root-1",
        ProviderName = "Fake",
        ConnectionId = "account-1",
        LocalRootPath = LocalRootPath,
        RemoteRootItemId = "remote-root",
        IsEnabled = true,
        CreatedTime = new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc),
    };
    static StorageItem FileItem(string Id, string ParentId, string Name) => new(Id, ParentId, Name, "/" + Name, StorageItemKind.File);
    static StorageItem FolderItem(string Id, string ParentId, string Name) => new(Id, ParentId, Name, "/" + Name, StorageItemKind.Folder);
    static MetadataSyncRunner CreateRunner(SqlMetadataStore Store, IStorageProvider Provider, ISyncExecutor Executor)
    {
        MetadataSyncSession Session = new(Store, new SyncPlanner());
        return new MetadataSyncRunner(Store, Session, new LocalScanner(), Provider, Executor);
    }

    // ● public

    /// <summary>
    /// Verifies bootstrap imports a recursive remote snapshot and stores the checkpoint.
    /// </summary>
    [Fact]
    public async Task RunOnceAsyncBootstrapsRemoteSnapshot()
    {
        using TestDatabase Database = new();
        using TempFolder Folder = new();
        SqlMetadataStore Store = new(Database.Store);
        Store.InsertSyncRoot(CreateSyncRoot(Folder.Path));
        FakeStorageProvider Provider = new();
        StorageItem RemoteFolder = FolderItem("remote-folder", "remote-root", "Folder");
        StorageItem RemoteFile = FileItem("remote-file", "remote-folder", "File.txt");
        Provider.FolderItems["remote-root"] = [RemoteFolder];
        Provider.FolderItems["remote-folder"] = [RemoteFile];
        Provider.ItemsById[RemoteFolder.Id] = RemoteFolder;
        Provider.ItemsById[RemoteFile.Id] = RemoteFile;
        CompletingExecutor Executor = new();
        MetadataSyncRunner Runner = CreateRunner(Store, Provider, Executor);

        Result<MetadataSyncRunResult> Result = await Runner.RunOnceAsync("root-1", CancellationToken.None);

        Assert.True(Result.Succeeded);
        Assert.Equal(MetadataSyncRunKind.Bootstrap, Result.Value.Kind);
        Assert.Equal(0, Result.Value.LocalObservedItemCount);
        Assert.Equal(2, Result.Value.RemoteObservedItemCount);
        Assert.Equal(0, Result.Value.RemoteObservedChangeCount);
        Assert.Equal(["remote-root", "remote-folder"], Provider.ListedFolderIds);
        Assert.Equal("token-1", Store.GetRemoteCheckpoint("root-1").StartPageToken);
        Assert.NotNull(Store.GetTrackedItemByRemoteId("root-1", "remote-folder"));
        Assert.NotNull(Store.GetTrackedItemByRemoteId("root-1", "remote-file"));
    }

    /// <summary>
    /// Verifies incremental runs use the stored checkpoint and advance it from changes.
    /// </summary>
    [Fact]
    public async Task RunOnceAsyncImportsRemoteChanges()
    {
        using TestDatabase Database = new();
        using TempFolder Folder = new();
        SqlMetadataStore Store = new(Database.Store);
        Store.InsertSyncRoot(CreateSyncRoot(Folder.Path));
        Store.UpsertRemoteCheckpoint(new RemoteCheckpointRecord()
        {
            SyncRootId = "root-1",
            ProviderName = "Fake",
            ConnectionId = "account-1",
            StartPageToken = "token-1",
            UpdatedTime = new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc),
        });
        FakeStorageProvider Provider = new()
        {
            ChangeListResult = new StorageChangeListResult("token-1", "token-2", []),
        };
        CompletingExecutor Executor = new();
        MetadataSyncRunner Runner = CreateRunner(Store, Provider, Executor);

        Result<MetadataSyncRunResult> Result = await Runner.RunOnceAsync("root-1", CancellationToken.None);

        Assert.True(Result.Succeeded);
        Assert.Equal(MetadataSyncRunKind.Incremental, Result.Value.Kind);
        Assert.Equal(0, Result.Value.LocalObservedItemCount);
        Assert.Equal(0, Result.Value.RemoteObservedItemCount);
        Assert.Equal(0, Result.Value.RemoteObservedChangeCount);
        Assert.Equal(["token-1"], Provider.ListChangesTokens);
        Assert.Equal(0, Provider.StartPageTokenCalls);
        Assert.Equal("token-2", Store.GetRemoteCheckpoint("root-1").StartPageToken);
    }
}
