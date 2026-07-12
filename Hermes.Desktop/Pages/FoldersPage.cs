// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays synchronization folder roots.
/// </summary>
public class FoldersPage : UserControl
{
    // ● fields

    readonly TextBlock fSyncRootIdText;
    readonly TextBlock fProviderText;
    readonly TextBlock fLocalRootText;
    readonly TextBlock fRemoteRootText;
    readonly TextBlock fEnabledText;
    readonly TextBlock fMutationsText;
    readonly TextBlock fPollingText;
    string fLocalRootPath;

    // ● private

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
    static void ConfigureLink(TextBlock TextBlock)
    {
        TextBlock.Foreground = Brushes.DodgerBlue;
        TextBlock.TextDecorations = TextDecorations.Underline;
        TextBlock.Cursor = new Cursor(StandardCursorType.Hand);
    }
    void OpenLocalRootPath()
    {
        if (string.IsNullOrWhiteSpace(fLocalRootPath) || !Directory.Exists(fLocalRootPath))
            return;

        Process.Start(new ProcessStartInfo(fLocalRootPath)
        {
            UseShellExecute = true,
        });
    }
    void LocalRootText_PointerPressed(object Sender, PointerPressedEventArgs Args)
    {
        OpenLocalRootPath();
    }
    Border CreateRootPanel()
    {
        return new Border()
        {
            Padding = new Thickness(16),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gainsboro,
            CornerRadius = new CornerRadius(6),
            Child = new Grid()
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto"),
                ColumnDefinitions = new ColumnDefinitions("180,*"),
                RowSpacing = 10,
                Children =
                {
                    Label("Sync root id", 0, 0),
                    Field(fSyncRootIdText, 0, 1),
                    Label("Provider", 1, 0),
                    Field(fProviderText, 1, 1),
                    Label("Local folder", 2, 0),
                    Field(fLocalRootText, 2, 1),
                    Label("Remote folder", 3, 0),
                    Field(fRemoteRootText, 3, 1),
                    Label("Enabled", 4, 0),
                    Field(fEnabledText, 4, 1),
                    Label("Mutations", 5, 0),
                    Field(fMutationsText, 5, 1),
                    Label("Polling", 6, 0),
                    Field(fPollingText, 6, 1),
                }
            }
        };
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="FoldersPage"/> class.
    /// </summary>
    public FoldersPage()
    {
        fSyncRootIdText = new TextBlock() { Text = "-" };
        fProviderText = new TextBlock() { Text = "-" };
        fLocalRootText = new TextBlock() { Text = "-", TextWrapping = TextWrapping.NoWrap };
        fRemoteRootText = new TextBlock() { Text = "-" };
        fEnabledText = new TextBlock() { Text = "-" };
        fMutationsText = new TextBlock() { Text = "-" };
        fPollingText = new TextBlock() { Text = "-" };
        fLocalRootPath = string.Empty;
        ConfigureLink(fLocalRootText);
        fLocalRootText.PointerPressed += LocalRootText_PointerPressed;

        Content = new StackPanel()
        {
            Spacing = 18,
            Children =
            {
                new TextBlock() { Text = "Synchronization roots", FontSize = 18, FontWeight = FontWeight.SemiBold },
                CreateRootPanel(),
            }
        };
    }

    // ● public

    /// <summary>
    /// Displays the configured synchronization root from service status.
    /// </summary>
    public void SetStatus(LocalServiceStatus Status)
    {
        if (Status == null)
        {
            fSyncRootIdText.Text = "-";
            fProviderText.Text = "-";
            fLocalRootText.Text = "-";
            fLocalRootPath = string.Empty;
            fRemoteRootText.Text = "-";
            fEnabledText.Text = "-";
            fMutationsText.Text = "-";
            fPollingText.Text = "-";
            return;
        }

        fSyncRootIdText.Text = Status.SyncRootId;
        fProviderText.Text = Status.ProviderName;
        fLocalRootText.Text = Status.LocalRootPath;
        fLocalRootPath = Status.LocalRootPath;
        fRemoteRootText.Text = Status.RemoteRootItemId;
        fEnabledText.Text = Status.SyncRootEnabled ? "Yes" : "No";
        fMutationsText.Text = Status.MutationsEnabled ? "Enabled" : "Disabled";
        fPollingText.Text = Status.PollingIntervalSeconds + " seconds";
    }
}
