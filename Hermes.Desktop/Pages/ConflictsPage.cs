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

    // ● private

    static Border CreateConflictItem(LocalOpenConflict Conflict)
    {
        string LocalPath = string.IsNullOrWhiteSpace(Conflict.LocalPath) ? "-" : Conflict.LocalPath;
        string RemoteName = string.IsNullOrWhiteSpace(Conflict.RemoteName) ? "-" : Conflict.RemoteName;

        return new Border()
        {
            Padding = new Thickness(12),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gainsboro,
            CornerRadius = new CornerRadius(6),
            Child = new StackPanel()
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock() { Text = Conflict.DiffKind + " / " + Conflict.DecisionKind, FontWeight = FontWeight.SemiBold },
                    new TextBlock() { Text = Conflict.Message, Opacity = 0.82 },
                    new TextBlock() { Text = "Local: " + LocalPath, Opacity = 0.72 },
                    new TextBlock() { Text = "Remote: " + RemoteName, Opacity = 0.72 },
                    new TextBlock() { Text = "Tracked item: " + Conflict.TrackedItemId, Opacity = 0.58, FontSize = 12 },
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
        fSummaryText = new TextBlock() { Text = "Open conflicts have not been loaded.", Opacity = 0.72 };
        fConflictList = new StackPanel() { Spacing = 8 };

        Content = new Border()
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
            return;
        }

        if (Conflicts.Count == 0)
        {
            fSummaryText.Text = "No open conflicts.";
            return;
        }

        fSummaryText.Text = Conflicts.Count + " open conflict(s).";

        foreach (LocalOpenConflict Conflict in Conflicts)
            fConflictList.Children.Add(CreateConflictItem(Conflict));
    }
}
