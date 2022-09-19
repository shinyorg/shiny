using Shiny.Jobs;
using Shiny;

namespace Sample.Jobs;


public class ListViewModel : ViewModel
{
    readonly IJobManager jobManager;


    public ListViewModel(BaseServices services, IJobManager jobManager) : base(services)
    {
        this.jobManager = jobManager;

        this.Create = this.Navigation.Command("CreatePage");

        this.LoadJobs = ReactiveCommand.CreateFromTask(async () =>
            this.Jobs = (await jobManager.GetJobs()).ToList()
        );

        this.RunAllJobs = new Command(async () =>
        {
            if (!await this.AssertJobs())
                return;

            if (this.jobManager.IsRunning)
            {
                await this.Dialogs.DisplayAlertAsync("ERROR", "Job Manager is already running", "OK");
            }
            else
            {
                await this.jobManager.RunAll();
                this.RunningText = "Job Batch Started";
            }
        });

        this.CancelAllJobs = new Command(async _ =>
        {
            if (!await this.AssertJobs())
                return;

            var confirm = await this.Confirm("Are you sure you wish to cancel all jobs?");
            if (confirm)
            {
                await this.jobManager.CancelAll();
                this.LoadJobs.Execute(null);
            }
        });
    }

    public ICommand LoadJobs { get; }
    public ICommand CancelAllJobs { get; }
    public ICommand RunAllJobs { get; }
    public ICommand Create { get; }
    public List<JobInfo> Jobs { get; private set; }

    [Reactive] public string RunningText { get; private set; }
    [Reactive] public JobInfo? SelectedJob { get; set; }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.LoadJobs.Execute(null);

        this.jobManager
            .JobStarted
            .SubOnMainThread(x =>
            {
                this.RunningText = $"{x.Identifier} Running";
                this.LoadJobs.Execute(null);
            })
            .DisposedBy(this.DestroyWith);

        this.jobManager
            .JobFinished
            .Subscribe(x =>
            {
                var status = x.Success ? "Completed" : "Failed";
                this.RunningText = $"{x.Job?.Identifier} {status}";
                this.LoadJobs.Execute(null);
            })
            .DisposedBy(this.DestroyWith);

        this.WhenAnyProperty(x => x.SelectedJob)
            .Where(x => x != null)
            .SubscribeAsync(async x =>
            {
                this.SelectedJob = null;
                await this.jobManager.Run(x.Identifier);
            })
            .DisposedBy(this.DestroyWith);

        return base.InitializeAsync(parameters);
    }



    async Task<bool> AssertJobs()
    {
        var jobs = await this.jobManager.GetJobs();
        if (!jobs.Any())
        {
            await this.Alert("There are no jobs");
            return false;
        }

        return true;
    }
}
