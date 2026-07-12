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
    readonly TextBlock fConflictLabelText;
    readonly TextBlock fConflictText;
    readonly TextBlock fCommandText;
    readonly TextBox fMemoTextBox;
    readonly Button fRefreshButton;
    readonly Button fStartButton;
    readonly Button fStopButton;
    readonly Button fRestartButton;
    readonly Button fClearButton;

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
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto"),
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
                    Field(fConflictLabelText, 5, 0),
                    Field(fConflictText, 5, 1),
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
    static Button ActionButton(string Text, bool IsEnabled)
    {
        return new Button()
        {
            Content = Text,
            MinWidth = 96,
            IsEnabled = IsEnabled,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
    }
    static string FormatUptime(int UptimeSeconds)
    {
        if (UptimeSeconds < 0)
            UptimeSeconds = 0;

        TimeSpan Value = TimeSpan.FromSeconds(UptimeSeconds);

        if (Value.TotalDays >= 1)
            return ((int)Value.TotalDays).ToString() + "d " + Value.Hours.ToString() + "h " + Value.Minutes.ToString() + "m";

        if (Value.TotalHours >= 1)
            return Value.Hours.ToString() + "h " + Value.Minutes.ToString() + "m " + Value.Seconds.ToString() + "s";

        if (Value.TotalMinutes >= 1)
            return Value.Minutes.ToString() + "m " + Value.Seconds.ToString() + "s";

        return Value.Seconds.ToString() + "s";
    }
    static string LogLine(string Message)
    {
        return DateTime.Now.ToString("HH:mm:ss") + "  " + Message;
    }
    void RefreshButton_Click(object Sender, RoutedEventArgs Args)
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }
    void StartButton_Click(object Sender, RoutedEventArgs Args)
    {
        StartRequested?.Invoke(this, EventArgs.Empty);
    }
    void StopButton_Click(object Sender, RoutedEventArgs Args)
    {
        StopRequested?.Invoke(this, EventArgs.Empty);
    }
    void RestartButton_Click(object Sender, RoutedEventArgs Args)
    {
        RestartRequested?.Invoke(this, EventArgs.Empty);
    }
    void ClearButton_Click(object Sender, RoutedEventArgs Args)
    {
        fMemoTextBox.Text = LogLine("Service memo cleared.");
        fMemoTextBox.CaretIndex = fMemoTextBox.Text.Length;
    }
    void SetConflictVisualState(int OpenConflictCount)
    {
        fConflictText.Text = OpenConflictCount.ToString();
        fConflictText.Foreground = OpenConflictCount == 0 ? Brushes.Black : Brushes.Firebrick;
        fConflictText.FontWeight = OpenConflictCount == 0 ? FontWeight.SemiBold : FontWeight.Bold;
        fConflictText.FontSize = OpenConflictCount == 0 ? 14 : 15;
        fConflictLabelText.Foreground = OpenConflictCount == 0 ? Brushes.Black : Brushes.Firebrick;
        fConflictLabelText.FontWeight = OpenConflictCount == 0 ? FontWeight.SemiBold : FontWeight.Bold;
        fConflictLabelText.FontSize = OpenConflictCount == 0 ? 14 : 15;
        fConflictLabelText.Opacity = OpenConflictCount == 0 ? 0.72 : 1;
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
        fConflictLabelText = new TextBlock() { Text = "Open conflicts", Opacity = 0.72, TextWrapping = TextWrapping.Wrap };
        fConflictText = new TextBlock() { Text = "-" };
        fCommandText = new TextBlock() { Text = "No service command has been requested.", Opacity = 0.72 };
        fMemoTextBox = new TextBox()
        {
            Text = LogLine("Service page initialized."),
            AcceptsReturn = true,
            IsReadOnly = true,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Monospace"),
            FontSize = 12,
            TextWrapping = TextWrapping.NoWrap,
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(fMemoTextBox, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(fMemoTextBox, ScrollBarVisibility.Auto);
        fRefreshButton = ActionButton("Refresh", true);
        fStartButton = ActionButton("Start", true);
        fStopButton = ActionButton("Stop", false);
        fRestartButton = ActionButton("Restart", false);
        fClearButton = ActionButton("Clear", true);
        fRefreshButton.Click += RefreshButton_Click;
        fStartButton.Click += StartButton_Click;
        fStopButton.Click += StopButton_Click;
        fRestartButton.Click += RestartButton_Click;
        fClearButton.Click += ClearButton_Click;

        Grid Layout = new()
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*"),
            RowSpacing = 18,
            Children =
            {
                CreateInfoPanel(),
                new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        fRefreshButton,
                        fStartButton,
                        fStopButton,
                        fRestartButton,
                        fClearButton,
                    }
                },
                fCommandText,
                fMemoTextBox
            }
        };
        Grid.SetRow(Layout.Children[1], 1);
        Grid.SetRow(Layout.Children[2], 2);
        Grid.SetRow(Layout.Children[3], 3);
        Content = Layout;
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
            fConflictText.Text = "-";
            fConflictText.Foreground = Brushes.Black;
            fConflictText.FontWeight = FontWeight.SemiBold;
            fConflictText.FontSize = 14;
            fConflictLabelText.Foreground = Brushes.Black;
            fConflictLabelText.FontWeight = FontWeight.SemiBold;
            fConflictLabelText.FontSize = 14;
            fConflictLabelText.Opacity = 0.72;
            fStartButton.IsEnabled = true;
            fStopButton.IsEnabled = false;
            fRestartButton.IsEnabled = false;
            return;
        }

        fStatusText.Text = Status.ServiceStatus;
        fProcessIdText.Text = Status.ProcessId.ToString();
        fUptimeText.Text = FormatUptime(Status.UptimeSeconds);
        fIpcText.Text = Status.IpcStatus;
        fVersionText.Text = Status.Version;
        SetConflictVisualState(Status.OpenConflictCount);
        fStartButton.IsEnabled = false;
        fStopButton.IsEnabled = true;
        fRestartButton.IsEnabled = true;
    }
    /// <summary>
    /// Displays the latest service command result.
    /// </summary>
    public void SetCommandResult(LocalServiceControlResult Result)
    {
        if (Result == null)
            return;

        fCommandText.Text = Result.Message;
        AppendMemo((Result.Succeeded ? "OK: " : "ERROR: ") + Result.Message);
    }
    /// <summary>
    /// Appends a service diagnostic message.
    /// </summary>
    public void AppendMemo(string Message)
    {
        if (string.IsNullOrWhiteSpace(Message))
            return;

        string Text = LogLine(Message);

        if (string.IsNullOrWhiteSpace(fMemoTextBox.Text))
            fMemoTextBox.Text = Text;
        else
            fMemoTextBox.Text += Environment.NewLine + Text;

        fMemoTextBox.CaretIndex = fMemoTextBox.Text.Length;
        fMemoTextBox.Focus();
    }

    // ● events

    /// <summary>
    /// Occurs when the user requests a service status refresh.
    /// </summary>
    public event EventHandler RefreshRequested;
    /// <summary>
    /// Occurs when the user requests service start.
    /// </summary>
    public event EventHandler StartRequested;
    /// <summary>
    /// Occurs when the user requests service stop.
    /// </summary>
    public event EventHandler StopRequested;
    /// <summary>
    /// Occurs when the user requests service restart.
    /// </summary>
    public event EventHandler RestartRequested;
}
