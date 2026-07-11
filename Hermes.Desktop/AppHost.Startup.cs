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
            await Tripous.Desktop.MessageBox.Info($"An empty SQLite database has been created.{Environment.NewLine}{Environment.NewLine}{ConnectionString}", StartupWindow);
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
    static private async Task ShowProgress(string Status, int StepIndex, double Percent)
    {
        StartupWindow.SetProgress(Status, StepIndex, Percent);
        await Task.Delay(80);
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
            await ShowProgress("Loading settings...", 0, 12);
            InitializeConfigs();
            await LoadConnectionStrings();
            await ShowProgress("Opening metadata store...", 1, 32);
            await CreateDatabase();
            Registry.RegisterSchemas();
            Schemas.Execute();
            Store = SqlStores.CreateDefaultSqlStore();
            await ShowProgress("Loading synchronization jobs...", 2, 58);
            LoadLibraries();
            TypeStore.RegisterLoadedAssemblies();
            Registry.RegisterDescriptors();
            InitializeLibraries();
            await ShowProgress("Connecting to service...", 3, 78);
            await ShowProgress("Ready", 4, 100);

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
