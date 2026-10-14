using Shiny.DeviceTests.Framework;

namespace Shiny.DeviceTests;


/// <summary>
/// Shows the live run (so DevFlow screenshots tell the story) and lets a person start a run by hand. A manual run
/// has no runner attached, so environment-driven tests skip.
/// </summary>
public class MainPage : ContentPage
{
    readonly TestRunner runner;
    readonly Label summary;
    readonly Label current;
    readonly CollectionView results;


    public MainPage(TestRunner runner)
    {
        this.runner = runner;
        this.Title = "Shiny Device Tests";

        this.summary = new Label { AutomationId = "Summary", FontAttributes = FontAttributes.Bold, FontSize = 18 };
        this.current = new Label { AutomationId = "Current", FontSize = 13 };

        var runButton = new Button { Text = "Run All (no runner)", AutomationId = "RunAll" };
        runButton.Clicked += (_, _) =>
        {
            try
            {
                this.runner.Start(new TestRunRequest(null, null));
            }
            catch (InvalidOperationException ex)
            {
                this.current.Text = ex.Message;
            }
        };

        this.results = new CollectionView
        {
            AutomationId = "Results",
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(() =>
            {
                var name = new Label { FontSize = 14 };
                name.SetBinding(Label.TextProperty, static (ResultRow r) => r.Title);
                name.SetBinding(Label.TextColorProperty, static (ResultRow r) => r.Color);

                var message = new Label { FontSize = 11, TextColor = Colors.Gray, MaxLines = 3 };
                message.SetBinding(Label.TextProperty, static (ResultRow r) => r.Message);

                return new VerticalStackLayout { Padding = new Thickness(0, 4), Children = { name, message } };
            })
        };

        this.Content = new Grid
        {
            Padding = new Thickness(16, 48, 16, 16),
            RowSpacing = 8,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            },
            Children =
            {
                this.summary,
                this.current.Row(1),
                runButton.Row(2),
                this.results.Row(3)
            }
        };

        this.runner.StatusChanged += s => this.Dispatcher.Dispatch(() => this.Render(s));
        this.Render(this.runner.Status);
    }


    void Render(TestRunStatus status)
    {
        this.summary.Text = status.RunId == null
            ? "Waiting for a run"
            : $"{(status.Running ? "Running" : "Done")} - {status.Passed} passed, {status.Failed} failed, {status.Skipped} skipped of {status.Total}";

        this.current.Text = status.Current ?? String.Empty;
        this.results.ItemsSource = status
            .Results
            .Select(x => new ResultRow(
                $"{Glyph(x.Outcome)} {x.Suite}.{x.Name} ({x.DurationMs:0}ms)",
                x.Message ?? String.Empty,
                x.Outcome switch
                {
                    TestOutcome.Passed => Colors.Green,
                    TestOutcome.Failed => Colors.Red,
                    _ => Colors.Orange
                }
            ))
            .ToList();
    }


    static string Glyph(TestOutcome outcome) => outcome switch
    {
        TestOutcome.Passed => "PASS",
        TestOutcome.Failed => "FAIL",
        _ => "SKIP"
    };


    public record ResultRow(string Title, string Message, Color Color);
}


static class GridExtensions
{
    public static T Row<T>(this T view, int row) where T : BindableObject
    {
        Grid.SetRow(view, row);
        return view;
    }
}
