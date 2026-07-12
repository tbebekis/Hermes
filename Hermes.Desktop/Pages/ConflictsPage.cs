// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays open synchronization conflicts.
/// </summary>
public class ConflictsPage : UserControl
{
    // ● fields

    readonly StackPanel fConflictList;
    readonly TextBlock fSummaryText;
    readonly Border fContentBorder;

    // ● private

    static TextBlock Text(string Text, double Opacity)
    {
        return new TextBlock()
        {
            Text = Text,
            Opacity = Opacity,
            TextWrapping = TextWrapping.Wrap,
        };
    }
    static TextBlock Heading(string Text)
    {
        return new TextBlock()
        {
            Text = Text,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
    }
    static string DescribeProblem(LocalOpenConflict Conflict)
    {
        if (string.Equals(Conflict.DiffKind, "Conflict", StringComparison.OrdinalIgnoreCase))
            return "Hermes detected incompatible local and remote changes for the same tracked item. Synchronization for this item is paused so neither side is overwritten automatically.";

        return "Hermes cannot safely choose a synchronization action for this item. Synchronization for this item is paused until the conflict is resolved.";
    }
    static string DescribeResolution(LocalOpenConflict Conflict)
    {
        string LocalPath = string.IsNullOrWhiteSpace(Conflict.LocalPath) ? "the local item" : Conflict.LocalPath;
        string RemoteName = string.IsNullOrWhiteSpace(Conflict.RemoteName) ? "the remote item" : Conflict.RemoteName;

        return "Resolve it manually: inspect " + LocalPath + " locally and " + RemoteName + " in Google Drive, decide which version should remain, then rename, delete, or edit one side so both sides represent the same intended file. After the next sync pass the conflict should clear if the item no longer differs.";
    }
    static Border CreateConflictItem(LocalOpenConflict Conflict)
    {
        string LocalPath = string.IsNullOrWhiteSpace(Conflict.LocalPath) ? "-" : Conflict.LocalPath;
        string RemoteName = string.IsNullOrWhiteSpace(Conflict.RemoteName) ? "-" : Conflict.RemoteName;
        string LastObserved = Conflict.LastObservedTime == default ? "-" : Conflict.LastObservedTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

        return new Border()
        {
            Padding = new Thickness(14),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Firebrick,
            CornerRadius = new CornerRadius(6),
            Child = new StackPanel()
            {
                Spacing = 8,
                Children =
                {
                    Heading(Conflict.DiffKind + " / " + Conflict.DecisionKind),
                    Text(DescribeProblem(Conflict), 0.82),
                    Text("Reason: " + Conflict.Message, 0.82),
                    Text("Local item: " + LocalPath, 0.72),
                    Text("Remote item: " + RemoteName, 0.72),
                    Text("Last observed: " + LastObserved, 0.72),
                    Text(DescribeResolution(Conflict), 0.82),
                    new TextBlock() { Text = "Tracked item: " + Conflict.TrackedItemId, Opacity = 0.58, FontSize = 12, TextWrapping = TextWrapping.Wrap },
                }
            }
        };
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictsPage"/> class.
    /// </summary>
    public ConflictsPage()
    {
        fSummaryText = new TextBlock()
        {
            Text = "Open conflicts have not been loaded.",
            Opacity = 0.72,
            TextWrapping = TextWrapping.Wrap,
        };
        fConflictList = new StackPanel() { Spacing = 8 };

        fContentBorder = new Border()
        {
            Padding = new Thickness(16),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gainsboro,
            CornerRadius = new CornerRadius(6),
            Child = new StackPanel()
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock() { Text = "Open conflicts", FontSize = 18, FontWeight = FontWeight.SemiBold },
                    fSummaryText,
                    fConflictList,
                }
            }
        };
        Content = new ScrollViewer()
        {
            Content = fContentBorder,
        };
    }

    // ● public

    /// <summary>
    /// Displays open conflicts returned by the local service.
    /// </summary>
    public void SetConflicts(IReadOnlyList<LocalOpenConflict> Conflicts)
    {
        fConflictList.Children.Clear();

        if (Conflicts == null)
        {
            fSummaryText.Text = "The local service HTTP API is not reachable.";
            fSummaryText.Foreground = Brushes.Black;
            fContentBorder.BorderBrush = Brushes.Gainsboro;
            fContentBorder.BorderThickness = new Thickness(1);
            return;
        }

        if (Conflicts.Count == 0)
        {
            fSummaryText.Text = "No open conflicts.";
            fSummaryText.Foreground = Brushes.Black;
            fContentBorder.BorderBrush = Brushes.Gainsboro;
            fContentBorder.BorderThickness = new Thickness(1);
            return;
        }

        fSummaryText.Text = Conflicts.Count + " open conflict(s). Hermes has paused synchronization for these item(s) to avoid overwriting data. Review each item and make the local and remote sides agree.";
        fSummaryText.Foreground = Brushes.Firebrick;
        fContentBorder.BorderBrush = Brushes.Firebrick;
        fContentBorder.BorderThickness = new Thickness(2);

        foreach (LocalOpenConflict Conflict in Conflicts)
            fConflictList.Children.Add(CreateConflictItem(Conflict));
    }
}
