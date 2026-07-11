// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Main desktop dashboard window.
/// </summary>
public partial class MainWindow : Window
{
    // ● private

    /// <summary>
    /// Describes a navigation page.
    /// </summary>
    class PageDescriptor
    {
        // ● constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PageDescriptor"/> class.
        /// </summary>
        public PageDescriptor(string Key, string Title, string Subtitle, string IconName, UserControl Page)
        {
            this.Key = Key;
            this.Title = Title;
            this.Subtitle = Subtitle;
            this.IconName = IconName;
            this.Page = Page;
        }

        // ● properties

        /// <summary>
        /// Gets the page key.
        /// </summary>
        public string Key { get; }
        /// <summary>
        /// Gets the page title.
        /// </summary>
        public string Title { get; }
        /// <summary>
        /// Gets the page subtitle.
        /// </summary>
        public string Subtitle { get; }
        /// <summary>
        /// Gets the page icon name.
        /// </summary>
        public string IconName { get; }
        /// <summary>
        /// Gets the page control.
        /// </summary>
        public UserControl Page { get; }
    }

    readonly ListBox fNavigationList;
    readonly TextBlock fPageTitleText;
    readonly TextBlock fPageSubtitleText;
    readonly ContentControl fPageHost;
    readonly TextBlock fServiceStatusText;
    readonly TextBlock fSyncStatusText;
    readonly TextBlock fConnectionStatusText;
    readonly TextBlock fUpdatedTimeText;
    readonly List<PageDescriptor> fPages;
    readonly LocalServiceClient fServiceClient;
    readonly DashboardPage fDashboardPage;
    readonly SynchronizationPage fSynchronizationPage;
    readonly ConnectionsPage fConnectionsPage;
    readonly ServicePage fServicePage;
    readonly FoldersPage fFoldersPage;
    readonly ActivityPage fActivityPage;
    readonly ConflictsPage fConflictsPage;
    readonly HistoryPage fHistoryPage;
    readonly LogsPage fLogsPage;
    readonly SettingsPage fSettingsPage;
    readonly LocalServiceProcessController fServiceProcessController;
    readonly DispatcherTimer fRefreshTimer;
    bool fRefreshInProgress;

    static Image CreateLogo(double Size)
    {
        Uri Uri = new("avares://Hermes.Desktop/Resources/Images/Hermes_Coin.jpg");
        using Stream Stream = AssetLoader.Open(Uri);

        return new Image()
        {
            Source = new Bitmap(Stream),
            Width = Size,
            Height = Size,
            Stretch = Stretch.UniformToFill,
        };
    }
    static TextBlock CreateStatusText(string Text)
    {
        return new TextBlock()
        {
            Text = Text,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
        };
    }
    static Image CreateSidebarIcon(string IconName)
    {
        Uri Uri = new("avares://Hermes.Desktop/Resources/Images/Sidebar/" + IconName);
        using Stream Stream = AssetLoader.Open(Uri);

        return new Image()
        {
            Source = new Bitmap(Stream),
            Width = 20,
            Height = 20,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }
    static ListBoxItem CreateNavigationItem(PageDescriptor Page)
    {
        return new ListBoxItem()
        {
            Content = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Children =
                {
                    CreateSidebarIcon(Page.IconName),
                    new TextBlock()
                    {
                        Text = Page.Title,
                        VerticalAlignment = VerticalAlignment.Center,
                    }
                }
            },
            Tag = Page,
            Padding = new Thickness(12, 9),
        };
    }
    Border CreateSidebar()
    {
        StackPanel Brand = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(12, 16, 12, 18),
            Children =
            {
                CreateLogo(40),
                new TextBlock()
                {
                    Text = "Hermes",
                    FontSize = 21,
                    FontWeight = FontWeight.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                }
            }
        };

        foreach (PageDescriptor Page in fPages)
            fNavigationList.Items.Add(CreateNavigationItem(Page));

        fNavigationList.SelectionChanged += NavigationList_SelectionChanged;

