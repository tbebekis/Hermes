// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays application information.
/// </summary>
public class AboutPage : UserControl
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutPage"/> class.
    /// </summary>
    public AboutPage()
    {
        Content = new StackPanel()
        {
            Spacing = 8,
            Children =
            {
                new TextBlock() { Text = "Hermes", FontSize = 26, FontWeight = FontWeight.SemiBold },
                new TextBlock() { Text = "Linux Google Drive synchronization service control center.", Opacity = 0.72 },
                new TextBlock() { Text = ".NET " + Environment.Version, Opacity = 0.72 },
                new TextBlock() { Text = "MIT License", Opacity = 0.72 },
            }
        };
    }
}
