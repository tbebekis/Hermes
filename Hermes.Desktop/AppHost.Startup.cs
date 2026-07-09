// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Contains application startup logic.
/// </summary>
static public partial class AppHost
{
    // ● private

    /// <summary>
    /// Initializes Tripous system configuration.
    /// </summary>
    static private void InitializeConfigs()
    {
        SysConfig.ApplicationMode = ApplicationMode.Desktop;
        SysConfig.MainAssembly = typeof(AppHost).Assembly;
        SysConfig.AppName = "Hermes";
    }

    /// <summary>
    /// Loads database connection settings.
    /// </summary>
    static private async Task LoadConnectionStrings()
    {
        DataLib.EnsureDefaultDbConnectionsFile();
        Db.Connections.Load();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates the database when it does not exist.
    /// </summary>
    static private async Task CreateDatabase()
    {
        DbConnectionInfo ConnectionInfo = Db.GetDefaultConnectionInfo();
        SqlProvider Provider = ConnectionInfo.GetSqlProvider();
        string ConnectionString = ConnectionInfo.ConnectionString;

        if (!Provider.DatabaseExists(ConnectionString) && Provider.CanCreateDatabases)
        {
            Provider.CreateDatabase(ConnectionString);
            await MessageBox.Info($"An empty SQLite database has been created.{Environment.NewLine}{Environment.NewLine}{ConnectionString}", HiddenMainWindow);
        }
    }

    /// <summary>
    /// Loads libraries required by the application.
    /// </summary>
    static private void LoadLibraries()
    {
        DataLib.Load();
    }

    /// <summary>
    /// Initializes libraries required by the application.
    /// </summary>
    static private void InitializeLibraries()
    {
        DataLib.Initialize();
    }

    // ● public

    /// <summary>
    /// Starts the application.
    /// </summary>
    /// <param name="AvaloniaDesktop">The Avalonia desktop lifetime.</param>
    static public async Task Start(IClassicDesktopStyleApplicationLifetime AvaloniaDesktop)
    {
        bool Flag = true;
        AppHost.AvaloniaDesktop = AvaloniaDesktop;
        Ui.MainWindow = HiddenMainWindow;

        try
        {
            InitializeConfigs();
            await LoadConnectionStrings();
            await CreateDatabase();
            Registry.RegisterSchemas();
            Schemas.Execute();
            Store = SqlStores.CreateDefaultSqlStore();
            LoadLibraries();
            TypeStore.RegisterLoadedAssemblies();
            Registry.RegisterDescriptors();
            InitializeLibraries();

            MainWindow = new MainWindow();
            Ui.MainWindow = MainWindow;
            MainWindow.Show();
        }
        catch (Exception Ex)
        {
            Console.WriteLine(Ex);
            await MessageBox.Error(Ex.Message, Ui.MainWindow);
            Flag = false;
        }

        if (!Flag)
        {
            Ui.MainWindow.Close();
            return;
        }

        DesktopExceptionHandler.Initialize();
    }
}
