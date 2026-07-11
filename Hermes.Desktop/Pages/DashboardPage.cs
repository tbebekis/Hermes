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
    readonly TextBlock fConflictText;
    readonly TextBlock fLastUpdateText;
    readonly TextBlock fCurrentActivityText;

    // ● private

    static Border CreateStatusTile(string Caption, TextBlock ValueText)
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
                    new TextBlock() { Text = Caption, Opacity = 0.72 },
                    ValueText,
                }
            }
        };
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardPage"/> class.
    /// </summary>
    public DashboardPage()
    {
        fServiceText = new TextBlock() { Text = "Unknown", FontSize = 22, FontWeight = FontWeight.SemiBold };
        fSyncText = new TextBlock() { Text = "Idle", FontSize = 22, FontWeight = FontWeight.SemiBold };
        fConflictText = new TextBlock() { Text = "0", FontSize = 22, FontWeight = FontWeight.SemiBold };
        fLastUpdateText = new TextBlock() { Text = "Never", FontSize = 22, FontWeight = FontWeight.SemiBold };
        fCurrentActivityText = new TextBlock() { Text = "No active synchronization run.", Opacity = 0.72 };

        Grid Tiles = new()
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*"),
            RowDefinitions = new RowDefinitions("Auto"),
            ColumnSpacing = 12,
        };

        Tiles.Children.Add(CreateStatusTile("Service", fServiceText));
        Tiles.Children.Add(CreateStatusTile("Synchronization", fSyncText));
        Tiles.Children.Add(CreateStatusTile("Open conflicts", fConflictText));
        Tiles.Children.Add(CreateStatusTile("Last update", fLastUpdateText));

        Grid.SetColumn(Tiles.Children[1], 1);
        Grid.SetColumn(Tiles.Children[2], 2);
        Grid.SetColumn(Tiles.Children[3], 3);

        Content = new StackPanel()
        {
            Spacing = 18,
            Children =
            {
                Tiles,
                new Border()
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
                            new TextBlock() { Text = "Current activity", FontSize = 18, FontWeight = FontWeight.SemiBold },
                            fCurrentActivityText,
                        }
                    }
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
            fLastUpdateText.Text = "Disconnected";
            fCurrentActivityText.Text = "The local service HTTP API is not reachable.";
            return;
        }

        fServiceText.Text = Status.ServiceStatus;
        fSyncText.Text = Status.SynchronizationStatus;
        fConflictText.Text = Status.OpenConflictCount.ToString();
        fLastUpdateText.Text = Status.TimestampUtc.ToLocalTime().ToString("HH:mm:ss");
        fCurrentActivityText.Text = Status.SyncRootId + " - " + Status.LocalRootPath;
    }
}
