// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace TestApp;

/// <summary>
/// Test Avalonia application.
/// </summary>
public partial class App : Application
{
    // ● public

    /// <inheritdoc/>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime Desktop)
            Desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
