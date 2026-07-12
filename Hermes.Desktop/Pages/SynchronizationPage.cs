// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays current synchronization status.
/// </summary>
public class SynchronizationPage : UserControl
{
    // ● fields

    readonly TextBlock fStatusText;
    readonly TextBlock fSyncRootText;
    readonly TextBlock fLocalRootText;
    readonly TextBlock fPendingText;
    readonly TextBlock fConflictsLabelText;
    readonly TextBlock fConflictsText;
    readonly TextBlock fPollingText;
    readonly TextBlock fUpdatedText;
    readonly Button fRunSyncButton;
    readonly TextBlock fCommandResultText;

    // ● private

    static TextBlock Label(string Text, int Row, int Column)
    {
        TextBlock Result = new() { Text = Text, Opacity = 0.72, TextWrapping = TextWrapping.Wrap };
        Grid.SetRow(Result, Row);
        Grid.SetColumn(Result, Column);
        return Result;
    }
    static TextBlock Field(TextBlock TextBlock, int Row, int Column)
    {
        TextBlock.FontWeight = FontWeight.SemiBold;
        Grid.SetRow(TextBlock, Row);
        Grid.SetColumn(TextBlock, Column);
        return TextBlock;
    }
    Border CreateStatusPanel()
    {
        return new Border()
        {
            Padding = new Thickness(16),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gainsboro,
            CornerRadius = new CornerRadius(6),
            Child = new Grid()
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto"),
                ColumnDefinitions = new ColumnDefinitions("180,*"),
                RowSpacing = 10,
                Children =
                {
                    Label("Status", 0, 0),
                    Field(fStatusText, 0, 1),
                    Label("Sync root", 1, 0),
                    Field(fSyncRootText, 1, 1),
                    Label("Local folder", 2, 0),
                    Field(fLocalRootText, 2, 1),
                    Label("Pending work", 3, 0),
                    Field(fPendingText, 3, 1),
                    Field(fConflictsLabelText, 4, 0),
                    Field(fConflictsText, 4, 1),
                    Label("Polling", 5, 0),
                    Field(fPollingText, 5, 1),
                    Label("Last update", 6, 0),
                    Field(fUpdatedText, 6, 1),
                }
            }
        };
    }
    void SetConflictVisualState(int OpenConflictCount)
    {
        fConflictsText.Foreground = OpenConflictCount == 0 ? Brushes.Black : Brushes.Firebrick;
        fConflictsText.FontWeight = OpenConflictCount == 0 ? FontWeight.SemiBold : FontWeight.Bold;
        fConflictsText.FontSize = OpenConflictCount == 0 ? 14 : 15;
        fConflictsLabelText.Foreground = OpenConflictCount == 0 ? Brushes.Black : Brushes.Firebrick;
        fConflictsLabelText.FontWeight = FontWeight.Bold;
        fConflictsLabelText.FontSize = 14;
        fConflictsLabelText.Opacity = OpenConflictCount == 0 ? 0.82 : 1;
    }
    void RunSyncButton_Click(object Sender, RoutedEventArgs Args)
    {
        RunSyncRequested?.Invoke(this, EventArgs.Empty);
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizationPage"/> class.
    /// </summary>
    public SynchronizationPage()
    {
        fStatusText = new TextBlock() { Text = "Unknown" };
        fSyncRootText = new TextBlock() { Text = "-" };
        fLocalRootText = new TextBlock() { Text = "-" };
        fPendingText = new TextBlock() { Text = "-" };
        fConflictsLabelText = new TextBlock() { Text = "Open conflicts", Opacity = 0.72, TextWrapping = TextWrapping.Wrap };
        fConflictsText = new TextBlock() { Text = "-" };
        fPollingText = new TextBlock() { Text = "-" };
        fUpdatedText = new TextBlock() { Text = "-" };
        fRunSyncButton = new Button()
        {
            Content = "Run Sync Cycle",
            Padding = new Thickness(14, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        fRunSyncButton.Click += RunSyncButton_Click;
        fCommandResultText = new TextBlock()
        {
            Text = "-",
            Opacity = 0.72,
            TextWrapping = TextWrapping.Wrap,
        };

        Content = new StackPanel()
        {
            Spacing = 18,
            Children =
            {
                CreateStatusPanel(),
                new StackPanel()
                {
                    Spacing = 8,
                    Children =
                    {
                        fRunSyncButton,
                        fCommandResultText,
                    }
                },
            }
        };
    }

    // ● public

    /// <summary>
    /// Displays the latest synchronization status.
    /// </summary>
    public void SetStatus(LocalServiceStatus Status)
    {
        if (Status == null)
        {
            fStatusText.Text = "Unknown";
            fSyncRootText.Text = "-";
            fLocalRootText.Text = "-";
            fPendingText.Text = "-";
            fConflictsText.Text = "-";
            SetConflictVisualState(0);
            fPollingText.Text = "-";
            fUpdatedText.Text = "Disconnected";
            return;
        }

        fStatusText.Text = Status.SynchronizationStatus;
        fSyncRootText.Text = Status.SyncRootId;
        fLocalRootText.Text = Status.LocalRootPath;
        fPendingText.Text = "0";
        fConflictsText.Text = Status.OpenConflictCount.ToString();
        SetConflictVisualState(Status.OpenConflictCount);
        fPollingText.Text = Status.PollingIntervalSeconds + " seconds";
        fUpdatedText.Text = Status.TimestampUtc.ToLocalTime().ToString("HH:mm:ss");
    }
    /// <summary>
    /// Displays the latest command result.
    /// </summary>
    public void SetCommandResult(LocalServiceControlResult Result)
    {
        if (Result == null)
        {
            fCommandResultText.Text = "-";
            fCommandResultText.Foreground = Brushes.Black;
            return;
        }

        fCommandResultText.Text = Result.Message;
        fCommandResultText.Foreground = Result.Succeeded ? Brushes.DarkGreen : Brushes.Firebrick;
    }

    // ● events

    /// <summary>
    /// Occurs when the user requests a manual synchronization cycle.
    /// </summary>
    public event EventHandler RunSyncRequested;
}
