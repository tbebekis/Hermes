// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays recent service activity.
/// </summary>
public class ActivityPage : UserControl
{
    // ● fields

    readonly StackPanel fListPanel;
    readonly Button fClearButton;
    readonly Button fRunSyncButton;
    readonly TextBlock fCommandResultText;

    // ● private

    static Border CreateActivityRow(LocalSyncActivity Activity)
    {
        return new Border()
        {
            Padding = new Thickness(12),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gainsboro,
            CornerRadius = new CornerRadius(6),
            Child = new Grid()
            {
                ColumnDefinitions = new ColumnDefinitions("170,*"),
                ColumnSpacing = 16,
                Children =
                {
                    new StackPanel()
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock() { Text = Activity.TimestampUtc.ToLocalTime().ToString("HH:mm:ss"), FontWeight = FontWeight.SemiBold },
                            new TextBlock() { Text = Activity.Level, Opacity = 0.72 },
                            new TextBlock() { Text = Activity.SyncRootId, Opacity = 0.72 },
                        }
                    },
                    Field(Activity.Title + " - " + Activity.Details, 0, 1),
                }
            }
        };
    }
    static TextBlock Field(string Text, int Row, int Column)
    {
        TextBlock Result = new()
        {
            Text = Text,
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetRow(Result, Row);
        Grid.SetColumn(Result, Column);
        return Result;
    }
    void SetMessage(string Message)
    {
        fListPanel.Children.Clear();
        fListPanel.Children.Add(new TextBlock()
        {
            Text = Message,
            Opacity = 0.72,
        });
    }
    void ClearButton_Click(object Sender, RoutedEventArgs Args)
    {
        ClearRequested?.Invoke(this, EventArgs.Empty);
    }
    void RunSyncButton_Click(object Sender, RoutedEventArgs Args)
    {
        RunSyncRequested?.Invoke(this, EventArgs.Empty);
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityPage"/> class.
    /// </summary>
    public ActivityPage()
    {
        fListPanel = new StackPanel() { Spacing = 8 };
        fRunSyncButton = new Button()
        {
            Content = "Run Sync Cycle",
            Padding = new Thickness(14, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        fRunSyncButton.Click += RunSyncButton_Click;
        fClearButton = new Button()
        {
            Content = "Clear",
            Padding = new Thickness(14, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        fClearButton.Click += ClearButton_Click;
        fCommandResultText = new TextBlock()
        {
            Text = "-",
            Opacity = 0.72,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };

        Content = new Grid()
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            RowSpacing = 12,
            Children =
            {
                new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        fRunSyncButton,
                        fClearButton,
                        fCommandResultText,
                    }
                },
                new ScrollViewer()
                {
                    Content = fListPanel,
                }
            }
        };
        Grid.SetRow(((Grid)Content).Children[1], 1);
        SetMessage("No activity loaded.");
    }

    // ● public

    /// <summary>
    /// Displays recent activity returned by the local service.
    /// </summary>
    public void SetActivities(IReadOnlyList<LocalSyncActivity> Activities)
    {
        if (Activities == null)
        {
            SetMessage("The local service HTTP API is not reachable.");
            return;
        }

        if (Activities.Count == 0)
        {
            SetMessage("No recent activity.");
            return;
        }

        fListPanel.Children.Clear();

        foreach (LocalSyncActivity Activity in Activities)
            fListPanel.Children.Add(CreateActivityRow(Activity));
    }
    /// <summary>
    /// Displays the latest command result.
    /// </summary>
    public void SetCommandResult(LocalServiceControlResult Result)
    {
        if (Result == null)
        {
            fCommandResultText.Text = string.Empty;
            fCommandResultText.Foreground = Brushes.Black;
            return;
        }

        fCommandResultText.Text = Result.Message;
        fCommandResultText.Foreground = Result.Succeeded ? Brushes.DarkGreen : Brushes.Firebrick;
    }

    // ● events

    /// <summary>
    /// Occurs when the user requests clearing recent activity.
    /// </summary>
    public event EventHandler ClearRequested;
    /// <summary>
    /// Occurs when the user requests a manual synchronization cycle.
    /// </summary>
    public event EventHandler RunSyncRequested;
}