        Grid SidebarGrid = new()
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Children =
            {
                Brand,
                fNavigationList,
            }
        };
        Grid.SetRow(fNavigationList, 1);

        return new Border()
        {
            Background = Brushes.WhiteSmoke,
            BorderBrush = Brushes.Gainsboro,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = SidebarGrid,
        };
    }
    Border CreateHeader()
    {
        return new Border()
        {
            Padding = new Thickness(24, 18, 24, 16),
            BorderBrush = Brushes.Gainsboro,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = new StackPanel()
            {
                Spacing = 3,
                Children =
                {
                    fPageTitleText,
                    fPageSubtitleText,
                }
            }
        };
    }
    Border CreateStatusBar()
    {
        StackPanel Panel = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 18,
            Children =
            {
                fServiceStatusText,
                fSyncStatusText,
                fConnectionStatusText,
                fUpdatedTimeText,
            }
        };

        return new Border()
        {
            Padding = new Thickness(16, 6),
            BorderBrush = Brushes.Gainsboro,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = Panel,
        };
    }
    Grid CreateMainArea()
    {
        fPageHost.HorizontalAlignment = HorizontalAlignment.Stretch;
        fPageHost.VerticalAlignment = VerticalAlignment.Stretch;

        Grid Result = new()
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Children =
            {
                CreateHeader(),
                new Border()
                {
                    Child = fPageHost,
                    Padding = new Thickness(24),
                },
                CreateStatusBar(),
            }
        };
        Grid.SetRow(Result.Children[1], 1);
        Grid.SetRow(Result.Children[2], 2);
        return Result;
    }
    Grid CreateLayout()
    {
        Border Sidebar = CreateSidebar();
        Grid Main = CreateMainArea();
        Grid.SetColumn(Main, 1);

        return new Grid()
        {
            ColumnDefinitions = new ColumnDefinitions("220,*"),
            Children =
            {
                Sidebar,
                Main,
            }
        };
    }
    void Navigate(PageDescriptor Page)
    {
        fPageTitleText.Text = Page.Title;
        fPageSubtitleText.Text = Page.Subtitle;
        fPageHost.Content = Page.Page;
        fUpdatedTimeText.Text = "Updated " + DateTime.Now.ToString("HH:mm:ss");
    }
    async Task RefreshServiceStatusAsync(bool LogDiagnostics)
    {
        if (fRefreshInProgress)
            return;

        fRefreshInProgress = true;

        try
        {
            if (LogDiagnostics)
                fServicePage.AppendMemo("Refreshing service status.");

            LocalServiceStatus Status = await fServiceClient.GetStatusAsync();
            fDashboardPage.SetStatus(Status);
            fSynchronizationPage.SetStatus(Status);
            fConnectionsPage.SetStatus(Status);
            fServicePage.SetStatus(Status);
            fFoldersPage.SetStatus(Status);
            fSettingsPage.SetStatus(Status);

            if (Status == null)
            {
                fServiceStatusText.Text = "Service: Stopped";
                fSyncStatusText.Text = "Sync: Unknown";
                fConnectionStatusText.Text = "HTTP API: Disconnected";
                fActivityPage.SetActivities(null);
                fConflictsPage.SetConflicts(null);
                fLogsPage.SetLogs(null);

                if (!string.IsNullOrWhiteSpace(fServiceClient.LastErrorMessage))
                {
                    if (LogDiagnostics)
                        fServicePage.AppendMemo("Service is not reachable: " + fServiceClient.LastErrorMessage);
                }
            }
            else
            {
                IReadOnlyList<LocalRecentLog> RecentLogs = await fServiceClient.GetRecentLogsAsync();
                if (LogDiagnostics && RecentLogs == null && !string.IsNullOrWhiteSpace(fServiceClient.LastErrorMessage))
                    fServicePage.AppendMemo("Logs error: " + fServiceClient.LastErrorMessage);
                IReadOnlyList<LocalSyncActivity> RecentActivity = await fServiceClient.GetRecentActivityAsync();
                if (LogDiagnostics && RecentActivity == null && !string.IsNullOrWhiteSpace(fServiceClient.LastErrorMessage))
                    fServicePage.AppendMemo("Activity error: " + fServiceClient.LastErrorMessage);
                fActivityPage.SetActivities(RecentActivity);
                IReadOnlyList<LocalOpenConflict> OpenConflicts = await fServiceClient.GetOpenConflictsAsync();
                if (LogDiagnostics && OpenConflicts == null && !string.IsNullOrWhiteSpace(fServiceClient.LastErrorMessage))
                    fServicePage.AppendMemo("Conflicts error: " + fServiceClient.LastErrorMessage);
                fConflictsPage.SetConflicts(OpenConflicts);
                fLogsPage.SetLogs(RecentLogs);
                fServiceStatusText.Text = "Service: " + Status.ServiceStatus;
                fSyncStatusText.Text = "Sync: " + Status.SynchronizationStatus;
                fConnectionStatusText.Text = "HTTP API: " + Status.IpcStatus;

                if (LogDiagnostics)
                    fServicePage.AppendMemo("Service is " + Status.ServiceStatus + ". PID " + Status.ProcessId.ToString() + ".");
            }

            fUpdatedTimeText.Text = "Updated " + DateTime.Now.ToString("HH:mm:ss");
        }
        finally
        {
            fRefreshInProgress = false;
        }
    }
    void NavigationList_SelectionChanged(object Sender, SelectionChangedEventArgs Args)
    {
        if (fNavigationList.SelectedItem is ListBoxItem Item && Item.Tag is PageDescriptor Page)
            Navigate(Page);
    }
    async void MainWindow_Opened(object Sender, EventArgs Args)
    {
        await RefreshServiceStatusAsync(true);
        fRefreshTimer.Start();
    }
    async void ServicePage_RefreshRequested(object Sender, EventArgs Args)
    {
        await RefreshServiceStatusAsync(true);
    }
    async void RefreshTimer_Tick(object Sender, EventArgs Args)
    {
        await RefreshServiceStatusAsync(false);
    }
    async void ServicePage_StartRequested(object Sender, EventArgs Args)
    {
        fServicePage.AppendMemo("Start requested.");
        fServicePage.SetCommandResult(fServiceProcessController.Start());
        await Task.Delay(750);
        await RefreshServiceStatusAsync(true);
    }
    async void ServicePage_StopRequested(object Sender, EventArgs Args)
    {
        fServicePage.AppendMemo("Stop requested.");
        fServicePage.SetCommandResult(await fServiceClient.StopAsync());
        await Task.Delay(750);
        await RefreshServiceStatusAsync(true);
    }
    async void ServicePage_RestartRequested(object Sender, EventArgs Args)
    {
        fServicePage.AppendMemo("Restart requested.");
        fServicePage.SetCommandResult(await fServiceClient.StopAsync());
        await Task.Delay(1200);
        fServicePage.SetCommandResult(fServiceProcessController.Start());
        await Task.Delay(750);
        await RefreshServiceStatusAsync(true);
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        fNavigationList = new ListBox();
        fPageTitleText = new TextBlock() { FontSize = 26, FontWeight = FontWeight.SemiBold };
        fPageSubtitleText = new TextBlock() { Opacity = 0.72 };
        fPageHost = new ContentControl();
        fServiceStatusText = CreateStatusText("Service: Unknown");
        fSyncStatusText = CreateStatusText("Sync: Idle");
        fConnectionStatusText = CreateStatusText("Google Drive: Unknown");
        fUpdatedTimeText = CreateStatusText("Updated -");
        fServiceClient = new LocalServiceClient();
        fServiceProcessController = new LocalServiceProcessController();
        fRefreshTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(10),
        };
        fRefreshTimer.Tick += RefreshTimer_Tick;
        fDashboardPage = new DashboardPage();
        fSynchronizationPage = new SynchronizationPage();
        fConnectionsPage = new ConnectionsPage();
        fServicePage = new ServicePage();
        fFoldersPage = new FoldersPage();
        fActivityPage = new ActivityPage();
        fConflictsPage = new ConflictsPage();
        fHistoryPage = new HistoryPage();
        fLogsPage = new LogsPage();
        fSettingsPage = new SettingsPage();
        fServicePage.RefreshRequested += ServicePage_RefreshRequested;
        fServicePage.StartRequested += ServicePage_StartRequested;
        fServicePage.StopRequested += ServicePage_StopRequested;
        fServicePage.RestartRequested += ServicePage_RestartRequested;
        fPages = new List<PageDescriptor>()
        {
            new("Dashboard", "Dashboard", "Overall service and synchronization status.", "dashboard.png", fDashboardPage),
            new("Synchronization", "Synchronization", "Monitor the current synchronization state.", "sync.png", fSynchronizationPage),
            new("Connections", "Connections", "Inspect configured storage provider connectivity.", "connections.png", fConnectionsPage),
            new("Service", "Service", "Manage the Hermes background service.", "service.png", fServicePage),
            new("Folders", "Folders", "Manage synchronization roots.", "folders.png", fFoldersPage),
            new("Activity", "Activity", "Watch recent service activity.", "activity.png", fActivityPage),
            new("Conflicts", "Conflicts", "Review items that need attention.", "conflicts.png", fConflictsPage),
            new("History", "History", "Review completed synchronization runs.", "history.png", fHistoryPage),
            new("Logs", "Logs", "Inspect service and desktop log output.", "logs.png", fLogsPage),
            new("Settings", "Settings", "Configure Hermes desktop and synchronization behavior.", "settings.png", fSettingsPage),
            new("About", "About", "Application version, runtime, and license information.", "about.png", new AboutPage()),
        };

        Content = CreateLayout();
        Opened += MainWindow_Opened;
        fNavigationList.SelectedIndex = 0;
    }
}
