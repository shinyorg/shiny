using Shiny.CallDirectory;

namespace Sample.CallDirectory;


public class CallDirectoryViewController : UIViewController
{
    // 555-01xx are the North American numbers reserved for fiction
    static readonly CallDirectoryEntry[] Seed =
    [
        new(18005550100, "Shiny Support"),
        new(18005550101, null, CallDirectoryEntryType.Block),
        new(18005550102, "Shiny Sales", CallDirectoryEntryType.Allow),
        new(18005550103, null, CallDirectoryEntryType.Block)
    ];

    readonly ICallDirectoryManager directory = Shiny.Hosting.Host.GetService<ICallDirectoryManager>()!;
    readonly UITextView output = new() { Editable = false, Font = UIFont.GetMonospacedSystemFont(11, UIFontWeight.Regular) };


    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        this.Title = "Call Directory";
        this.View!.BackgroundColor = UIColor.SystemBackground;

        var buttons = new UIStackView([
            Button("Status", this.Status),
            Button("Seed", () => this.Run("seed", async () =>
            {
                await this.directory.ReplaceAll(Seed);
                return await this.Reload();
            })),
            Button("Edit", () => this.Run("edit", async () =>
            {
                await this.directory.Set([new(18005550100, "Shiny Support (renamed)"), new(18005550104, "New Number"), new(18005550101, "Unblocked")]);
                await this.directory.Remove([18005550103, 19999999999]);
                return await this.Reload();
            })),
            Button("Clear", () => this.Run("clear", async () =>
            {
                await this.directory.Clear();
                return await this.Reload();
            })),
            Button("Settings", () => this.Run("settings", async () => (await this.directory.RequestAccess()).ToString()))
        ])
        {
            Axis = UILayoutConstraintAxis.Horizontal,
            Distribution = UIStackViewDistribution.FillEqually,
            Spacing = 4,
            TranslatesAutoresizingMaskIntoConstraints = false
        };
        this.output.TranslatesAutoresizingMaskIntoConstraints = false;
        this.View.AddSubviews(buttons, this.output);

        var guide = this.View.SafeAreaLayoutGuide;
        NSLayoutConstraint.ActivateConstraints([
            buttons.TopAnchor.ConstraintEqualTo(guide.TopAnchor, 8),
            buttons.LeadingAnchor.ConstraintEqualTo(guide.LeadingAnchor, 8),
            buttons.TrailingAnchor.ConstraintEqualTo(guide.TrailingAnchor, -8),
            this.output.TopAnchor.ConstraintEqualTo(buttons.BottomAnchor, 8),
            this.output.LeadingAnchor.ConstraintEqualTo(guide.LeadingAnchor, 8),
            this.output.TrailingAnchor.ConstraintEqualTo(guide.TrailingAnchor, -8),
            this.output.BottomAnchor.ConstraintEqualTo(guide.BottomAnchor)
        ]);

        AppLog.Written += line => this.InvokeOnMainThread(() => this.output.Text = line + "\n" + this.output.Text);

        // simctl launch <udid> org.shiny.sample.calldirectory --selftest runs the whole sequence with no taps
        if (NSProcessInfo.ProcessInfo.Arguments.Contains("--selftest"))
            _ = this.SelfTest();
        else
            this.Status();
    }


    async Task SelfTest()
    {
        await this.RunAsync("status", async () => $"{await this.directory.GetCurrentAccess()} - {await this.directory.Count()} stored");
        await this.RunAsync("seed", async () => { await this.directory.ReplaceAll(Seed); return await this.Reload(); });
        await this.RunAsync("edit", async () =>
        {
            await this.directory.Set([new(18005550100, "Shiny Support (renamed)"), new(18005550104, "New Number"), new(18005550101, "Unblocked")]);
            await this.directory.Remove([18005550103, 19999999999]);
            return await this.Reload();
        });
        await this.RunAsync("reload-noop", this.Reload);
        AppLog.Write("SELFTEST DONE");
    }


    async Task<string> Reload()
    {
        var result = await this.directory.Reload();
        return $"reload={result}, stored={await this.directory.Count()}";
    }


    void Status() => this.Run("status", async () =>
    {
        var path = (this.directory as CallDirectoryManager)?.DatabasePath;
        return $"{await this.directory.GetCurrentAccess()} - {await this.directory.Count()} stored at {path}";
    });


    void Run(string name, Func<Task<string>> work) => _ = this.RunAsync(name, work);

    async Task RunAsync(string name, Func<Task<string>> work)
    {
        try
        {
            AppLog.Write($"{name}: {await work()}");
        }
        catch (Exception ex)
        {
            AppLog.Write($"{name} FAILED: {ex}");
        }
    }


    static UIButton Button(string title, Action action)
    {
        var button = new UIButton(UIButtonType.System);
        button.SetTitle(title, UIControlState.Normal);
        button.TouchUpInside += (_, _) => action();
        return button;
    }
}
