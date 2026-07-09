// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace TestApp;

/// <summary>
/// Startup window used while the application initializes.
/// </summary>
public class StartupWindow : Window
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupWindow"/> class.
    /// </summary>
    public StartupWindow()
    {
        Title = "Hermes TestApp";
        Width = 980;
        Height = 640;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = new TextBlock
        {
            Text = "Hermes test app",
            FontSize = 24,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
    }
}
