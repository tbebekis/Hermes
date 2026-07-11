// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays Hermes service status and service control actions.
/// </summary>
public class ServicePage : UserControl
{
    // ● fields

    readonly TextBlock fStatusText;
    readonly TextBlock fProcessIdText;
    readonly TextBlock fUptimeText;
    readonly TextBlock fIpcText;
    readonly TextBlock fVersionText;

    // ● private

    Border CreateInfoPanel()
    {
        return new Border()
        {
            Padding = new Thickness(16),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gainsboro,
            CornerRadius = new CornerRadius(6),
            Child = new Grid()
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"),
                ColumnDefinitions = new ColumnDefinitions("180,*"),
                RowSpacing = 10,
                Children =
                {
                    Label("Status", 0, 0),
                    Field(fStatusText, 0, 1),
                    Label("Process id", 1, 0),
                    Field(fProcessIdText, 1, 1),
                    Label("Uptime", 2, 0),
                    Field(fUptimeText, 2, 1),
                    Label("IPC", 3, 0),
                    Field(fIpcText, 3, 1),
                    Label("Version", 4, 0),
                    Field(fVersionText, 4, 1),
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
    static TextBlock Field(TextBlock TextBlock, int Row, int Column)
    {
        TextBlock.FontWeight = FontWeight.SemiBold;
        Grid.SetRow(TextBlock, Row);
        Grid.SetColumn(TextBlock, Column);
        return TextBlock;
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
        fStatusText = new TextBlock() { Text = "Unknown" };
        fProcessIdText = new TextBlock() { Text = "-" };
        fUptimeText = new TextBlock() { Text = "-" };
        fIpcText = new TextBlock() { Text = "localhost HTTP API not connected" };
        fVersionText = new TextBlock() { Text = "-" };

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

    // ● public

    /// <summary>
    /// Displays the latest local service status.
    /// </summary>
    public void SetStatus(LocalServiceStatus Status)
    {
        if (Status == null)
        {
            fStatusText.Text = "Stopped";
            fProcessIdText.Text = "-";
            fUptimeText.Text = "-";
            fIpcText.Text = "localhost HTTP API not connected";
            fVersionText.Text = "-";
            return;
        }

        fStatusText.Text = Status.ServiceStatus;
        fProcessIdText.Text = Status.ProcessId.ToString();
        fUptimeText.Text = "-";
        fIpcText.Text = Status.IpcStatus;
        fVersionText.Text = Status.Version;
    }
}
