// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace TestApp;

/// <summary>
/// Contains application startup logic.
/// </summary>
static public partial class AppHost
{
    // ● private

    /// <summary>
    /// Initializes Tripous system configuration.
    /// </summary>
    static void InitializeConfigs()
    {
        SysConfig.ApplicationMode = ApplicationMode.Desktop;
        SysConfig.MainAssembly = typeof(AppHost).Assembly;
        SysConfig.AppName = "Hermes";
    }

    /// <summary>
    /// Loads database connection settings.
    /// </summary>
    static async Task LoadConnectionStrings()
    {
        DataLib.EnsureDefaultDbConnectionsFile();
        Db.Connections.Load();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates the database when it does not exist.
    /// </summary>
    static async Task CreateDatabase()
    {
        DbConnectionInfo ConnectionInfo = Db.GetDefaultConnectionInfo();
        SqlProvider Provider = ConnectionInfo.GetSqlProvider();
        string ConnectionString = ConnectionInfo.ConnectionString;

        if (!Provider.DatabaseExists(ConnectionString) && Provider.CanCreateDatabases)
        {
            Provider.CreateDatabase(ConnectionString);
            await Tripous.Desktop.MessageBox.Info($"An empty SQLite database has been created.{Environment.NewLine}{Environment.NewLine}{ConnectionString}", StartupWindow);
        }
    }

    /// <summary>
    /// Loads libraries required by the application.
    /// </summary>
    static void LoadLibraries()
    {
        DataLib.Load();
    }

    /// <summary>
    /// Initializes libraries required by the application.
    /// </summary>
    static void InitializeLibraries()
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
        Tripous.Desktop.Ui.MainWindow = StartupWindow;

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
            AvaloniaDesktop.MainWindow = MainWindow;
            Tripous.Desktop.Ui.MainWindow = MainWindow;
            MainWindow.Show();
            StartupWindow.Close();
        }
        catch (Exception Ex)
        {
            Console.WriteLine(Ex);
            await Tripous.Desktop.MessageBox.Error(Ex.Message, Tripous.Desktop.Ui.MainWindow);
            Flag = false;
        }

        if (!Flag)
        {
            Tripous.Desktop.Ui.MainWindow.Close();
            return;
        }

        Tripous.Desktop.DesktopExceptionHandler.Initialize();
    }
}
