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
        public PageDescriptor(string Key, string Title, string Subtitle, UserControl Page)
        {
            this.Key = Key;
            this.Title = Title;
            this.Subtitle = Subtitle;
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
    readonly ServicePage fServicePage;

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
    static ListBoxItem CreateNavigationItem(PageDescriptor Page)
    {
        return new ListBoxItem()
        {
            Content = Page.Title,
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
        Grid Result = new()
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Children =
            {
                CreateHeader(),
                new ScrollViewer()
                {
                    Content = fPageHost,
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
    async Task RefreshServiceStatusAsync()
    {
        LocalServiceStatus Status = await fServiceClient.GetStatusAsync();
        fServicePage.SetStatus(Status);

        if (Status == null)
        {
            fServiceStatusText.Text = "Service: Stopped";
            fSyncStatusText.Text = "Sync: Unknown";
            fConnectionStatusText.Text = "HTTP API: Disconnected";
        }
        else
        {
            fServiceStatusText.Text = "Service: " + Status.ServiceStatus;
            fSyncStatusText.Text = "Sync: " + Status.SynchronizationStatus;
            fConnectionStatusText.Text = "HTTP API: " + Status.IpcStatus;
        }

        fUpdatedTimeText.Text = "Updated " + DateTime.Now.ToString("HH:mm:ss");
    }
    void NavigationList_SelectionChanged(object Sender, SelectionChangedEventArgs Args)
    {
        if (fNavigationList.SelectedItem is ListBoxItem Item && Item.Tag is PageDescriptor Page)
            Navigate(Page);
    }
    async void MainWindow_Opened(object Sender, EventArgs Args)
    {
        await RefreshServiceStatusAsync();
    }
    async void ServicePage_RefreshRequested(object Sender, EventArgs Args)
    {
        await RefreshServiceStatusAsync();
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
        fServicePage = new ServicePage();
        fServicePage.RefreshRequested += ServicePage_RefreshRequested;
        fPages = new List<PageDescriptor>()
        {
            new("Dashboard", "Dashboard", "Overall service and synchronization status.", new DashboardPage()),
            new("Service", "Service", "Manage the Hermes background service.", fServicePage),
            new("Folders", "Folders", "Manage synchronization roots.", new FoldersPage()),
            new("Conflicts", "Conflicts", "Review items that need attention.", new ConflictsPage()),
            new("Logs", "Logs", "Inspect service and desktop log output.", new LogsPage()),
            new("Settings", "Settings", "Configure Hermes desktop and synchronization behavior.", new SettingsPage()),
            new("About", "About", "Application version, runtime, and license information.", new AboutPage()),
        };

        Content = CreateLayout();
        Opened += MainWindow_Opened;
        fNavigationList.SelectedIndex = 0;
    }
}
