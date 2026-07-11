// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Displays application information.
/// </summary>
public class AboutPage : UserControl
{
    // ● private

    static TextBlock Label(string Text, int Row, int Column)
    {
        TextBlock Result = new() { Text = Text, Opacity = 0.72 };
        Grid.SetRow(Result, Row);
        Grid.SetColumn(Result, Column);
        return Result;
    }
    static TextBlock Field(string Text, int Row, int Column)
    {
        TextBlock Result = new()
        {
            Text = Text,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetRow(Result, Row);
        Grid.SetColumn(Result, Column);
        return Result;
    }
    static string AssemblyVersion()
    {
        return typeof(Program).Assembly.GetName().Version?.ToString() ?? string.Empty;
    }
    static string InformationalVersion()
    {
        AssemblyInformationalVersionAttribute Attribute = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return Attribute?.InformationalVersion ?? AssemblyVersion();
    }
    static Image CreateLogo(double Size)
    {
        Uri Uri = new("avares://Hermes.Desktop/Resources/Images/Hermes_Coin.jpg");
        using Stream Stream = AssetLoader.Open(Uri);

        return new Image()
        {
            Source = new Bitmap(Stream),
            Width = Size,
            Height = Size,
            Stretch = Stretch.UniformToFill,
        };
    }
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
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto"),
                ColumnDefinitions = new ColumnDefinitions("160,*"),
                RowSpacing = 10,
                Children =
                {
                    Label("Version", 0, 0),
                    Field(AssemblyVersion(), 0, 1),
                    Label("Build", 1, 0),
                    Field(InformationalVersion(), 1, 1),
                    Label("Runtime", 2, 0),
                    Field(RuntimeInformation.FrameworkDescription, 2, 1),
                    Label("Operating system", 3, 0),
                    Field(RuntimeInformation.OSDescription, 3, 1),
                    Label("Architecture", 4, 0),
                    Field(RuntimeInformation.ProcessArchitecture.ToString(), 4, 1),
                    Label("License", 5, 0),
                    Field("MIT License", 5, 1),
                }
            }
        };
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutPage"/> class.
    /// </summary>
    public AboutPage()
    {
        Content = new StackPanel()
        {
            Spacing = 8,
            Children =
            {
                new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    Children =
                    {
                        CreateLogo(56),
                        new TextBlock()
                        {
                            Text = "Hermes",
                            FontSize = 26,
                            FontWeight = FontWeight.SemiBold,
                            VerticalAlignment = VerticalAlignment.Center,
                        },
                    }
                },
                new TextBlock() { Text = "Linux Google Drive synchronization service control center.", Opacity = 0.72 },
                CreateInfoPanel(),
            }
        };
    }
}
