// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays open synchronization conflicts.
/// </summary>
public class ConflictsPage : UserControl
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictsPage"/> class.
    /// </summary>
    public ConflictsPage()
    {
        Content = new Border()
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
                    new TextBlock() { Text = "Open conflicts", FontSize = 18, FontWeight = FontWeight.SemiBold },
                    new TextBlock() { Text = "Conflict details will be read from the service and shown here before resolution commands are added.", Opacity = 0.72 },
                }
            }
        };
    }
}
