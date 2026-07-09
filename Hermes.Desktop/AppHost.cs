// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Provides application-wide startup state and helpers.
/// </summary>
static public partial class AppHost
{
    // ● constructor

    /// <summary>
    /// Initializes static application state.
    /// </summary>
    static AppHost()
    {
#if DEBUG
        Sys.DebugMode = true;
#endif
        HiddenMainWindow = new HiddenMainWindow();
    }

    // ● properties

    /// <summary>
    /// Gets the hidden startup window.
    /// </summary>
    static public HiddenMainWindow HiddenMainWindow { get; private set; }

    /// <summary>
    /// Gets the real main window.
    /// </summary>
    static public MainWindow MainWindow { get; private set; }

    /// <summary>
    /// Gets the Avalonia desktop lifetime.
    /// </summary>
    static public IClassicDesktopStyleApplicationLifetime AvaloniaDesktop { get; private set; }

    /// <summary>
    /// Gets the default SQL store.
    /// </summary>
    static public SqlStore Store { get; private set; }
}
