// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays Hermes service status and service control actions.
/// </summary>
public class ServicePage : UserControl
{
    // ● private

    static Border CreateInfoPanel()
    {
        return new Border()
        {
            Padding = new Thickness(16),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gainsboro,
            CornerRadius = new CornerRadius(6),
            Child = new Grid()
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
                ColumnDefinitions = new ColumnDefinitions("180,*"),
                RowSpacing = 10,
                Children =
                {
                    Label("Status", 0, 0),
                    Value("Unknown", 0, 1),
                    Label("Process id", 1, 0),
                    Value("-", 1, 1),
                    Label("Uptime", 2, 0),
                    Value("-", 2, 1),
                    Label("IPC", 3, 0),
                    Value("localhost HTTP API not connected", 3, 1),
                }
            }
        };
    }
    static TextBlock Label(string Text, int Row, int Column)
    {
        TextBlock Result = new() { Text = Text, Opacity = 0.72 };
        Grid.SetRow(Result, Row);
        Grid.SetColumn(Result, Column);
        return Result;
    }
    static TextBlock Value(string Text, int Row, int Column)
    {
        TextBlock Result = new() { Text = Text, FontWeight = FontWeight.SemiBold };
        Grid.SetRow(Result, Row);
        Grid.SetColumn(Result, Column);
        return Result;
    }
    static Button ActionButton(string Text)
    {
        return new Button()
        {
            Content = Text,
            MinWidth = 96,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ServicePage"/> class.
    /// </summary>
    public ServicePage()
    {
        Content = new StackPanel()
        {
            Spacing = 18,
            Children =
            {
                CreateInfoPanel(),
                new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        ActionButton("Start"),
                        ActionButton("Stop"),
                        ActionButton("Restart"),
                    }
                },
                new TextBlock()
                {
                    Text = "Service commands will be connected through the local HTTP API.",
                    Opacity = 0.72,
                }
            }
        };
    }
}
