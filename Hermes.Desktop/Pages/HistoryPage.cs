// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays synchronization history.
/// </summary>
public class HistoryPage : UserControl
{
    // ● fields

    readonly TextBlock fMessageText;

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryPage"/> class.
    /// </summary>
    public HistoryPage()
    {
        fMessageText = new TextBlock()
        {
            Text = "No synchronization history.",
            Opacity = 0.72,
        };

        Content = fMessageText;
    }
}
