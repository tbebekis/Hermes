// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests synchronization root settings synchronization.
/// </summary>
public class SyncRootSettingsSynchronizerTests
{
    // ● private

    /// <summary>
    /// Provides an isolated SQLite database for sync root settings tests.
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
            SysConfig.MainAssembly = typeof(SyncRootSettingsSynchronizerTests).Assembly;
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
            fFolder = Path.Combine(Path.GetTempPath(), "hermes-sync-root-settings-tests", Sys.GenId());
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

    static SyncSettings Settings() => new()
    {
        SyncRootId = "default",
        LocalRootPath = "/tmp/hermes",
        RemoteRootFolderId = "remote-root",
    };

    // ● public

    /// <summary>
    /// Verifies configured sync root insertion.
    /// </summary>
    [Fact]
    public void EnsureSyncRootInsertsConfiguredRoot()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        DateTime Time = new(2026, 7, 10, 10, 0, 0);

        SyncRootRecord Record = SyncRootSettingsSynchronizer.EnsureSyncRoot(Store, Settings(), Time);

        SyncRootRecord Loaded = Store.GetSyncRoot("default");
        Assert.Equal("default", Record.Id);
        Assert.Equal("GoogleDrive", Loaded.ProviderName);
        Assert.Equal("default", Loaded.ConnectionId);
        Assert.Equal("/tmp/hermes", Loaded.LocalRootPath);
        Assert.Equal("remote-root", Loaded.RemoteRootItemId);
        Assert.True(Loaded.IsEnabled);
        Assert.Equal(Time, Loaded.CreatedTime);
    }

    /// <summary>
    /// Verifies configured sync root update.
    /// </summary>
    [Fact]
    public void EnsureSyncRootUpdatesConfiguredRoot()
    {
        using TestDatabase Database = new();
        SqlMetadataStore Store = new(Database.Store);
        SyncSettings SyncSettings = Settings();
        DateTime Time = new(2026, 7, 10, 10, 0, 0);
        SyncRootSettingsSynchronizer.EnsureSyncRoot(Store, SyncSettings, Time);
        SyncSettings.LocalRootPath = "/tmp/hermes-updated";
        SyncSettings.RemoteRootFolderId = "remote-root-updated";

        SyncRootRecord Record = SyncRootSettingsSynchronizer.EnsureSyncRoot(Store, SyncSettings, Time.AddHours(1));

        Assert.Equal("/tmp/hermes-updated", Record.LocalRootPath);
        Assert.Equal("remote-root-updated", Record.RemoteRootItemId);
        Assert.Equal(Time, Record.CreatedTime);
    }
}
