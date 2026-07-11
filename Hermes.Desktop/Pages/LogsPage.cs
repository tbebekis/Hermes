// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays Hermes logs.
/// </summary>
public class LogsPage : UserControl
{
    // ● fields

    readonly TextBox fTextBox;

    // ● private

    static string FormatLog(LocalRecentLog Log)
    {
        return Log.LogTime + " [" + Log.Level + "] " + Log.Source + " " + Log.EventId + " - " + Log.Message;
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="LogsPage"/> class.
    /// </summary>
    public LogsPage()
    {
        fTextBox = new TextBox()
        {
            Text = "No log entries loaded.",
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 280,
        };
        Content = fTextBox;
    }

    // ● public

    /// <summary>
    /// Displays recent logs returned by the local service.
    /// </summary>
    public void SetLogs(IReadOnlyList<LocalRecentLog> Logs)
    {
        if (Logs == null)
        {
            fTextBox.Text = "The local service HTTP API is not reachable.";
            return;
        }

        if (Logs.Count == 0)
        {
            fTextBox.Text = "No log entries.";
            return;
        }

        List<string> Lines = new();

        foreach (LocalRecentLog Log in Logs)
            Lines.Add(FormatLog(Log));

        fTextBox.Text = string.Join(Environment.NewLine, Lines);
    }
}
