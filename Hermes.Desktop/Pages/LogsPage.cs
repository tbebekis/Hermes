// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays Hermes logs.
/// </summary>
public class LogsPage : UserControl
{
    // ● fields

    readonly ComboBox fLevelBox;
    readonly TextBox fSearchBox;
    readonly TextBox fTextBox;
    IReadOnlyList<LocalRecentLog> fLogs;

    // ● private

    static string FormatLog(LocalRecentLog Log)
    {
        return Log.LogTime + " [" + Log.Level + "] " + Log.Source + " " + Log.EventId + " - " + Log.Message;
    }
    static bool ContainsText(string Source, string Text)
    {
        return Source != null && Source.IndexOf(Text, StringComparison.OrdinalIgnoreCase) >= 0;
    }
    string SelectedLevel()
    {
        if (fLevelBox.SelectedItem is ComboBoxItem Item && Item.Content != null)
            return Item.Content.ToString();

        return "All";
    }
    bool ShouldDisplay(LocalRecentLog Log)
    {
        string Level = SelectedLevel();
        string SearchText = fSearchBox.Text ?? string.Empty;

        if (Level != "All" && !string.Equals(Log.Level, Level, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        return ContainsText(Log.LogTime, SearchText)
            || ContainsText(Log.Level, SearchText)
            || ContainsText(Log.Source, SearchText)
            || ContainsText(Log.EventId, SearchText)
            || ContainsText(Log.Message, SearchText);
    }
    void RefreshText()
    {
        if (fLogs == null)
        {
            fTextBox.Text = "The local service HTTP API is not reachable.";
            return;
        }

        if (fLogs.Count == 0)
        {
            fTextBox.Text = "No log entries.";
            return;
        }

        List<string> Lines = new();

        foreach (LocalRecentLog Log in fLogs)
        {
            if (ShouldDisplay(Log))
                Lines.Add(FormatLog(Log));
        }

        fTextBox.Text = Lines.Count == 0 ? "No log entries match the current filters." : string.Join(Environment.NewLine, Lines);
    }
    void Filter_Changed(object Sender, EventArgs Args)
    {
        RefreshText();
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="LogsPage"/> class.
    /// </summary>
    public LogsPage()
    {
        fLogs = new List<LocalRecentLog>();
        fLevelBox = new ComboBox()
        {
            MinWidth = 140,
            SelectedIndex = 0,
            Items =
            {
                new ComboBoxItem() { Content = "All" },
                new ComboBoxItem() { Content = "Debug" },
                new ComboBoxItem() { Content = "Information" },
                new ComboBoxItem() { Content = "Warning" },
                new ComboBoxItem() { Content = "Error" },
            }
        };
        fSearchBox = new TextBox()
        {
            PlaceholderText = "Search logs",
            MinWidth = 240,
        };
        fTextBox = new TextBox()
        {
            Text = "No log entries loaded.",
            AcceptsReturn = true,
            IsReadOnly = true,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Monospace"),
            FontSize = 12,
            TextWrapping = TextWrapping.NoWrap,
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(fTextBox, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(fTextBox, ScrollBarVisibility.Auto);
        fLevelBox.SelectionChanged += Filter_Changed;
        fSearchBox.TextChanged += Filter_Changed;

        Grid Layout = new()
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            RowSpacing = 12,
            Children =
            {
                new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        fLevelBox,
                        fSearchBox,
                    }
                },
                fTextBox,
            }
        };
        Grid.SetRow(Layout.Children[1], 1);
        Content = Layout;
    }

    // ● public

    /// <summary>
    /// Displays recent logs returned by the local service.
    /// </summary>
    public void SetLogs(IReadOnlyList<LocalRecentLog> Logs)
    {
        fLogs = Logs;
        RefreshText();
    }
}
