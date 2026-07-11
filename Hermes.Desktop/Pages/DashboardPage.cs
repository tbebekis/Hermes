// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays the main Hermes desktop dashboard.
/// </summary>
public class DashboardPage : UserControl
{
    // ● private

    static Border CreateStatusTile(string Caption, string Value)
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
                    new TextBlock() { Text = Value, FontSize = 22, FontWeight = FontWeight.SemiBold },
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
        Grid Tiles = new()
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*"),
            RowDefinitions = new RowDefinitions("Auto"),
            ColumnSpacing = 12,
        };

        Tiles.Children.Add(CreateStatusTile("Service", "Unknown"));
        Tiles.Children.Add(CreateStatusTile("Synchronization", "Idle"));
        Tiles.Children.Add(CreateStatusTile("Open conflicts", "0"));
        Tiles.Children.Add(CreateStatusTile("Last update", "Never"));

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
                            new TextBlock() { Text = "No active synchronization run.", Opacity = 0.72 },
                        }
                    }
                }
            }
        };
    }
}
