// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace TestApp;

/// <summary>
/// Main playground window.
/// </summary>
public partial class MainWindow : Window
{
    // ● private

    private void AppendLog(string Text)
    {
        if (!string.IsNullOrWhiteSpace(LogBox.Text))
            LogBox.Text += Environment.NewLine;

        LogBox.Text += $"[{DateTimeOffset.Now:HH:mm:ss}] {Text}";
        LogBox.CaretIndex = LogBox.Text.Length;
    }

    private void SetButtonsEnabled(bool Enabled)
    {
        foreach (Control Control in ButtonPanel.Children)
        {
            if (Control is Button Button)
                Button.IsEnabled = Enabled;
        }
    }

    private async Task RunButtonAsync(string OperationName, Func<Task> Operation)
    {
        DateTimeOffset StartedAt = DateTimeOffset.Now;

        SetButtonsEnabled(false);
        AppendLog($"{OperationName}: started at {StartedAt:yyyy-MM-dd HH:mm:ss}.");

        try
        {
            await Operation();
            AppendLog($"{OperationName}: completed.");
        }
        catch (Exception Ex)
        {
            AppendLog($"{OperationName}: failed.");
            AppendLog($"{Ex.GetType().FullName}: {Ex.Message}");
            AppendLog(Ex.StackTrace ?? "No stack trace.");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private Task ConnectAsync()
    {
        AppendLog("Connect placeholder async method called.");
        return Task.CompletedTask;
    }

    private Task AboutAsync()
    {
        AppendLog("About placeholder async method called.");
        return Task.CompletedTask;
    }

    private Task GetStartPageTokenAsync()
    {
        AppendLog("Get Start Page Token placeholder async method called.");
        return Task.CompletedTask;
    }

    private Task ListFilesAsync()
    {
        AppendLog("List Files placeholder async method called.");
        return Task.CompletedTask;
    }

    private Task ListChangesAsync()
    {
        AppendLog("List Changes placeholder async method called.");
        return Task.CompletedTask;
    }

    private async void ConnectClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Connect", ConnectAsync);
    }

    private async void AboutClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("About", AboutAsync);
    }

    private async void GetStartPageTokenClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Get Start Page Token", GetStartPageTokenAsync);
    }

    private async void ListFilesClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("List Files", ListFilesAsync);
    }

    private async void ListChangesClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("List Changes", ListChangesAsync);
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}
