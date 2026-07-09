// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace TestApp;

/// <summary>
/// Test application entry point.
/// </summary>
public class Program
{
    // ● public

    /// <summary>
    /// Runs the test application.
    /// </summary>
    [STAThread]
    static public void Main(string[] Args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(Args);
    }

    /// <summary>
    /// Builds the Avalonia application.
    /// </summary>
    static public AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
