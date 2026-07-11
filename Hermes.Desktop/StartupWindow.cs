// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Startup window used while the application initializes.
/// </summary>
public class StartupWindow : Window
{
    // ● fields

    readonly TextBlock fStatusText;
    readonly ProgressBar fProgressBar;
    readonly List<TextBlock> fStepBlocks;

    // ● private

    static Bitmap LoadImage()
    {
        Uri Uri = new("avares://Hermes.Desktop/Resources/Images/Hermes_Coin.jpg");

        using Stream Stream = AssetLoader.Open(Uri);
        return new Bitmap(Stream);
    }
    static TextBlock CreateStep(string Text)
    {
        return new TextBlock()
        {
            Text = "- " + Text,
            Opacity = 0.58,
            FontSize = 14,
        };
    }
    Grid CreateLayout()
    {
        Image Logo = new()
        {
            Source = LoadImage(),
            Width = 84,
            Height = 84,
            Stretch = Stretch.UniformToFill,
        };
        TextBlock TitleText = new()
        {
            Text = "Hermes",
            FontSize = 32,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        StackPanel Header = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 18,
            Children = { Logo, TitleText },
        };
        fStepBlocks.Add(CreateStep("Loading settings"));
        fStepBlocks.Add(CreateStep("Opening metadata store"));
        fStepBlocks.Add(CreateStep("Loading synchronization jobs"));
        fStepBlocks.Add(CreateStep("Connecting to service"));
        fStepBlocks.Add(CreateStep("Ready"));

        StackPanel Steps = new()
        {
            Spacing = 6,
        };

        foreach (TextBlock Step in fStepBlocks)
            Steps.Children.Add(Step);

        Grid Result = new()
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
            Margin = new Thickness(40),
            RowSpacing = 24,
            Children =
            {
                Header,
                fStatusText,
                Steps,
                fProgressBar,
            }
        };

        Grid.SetRow(fStatusText, 1);
        Grid.SetRow(Steps, 2);
        Grid.SetRow(fProgressBar, 3);

        return Result;
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupWindow"/> class.
    /// </summary>
    public StartupWindow()
    {
        Title = "Hermes";
        Width = 560;
        Height = 420;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        fStatusText = new TextBlock()
        {
            Text = "Initializing...",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
        };
        fProgressBar = new ProgressBar()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 18,
        };
        fStepBlocks = new List<TextBlock>();
        Content = CreateLayout();
    }

    // ● public

    /// <summary>
    /// Displays initialization progress.
    /// </summary>
    public void SetProgress(string Status, int StepIndex, double Percent)
    {
        fStatusText.Text = Status;
        fProgressBar.Value = Percent;

        for (int Index = 0; Index < fStepBlocks.Count; Index++)
        {
            TextBlock Step = fStepBlocks[Index];
            string Text = Step.Text.TrimStart('*', '-', ' ');

            if (Index < StepIndex)
            {
                Step.Text = "* " + Text;
                Step.Opacity = 1;
            }
            else if (Index == StepIndex)
            {
                Step.Text = "- " + Text;
                Step.Opacity = 1;
            }
            else
            {
                Step.Text = "- " + Text;
                Step.Opacity = 0.58;
            }
        }
    }
}
