// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays Hermes desktop and synchronization settings.
/// </summary>
public class SettingsPage : UserControl
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPage"/> class.
    /// </summary>
    public SettingsPage()
    {
        Content = new Border()
        {
            Padding = new Thickness(16),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gainsboro,
            CornerRadius = new CornerRadius(6),
            Child = new TextBlock()
            {
                Text = "Settings will be loaded from the service configuration.",
                Opacity = 0.72,
            }
        };
    }
}
