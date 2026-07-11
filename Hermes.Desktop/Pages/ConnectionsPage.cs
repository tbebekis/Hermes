// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays configured storage provider connection status.
/// </summary>
public class ConnectionsPage : UserControl
{
    // ● fields

    readonly TextBlock fProviderText;
    readonly TextBlock fAuthenticationText;
    readonly TextBlock fRemoteRootText;
    readonly TextBlock fMutationsText;
    readonly TextBlock fServiceText;
    readonly TextBlock fIpcText;

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
    Border CreateConnectionPanel()
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
                    Label("Provider", 0, 0),
                    Field(fProviderText, 0, 1),
                    Label("Authentication", 1, 0),
                    Field(fAuthenticationText, 1, 1),
                    Label("Remote root", 2, 0),
                    Field(fRemoteRootText, 2, 1),
                    Label("Mutations", 3, 0),
                    Field(fMutationsText, 3, 1),
                    Label("Service", 4, 0),
                    Field(fServiceText, 4, 1),
                    Label("IPC", 5, 0),
                    Field(fIpcText, 5, 1),
                }
            }
        };
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionsPage"/> class.
    /// </summary>
    public ConnectionsPage()
    {
        fProviderText = new TextBlock() { Text = "-" };
        fAuthenticationText = new TextBlock() { Text = "-" };
        fRemoteRootText = new TextBlock() { Text = "-" };
        fMutationsText = new TextBlock() { Text = "-" };
        fServiceText = new TextBlock() { Text = "-" };
        fIpcText = new TextBlock() { Text = "-" };

        Content = new StackPanel()
        {
            Spacing = 18,
            Children =
            {
                CreateConnectionPanel(),
            }
        };
    }

    // ● public

    /// <summary>
    /// Displays the latest provider connection status.
    /// </summary>
    public void SetStatus(LocalServiceStatus Status)
    {
        if (Status == null)
        {
            fProviderText.Text = "-";
            fAuthenticationText.Text = "Unknown";
            fRemoteRootText.Text = "-";
            fMutationsText.Text = "-";
            fServiceText.Text = "Stopped";
            fIpcText.Text = "Disconnected";
            return;
        }

        fProviderText.Text = Status.ProviderName;
        fAuthenticationText.Text = "Configured";
        fRemoteRootText.Text = Status.RemoteRootItemId;
        fMutationsText.Text = Status.MutationsEnabled ? "Enabled" : "Disabled";
        fServiceText.Text = Status.ServiceStatus;
        fIpcText.Text = Status.IpcStatus;
    }
}
