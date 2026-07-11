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
    readonly TextBlock fConflictsText;
    readonly TextBlock fPollingText;
    readonly TextBlock fUpdatedText;

    // ● private

    static TextBlock Label(string Text, int Row, int Column)
    {
        TextBlock Result = new() { Text = Text, Opacity = 0.72 };
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
                    Label("Open conflicts", 4, 0),
                    Field(fConflictsText, 4, 1),
                    Label("Polling", 5, 0),
                    Field(fPollingText, 5, 1),
                    Label("Last update", 6, 0),
                    Field(fUpdatedText, 6, 1),
                }
            }
        };
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
        fConflictsText = new TextBlock() { Text = "-" };
        fPollingText = new TextBlock() { Text = "-" };
        fUpdatedText = new TextBlock() { Text = "-" };

        Content = new StackPanel()
        {
            Spacing = 18,
            Children =
            {
                CreateStatusPanel(),
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
            fPollingText.Text = "-";
            fUpdatedText.Text = "Disconnected";
            return;
        }

        fStatusText.Text = Status.SynchronizationStatus;
        fSyncRootText.Text = Status.SyncRootId;
        fLocalRootText.Text = Status.LocalRootPath;
        fPendingText.Text = "0";
        fConflictsText.Text = Status.OpenConflictCount.ToString();
        fPollingText.Text = Status.PollingIntervalSeconds + " seconds";
        fUpdatedText.Text = Status.TimestampUtc.ToLocalTime().ToString("HH:mm:ss");
    }
}
