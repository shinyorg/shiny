using Shiny;
using Shiny.Hosting;

namespace Sample.tvOS.Infrastructure;


/// <summary>
/// Every page in this sample is the same shape: a row of buttons across the top and a scrolling
/// log underneath. tvOS is a 10-foot UI driven by a remote, so everything is oversized and
/// everything you can interact with has to be focusable.
/// </summary>
public abstract class ModuleViewController : UIViewController
{
    readonly List<string> lines = new();
    readonly UIStackView actions = new()
    {
        Axis = UILayoutConstraintAxis.Horizontal,
        Distribution = UIStackViewDistribution.FillEqually,
        Spacing = 30
    };
    readonly UITableView table = new(CGRect.Empty, UITableViewStyle.Plain);
    readonly UILabel header = new();

    protected ModuleViewController(string subtitle) => this.Subtitle = subtitle;

    protected string Subtitle { get; }

    /// <summary>Resolve a Shiny service out of the running host.</summary>
    protected static T Resolve<T>() where T : notnull
        => (T)Host.Current.Services.GetService(typeof(T))!;


    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        this.View!.BackgroundColor = UIColor.FromRGB(18, 18, 22);

        this.header.Text = this.Subtitle;
        this.header.TextColor = UIColor.FromRGB(150, 200, 255);
        this.header.Font = UIFont.SystemFontOfSize(30)!;
        this.header.Lines = 0;

        this.table.BackgroundColor = UIColor.Clear;
        this.table.RowHeight = UITableView.AutomaticDimension;
        this.table.EstimatedRowHeight = 44;
        this.table.RemembersLastFocusedIndexPath = true;
        this.table.Source = new LogSource(this.lines);

        var root = new UIStackView(new UIView[] { this.header, this.actions, this.table })
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Spacing = 30,
            TranslatesAutoresizingMaskIntoConstraints = false
        };
        root.SetCustomSpacing(20, this.header);
        this.View.AddSubview(root);

        var safe = this.View.SafeAreaLayoutGuide;
        NSLayoutConstraint.ActivateConstraints(
        [
            root.TopAnchor.ConstraintEqualTo(safe.TopAnchor, 20),
            root.LeadingAnchor.ConstraintEqualTo(safe.LeadingAnchor, 60),
            root.TrailingAnchor.ConstraintEqualTo(safe.TrailingAnchor, -60),
            root.BottomAnchor.ConstraintEqualTo(safe.BottomAnchor, -20),
            this.actions.HeightAnchor.ConstraintEqualTo(80)
        ]);

        this.OnReady();
    }


    /// <summary>Called once the view is built - add your actions and kick off any listeners here.</summary>
    protected virtual void OnReady() { }


    protected void AddAction(string title, Func<Task> handler)
    {
        var button = UIButton.GetButton(UIButtonConfiguration.FilledButtonConfiguration, null);
        button.SetTitle(title, UIControlState.Normal);
        button.TitleLabel!.Font = UIFont.SystemFontOfSize(26)!;
        button.PrimaryActionTriggered += async (_, _) =>
        {
            try
            {
                await handler().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                this.Log($"FAILED: {ex.Message}");
            }
        };
        this.actions.AddArrangedSubview(button);
    }


    /// <summary>Appends a line to the on-screen log. Safe to call from any thread.</summary>
    protected void Log(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss}  {message}";

        void Append()
        {
            this.lines.Insert(0, line);
            if (this.lines.Count > 200)
                this.lines.RemoveRange(200, this.lines.Count - 200);

            this.table.ReloadData();
        }

        if (NSThread.Current.IsMainThread)
            Append();
        else
            this.InvokeOnMainThread(Append);
    }


    protected void ClearLog()
    {
        this.lines.Clear();
        this.table.ReloadData();
    }


    class LogSource(List<string> lines) : UITableViewSource
    {
        public override nint RowsInSection(UITableView tableView, nint section) => lines.Count;

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell("log") ?? new UITableViewCell(UITableViewCellStyle.Default, "log");
            cell.BackgroundColor = UIColor.Clear;
            cell.TextLabel!.Text = lines[indexPath.Row];
            cell.TextLabel.Lines = 0;
            cell.TextLabel.Font = UIFont.SystemFontOfSize(24)!;
            cell.TextLabel.TextColor = UIColor.FromRGB(225, 225, 230);
            return cell;
        }
    }
}
