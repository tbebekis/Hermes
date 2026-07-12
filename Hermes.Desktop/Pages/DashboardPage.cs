// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays the main Hermes desktop dashboard.
/// </summary>
public class DashboardPage : UserControl
{
    // ● fields

    readonly TextBlock fServiceText;
    readonly TextBlock fSyncText;
    readonly TextBlock fConflictLabelText;
    readonly TextBlock fConflictText;
    readonly TextBlock fLastUpdateText;
    readonly TextBlock fProviderText;
    readonly TextBlock fSyncRootText;
    readonly TextBlock fRootText;
    readonly TextBlock fPollingText;
    readonly TextBlock fMutationsText;
    readonly TextBlock fCurrentActivityText;
    readonly Border fConflictTile;
    string fRootPath;

    // ● private

    static TextBlock CreateTileCaption(string Text)
    {
        return new TextBlock()
        {
            Text = Text,
            Opacity = 0.72,
            TextWrapping = TextWrapping.Wrap,
        };
    }
    static Border CreateStatusTile(TextBlock CaptionText, TextBlock ValueText)
    {
        return new Border()
        {
            Padding = new Thickness(16),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gainsboro,
            CornerRadius = new CornerRadius(6),
            Child = new StackPanel()
            {
                Spacing = 4,
                Children =
                {
                    CaptionText,
                    ValueText,
                }
            }
        };
    }
    static Border CreateStatusTile(string Caption, TextBlock ValueText)
    {
        return CreateStatusTile(CreateTileCaption(Caption), ValueText);
    }
    static TextBlock CreateTileValue(string Text)
    {
        return new TextBlock()
        {
            Text = Text,
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
    }
    static TextBlock CreateRootValue()
    {
        return new TextBlock()
        {
            Text = "-",
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.NoWrap,
            Foreground = Brushes.DodgerBlue,
            TextDecorations = TextDecorations.Underline,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
    }
    static Border CreateInfoPanel(string Caption, Control Content)
    {
        return new Border()
        {
            Padding = new Thickness(16),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gainsboro,
            CornerRadius = new CornerRadius(6),
            Child = new StackPanel()
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock() { Text = Caption, FontSize = 18, FontWeight = FontWeight.SemiBold },
                    Content,
                }
            }
        };
    }
    static Grid CreateTileGrid()
    {
        return new Grid()
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 12,
        };
    }
    static void AddTile(Grid Grid, Border Tile, int Row, int Column)
    {
        Grid.Children.Add(Tile);
        Grid.SetRow(Tile, Row);
        Grid.SetColumn(Tile, Column);
    }
    void SetConflictVisualState(int OpenConflictCount)
    {
        if (OpenConflictCount == 0)
        {
            fConflictText.Foreground = Brushes.Black;
            fConflictText.FontWeight = FontWeight.SemiBold;
            fConflictText.FontSize = 15;
            fConflictLabelText.Foreground = Brushes.Black;
            fConflictLabelText.FontWeight = FontWeight.Bold;
            fConflictLabelText.FontSize = 14;
            fConflictLabelText.Opacity = 0.82;
            fConflictTile.BorderBrush = Brushes.Gainsboro;
            fConflictTile.BorderThickness = new Thickness(1);
            return;
        }

        fConflictText.Foreground = Brushes.Firebrick;
        fConflictText.FontWeight = FontWeight.Bold;
        fConflictText.FontSize = 16;
        fConflictLabelText.Foreground = Brushes.Firebrick;
        fConflictLabelText.FontWeight = FontWeight.Bold;
        fConflictLabelText.FontSize = 14;
        fConflictLabelText.Opacity = 1;
        fConflictTile.BorderBrush = Brushes.Firebrick;
        fConflictTile.BorderThickness = new Thickness(2);
    }
    void OpenRootPath()
    {
        if (string.IsNullOrWhiteSpace(fRootPath) || !Directory.Exists(fRootPath))
            return;

        Process.Start(new ProcessStartInfo(fRootPath)
        {
            UseShellExecute = true,
        });
    }
    void RootText_PointerPressed(object Sender, PointerPressedEventArgs Args)
    {
        OpenRootPath();
    }
    ScrollViewer CreateRootPanel()
    {
        return new ScrollViewer()
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = fRootText,
        };
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardPage"/> class.
    /// </summary>
    public DashboardPage()
    {
        fServiceText = CreateTileValue("Unknown");
        fSyncText = CreateTileValue("Idle");
        fConflictLabelText = CreateTileCaption("Open conflicts");
        fConflictText = CreateTileValue("0");
        fLastUpdateText = CreateTileValue("Never");
        fProviderText = CreateTileValue("-");
        fSyncRootText = CreateTileValue("-");
        fRootText = CreateRootValue();
        fPollingText = CreateTileValue("-");
        fMutationsText = CreateTileValue("-");
        fRootPath = string.Empty;
        fConflictTile = CreateStatusTile(fConflictLabelText, fConflictText);
        fCurrentActivityText = new TextBlock()
        {
            Text = "No active synchronization run.",
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 120,
        };
        fRootText.PointerPressed += RootText_PointerPressed;

        Grid Tiles = CreateTileGrid();
        AddTile(Tiles, CreateStatusTile("Service", fServiceText), 0, 0);
        AddTile(Tiles, CreateStatusTile("Synchronization", fSyncText), 0, 1);
        AddTile(Tiles, fConflictTile, 0, 2);
        AddTile(Tiles, CreateStatusTile("Last update", fLastUpdateText), 0, 3);
        AddTile(Tiles, CreateStatusTile("Provider", fProviderText), 1, 0);
        AddTile(Tiles, CreateStatusTile("Sync root", fSyncRootText), 1, 1);
        AddTile(Tiles, CreateStatusTile("Polling", fPollingText), 1, 2);
        AddTile(Tiles, CreateStatusTile("Mutations", fMutationsText), 1, 3);

        Content = new ScrollViewer()
        {
            Content = new StackPanel()
            {
                Spacing = 18,
                Children =
                {
                    Tiles,
                    CreateInfoPanel("Local path", CreateRootPanel()),
                    CreateInfoPanel("Latest activity", fCurrentActivityText),
                }
            }
        };
    }

    // ● public

    /// <summary>
    /// Displays the latest local service status.
    /// </summary>
    public void SetStatus(LocalServiceStatus Status)
    {
        if (Status == null)
        {
            fServiceText.Text = "Stopped";
            fSyncText.Text = "Unknown";
            fConflictText.Text = "-";
            SetConflictVisualState(0);
            fLastUpdateText.Text = "Disconnected";
            fProviderText.Text = "-";
            fSyncRootText.Text = "-";
            fRootText.Text = "-";
            fRootPath = string.Empty;
            fPollingText.Text = "-";
            fMutationsText.Text = "-";
            fCurrentActivityText.Text = "The local service HTTP API is not reachable.";
            return;
        }

        fServiceText.Text = Status.ServiceStatus;
        fSyncText.Text = Status.SynchronizationStatus;
        fConflictText.Text = Status.OpenConflictCount.ToString();
        SetConflictVisualState(Status.OpenConflictCount);
        fLastUpdateText.Text = Status.TimestampUtc.ToLocalTime().ToString("HH:mm:ss");
        fProviderText.Text = Status.ProviderName;
        fSyncRootText.Text = Status.SyncRootId + " - " + (Status.SyncRootEnabled ? "Enabled" : "Disabled");
        fRootText.Text = Status.LocalRootPath;
        fRootPath = Status.LocalRootPath;
        fPollingText.Text = Status.PollingIntervalSeconds.ToString() + " seconds";
        fMutationsText.Text = Status.MutationsEnabled ? "Enabled" : "Disabled";
    }
    /// <summary>
    /// Displays the latest synchronization activity summary.
    /// </summary>
    public void SetActivities(IReadOnlyList<LocalSyncActivity> Activities)
    {
        if (Activities == null)
            return;

        if (Activities.Count == 0)
        {
            fCurrentActivityText.Text = "No recent synchronization activity.";
            return;
        }

        LocalSyncActivity Activity = Activities[0];
        fCurrentActivityText.Text = Activity.TimestampUtc.ToLocalTime().ToString("HH:mm:ss") + " - " + Activity.Level + Environment.NewLine + Activity.Title + Environment.NewLine + Activity.Details;
    }
}
