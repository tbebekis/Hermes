// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Initializes Tripous data services for the background service.
/// </summary>
static public class ServiceDataStartup
{
    // ● private

    static void ConfigureSystem()
    {
        SysConfig.ApplicationMode = ApplicationMode.Service;
        SysConfig.MainAssembly = typeof(ServiceDataStartup).Assembly;
        SysConfig.AppName = CommonConstants.ApplicationName;
    }
    static void LoadConnectionStrings()
    {
        DataLib.EnsureDefaultDbConnectionsFile();
        Db.Connections.Load();
    }
    static void CreateDatabaseIfMissing()
    {
        DbConnectionInfo ConnectionInfo = Db.GetDefaultConnectionInfo();
        SqlProvider Provider = ConnectionInfo.GetSqlProvider();
        string ConnectionString = ConnectionInfo.ConnectionString;

        if (!Provider.DatabaseExists(ConnectionString) && Provider.CanCreateDatabases)
            Provider.CreateDatabase(ConnectionString);
    }
    static void InitializeSchema()
    {
        Registry.RegisterSchemas();
        Schemas.Execute();
    }
    static void InitializeLibraries()
    {
        DataLib.Load();
        TypeStore.RegisterLoadedAssemblies();
        Registry.RegisterDescriptors();
        DataLib.Initialize();
    }

    // ● public

    /// <summary>
    /// Initializes data services and returns the default SQL store.
    /// </summary>
    static public SqlStore CreateDefaultStore()
    {
        ConfigureSystem();
        LoadConnectionStrings();
        CreateDatabaseIfMissing();
        InitializeSchema();
        InitializeLibraries();

        return SqlStores.CreateDefaultSqlStore();
    }
}
