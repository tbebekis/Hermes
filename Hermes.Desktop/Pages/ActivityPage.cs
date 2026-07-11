// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays recent service activity.
/// </summary>
public class ActivityPage : UserControl
{
    // ● fields

    readonly StackPanel fListPanel;

    // ● private

    static Border CreateActivityRow(LocalSyncActivity Activity)
    {
        return new Border()
        {
            Padding = new Thickness(12),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gainsboro,
            CornerRadius = new CornerRadius(6),
            Child = new Grid()
            {
                ColumnDefinitions = new ColumnDefinitions("170,*"),
                ColumnSpacing = 16,
                Children =
                {
                    new StackPanel()
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock() { Text = Activity.TimestampUtc.ToLocalTime().ToString("HH:mm:ss"), FontWeight = FontWeight.SemiBold },
                            new TextBlock() { Text = Activity.Level, Opacity = 0.72 },
                            new TextBlock() { Text = Activity.SyncRootId, Opacity = 0.72 },
                        }
                    },
                    Field(Activity.Title + " - " + Activity.Details, 0, 1),
                }
            }
        };
    }
    static TextBlock Field(string Text, int Row, int Column)
    {
        TextBlock Result = new()
        {
            Text = Text,
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetRow(Result, Row);
        Grid.SetColumn(Result, Column);
        return Result;
    }
    void SetMessage(string Message)
    {
        fListPanel.Children.Clear();
        fListPanel.Children.Add(new TextBlock()
        {
            Text = Message,
            Opacity = 0.72,
        });
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityPage"/> class.
    /// </summary>
    public ActivityPage()
    {
        fListPanel = new StackPanel() { Spacing = 8 };

        Content = fListPanel;
        SetMessage("No activity loaded.");
    }

    // ● public

    /// <summary>
    /// Displays recent activity returned by the local service.
    /// </summary>
    public void SetActivities(IReadOnlyList<LocalSyncActivity> Activities)
    {
        if (Activities == null)
        {
            SetMessage("The local service HTTP API is not reachable.");
            return;
        }

        if (Activities.Count == 0)
        {
            SetMessage("No recent activity.");
            return;
        }

        fListPanel.Children.Clear();

        foreach (LocalSyncActivity Activity in Activities)
            fListPanel.Children.Add(CreateActivityRow(Activity));
    }
}
