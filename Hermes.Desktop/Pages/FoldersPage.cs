// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays synchronization folder roots.
/// </summary>
public class FoldersPage : UserControl
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="FoldersPage"/> class.
    /// </summary>
    public FoldersPage()
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
                    new TextBlock() { Text = "Synchronization roots", FontSize = 18, FontWeight = FontWeight.SemiBold },
                    new TextBlock() { Text = "Configured folders will appear here when the service API is connected.", Opacity = 0.72 },
                }
            }
        };
    }
}
