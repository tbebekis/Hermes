// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace TestApp;

/// <summary>
/// Main playground window.
/// </summary>
public partial class MainWindow : Window
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    // ● private

    private void AppendLog(string Text)
    {
        LogBox.Text += $"[{DateTimeOffset.Now:HH:mm:ss}] {Text}{Environment.NewLine}";
        LogBox.CaretIndex = LogBox.Text.Length;
    }

    private void ConnectClick(object Sender, RoutedEventArgs Args)
    {
        AppendLog("Connect stub called.");
    }

    private void GetStartPageTokenClick(object Sender, RoutedEventArgs Args)
    {
        AppendLog("Get Start Page Token stub called.");
    }

    private void ListFilesClick(object Sender, RoutedEventArgs Args)
    {
        AppendLog("List Files stub called.");
    }

    private void ListChangesClick(object Sender, RoutedEventArgs Args)
    {
        AppendLog("List Changes stub called.");
    }

    private void UploadTestFileClick(object Sender, RoutedEventArgs Args)
    {
        AppendLog("Upload Test File stub called.");
    }

    private void DownloadTestFileClick(object Sender, RoutedEventArgs Args)
    {
        AppendLog("Download Test File stub called.");
    }

    private void ScanLocalFolderClick(object Sender, RoutedEventArgs Args)
    {
        AppendLog("Scan Local Folder stub called.");
    }
}
