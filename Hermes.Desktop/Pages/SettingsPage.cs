// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays Hermes desktop and synchronization settings.
/// </summary>
public class SettingsPage : UserControl
{
    // ● fields

    readonly TextBlock fServiceEndpointText;
    readonly TextBlock fSyncRootText;
    readonly TextBlock fPollingText;
    readonly TextBlock fMutationsText;
    readonly TextBlock fProviderText;
    readonly TextBlock fLocalRootText;
    readonly TextBlock fRemoteRootText;

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
        TextBlock.TextWrapping = TextWrapping.Wrap;
        Grid.SetRow(TextBlock, Row);
        Grid.SetColumn(TextBlock, Column);
        return TextBlock;
    }
    Border CreateSettingsPanel()
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
                    Label("Service endpoint", 0, 0),
                    Field(fServiceEndpointText, 0, 1),
                    Label("Sync root", 1, 0),
                    Field(fSyncRootText, 1, 1),
                    Label("Polling interval", 2, 0),
                    Field(fPollingText, 2, 1),
                    Label("Mutations", 3, 0),
                    Field(fMutationsText, 3, 1),
                    Label("Provider", 4, 0),
                    Field(fProviderText, 4, 1),
                    Label("Local folder", 5, 0),
                    Field(fLocalRootText, 5, 1),
                    Label("Remote folder", 6, 0),
                    Field(fRemoteRootText, 6, 1),
                }
            }
        };
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPage"/> class.
    /// </summary>
    public SettingsPage()
    {
        fServiceEndpointText = new TextBlock() { Text = "http://127.0.0.1:8765" };
        fSyncRootText = new TextBlock() { Text = "-" };
        fPollingText = new TextBlock() { Text = "-" };
        fMutationsText = new TextBlock() { Text = "-" };
        fProviderText = new TextBlock() { Text = "-" };
        fLocalRootText = new TextBlock() { Text = "-" };
        fRemoteRootText = new TextBlock() { Text = "-" };

        Content = new StackPanel()
        {
            Spacing = 18,
            Children =
            {
                CreateSettingsPanel(),
            }
        };
    }

    // ● public

    /// <summary>
    /// Displays the latest read-only service settings.
    /// </summary>
    public void SetStatus(LocalServiceStatus Status)
    {
        if (Status == null)
        {
            fSyncRootText.Text = "-";
            fPollingText.Text = "-";
            fMutationsText.Text = "-";
            fProviderText.Text = "-";
            fLocalRootText.Text = "-";
            fRemoteRootText.Text = "-";
            return;
        }

        fSyncRootText.Text = Status.SyncRootId;
        fPollingText.Text = Status.PollingIntervalSeconds + " seconds";
        fMutationsText.Text = Status.MutationsEnabled ? "Enabled" : "Disabled";
        fProviderText.Text = Status.ProviderName;
        fLocalRootText.Text = Status.LocalRootPath;
        fRemoteRootText.Text = Status.RemoteRootItemId;
    }
}
