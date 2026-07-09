// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace TestApp;

/// <summary>
/// Main playground window.
/// </summary>
public partial class MainWindow : Window
{
    // ● private

    private readonly GoogleDriveClient fClient;
    private string fLastPageToken = string.Empty;

    private void AppendLog(string Text)
    {
        if (!string.IsNullOrWhiteSpace(LogBox.Text))
            LogBox.Text += Environment.NewLine;

        LogBox.Text += $"[{DateTimeOffset.Now:HH:mm:ss}] {Text}";
        LogBox.CaretIndex = LogBox.Text.Length;
    }

    private async Task RunOperationAsync(string OperationName, Func<Task> Operation)
    {
        AppendLog($"{OperationName}: started.");

        try
        {
            await Operation();
            AppendLog($"{OperationName}: succeeded.");
        }
        catch (Exception Ex)
        {
            AppendLog($"{OperationName}: failed.");
            AppendLog($"{Ex.GetType().FullName}: {Ex.Message}");
            AppendLog(Ex.StackTrace ?? "No stack trace.");
        }
    }

    private async void ConnectClick(object Sender, RoutedEventArgs Args)
    {
        await RunOperationAsync("Connect", async () =>
        {
            await fClient.AuthenticateAsync(CancellationToken.None);
            AppendLog("DriveService created.");
        });
    }

    private async void AboutClick(object Sender, RoutedEventArgs Args)
    {
        await RunOperationAsync("About", async () =>
        {
            GoogleDriveAbout About = await fClient.GetAboutAsync(CancellationToken.None);
            AppendLog($"User: {About.DisplayName} <{About.EmailAddress}>");
            AppendLog($"Root folder id: {About.RootFolderId}");
            AppendLog($"Storage usage: {About.StorageUsage}");
            AppendLog($"Storage limit: {About.StorageLimit}");
        });
    }

    private async void GetStartPageTokenClick(object Sender, RoutedEventArgs Args)
    {
        await RunOperationAsync("Get Start Page Token", async () =>
        {
            fLastPageToken = await fClient.GetStartPageTokenAsync(CancellationToken.None);
            AppendLog($"Start page token: {fLastPageToken}");
        });
    }

    private async void ListFilesClick(object Sender, RoutedEventArgs Args)
    {
        await RunOperationAsync("List Files", async () =>
        {
            IReadOnlyList<StorageItem> Items = await fClient.ListFilesAsync(CancellationToken.None);
            AppendLog($"Files returned: {Items.Count}");

            foreach (StorageItem Item in Items)
                AppendLog($"{Item.Kind}: {Item.Name} [{Item.Id}]");
        });
    }

    private async void ListChangesClick(object Sender, RoutedEventArgs Args)
    {
        await RunOperationAsync("List Changes", async () =>
        {
            if (string.IsNullOrWhiteSpace(fLastPageToken))
                fLastPageToken = await fClient.GetStartPageTokenAsync(CancellationToken.None);

            IReadOnlyList<StorageChange> Changes = await fClient.ListChangesAsync(fLastPageToken, CancellationToken.None);
            AppendLog($"Changes returned: {Changes.Count}");

            foreach (StorageChange Change in Changes)
                AppendLog($"{Change.ChangeType}: {Change.Item.Name} [{Change.ChangeId}]");
        });
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        fClient = new GoogleDriveClient(new GoogleDriveAuthManager(), new GoogleDriveMapper());
        InitializeComponent();
    }
}
