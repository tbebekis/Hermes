// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays Hermes logs.
/// </summary>
public class LogsPage : UserControl
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="LogsPage"/> class.
    /// </summary>
    public LogsPage()
    {
        Content = new TextBox()
        {
            Text = "No log entries loaded.",
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 280,
        };
    }
}
