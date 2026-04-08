using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.Jobs;

[ShellMap<JobsPage>("jobs")]
public partial class JobsViewModel(IJobManager jobManager, IMainThread mainThread) : ObservableObject, IPageLifecycleAware
{
    IDisposable? startedSub;
    IDisposable? finishedSub;

    [ObservableProperty] string status = string.Empty;
    [ObservableProperty] bool isRunning;

    public ObservableCollection<JobItemViewModel> Jobs { get; } = new();
    public ObservableCollection<JobRunEntry> RunLog { get; } = new();

    public void OnAppearing()
    {
        this.LoadJobs();
        this.IsRunning = jobManager.IsRunning;

        this.startedSub = jobManager
            .JobStarted
            .Subscribe(info => mainThread.InvokeOnMainThreadAsync(() =>
            {
                this.IsRunning = true;
                this.Status = $"Started: {info.Identifier}";
                this.RunLog.Insert(0, new JobRunEntry(DateTime.Now, info.Identifier, "Started", null));
                this.Trim();
            }));

        this.finishedSub = jobManager
            .JobFinished
            .Subscribe(result => mainThread.InvokeOnMainThreadAsync(() =>
            {
                this.IsRunning = jobManager.IsRunning;
                var id = result.Job?.Identifier ?? "(unknown)";
                var outcome = result.Success ? "Success" : "Failed";
                this.Status = $"{outcome}: {id}";
                this.RunLog.Insert(0, new JobRunEntry(DateTime.Now, id, outcome, result.Exception?.Message));
                this.Trim();
            }));
    }

    public void OnDisappearing()
    {
        this.startedSub?.Dispose();
        this.finishedSub?.Dispose();
        this.startedSub = null;
        this.finishedSub = null;
        this.RunLog.Clear();
    }

    void LoadJobs()
    {
        this.Jobs.Clear();
        foreach (var job in jobManager.GetJobs())
        {
            this.Jobs.Add(new JobItemViewModel(this)
            {
                Identifier = job.Identifier,
                JobType = job.JobType?.Name ?? "(dynamic)",
                Requirements = FormatRequirements(job),
                IsSystemJob = job.IsSystemJob
            });
        }
    }

    static string FormatRequirements(JobInfo job)
    {
        var parts = new List<string>();
        if (job.RunOnForeground) parts.Add("Foreground");
        if (job.DeviceCharging) parts.Add("Charging");
        if (job.BatteryNotLow) parts.Add("Battery OK");
        if (job.RequiredInternetAccess != InternetAccess.None) parts.Add($"Net: {job.RequiredInternetAccess}");
        return parts.Count == 0 ? "No requirements" : string.Join(" · ", parts);
    }

    [RelayCommand]
    async Task RunAll()
    {
        this.Status = "Running all jobs...";
        var access = await jobManager.RequestAccess();
        if (access != AccessState.Available)
        {
            this.Status = $"Access: {access}";
            return;
        }

        var results = await jobManager.RunAll();
        this.Status = $"Completed {results.Count()} jobs";
    }

    [RelayCommand]
    async Task RunJob(string jobIdentifier)
    {
        if (string.IsNullOrWhiteSpace(jobIdentifier))
            return;

        this.Status = $"Running {jobIdentifier}...";
        var access = await jobManager.RequestAccess();
        if (access != AccessState.Available)
        {
            this.Status = $"Access: {access}";
            return;
        }

        var result = await jobManager.Run(jobIdentifier);
        this.Status = result.Success
            ? $"{jobIdentifier}: success"
            : $"{jobIdentifier}: {result.Exception?.Message}";
    }

    [RelayCommand]
    void CancelJob(string jobIdentifier)
    {
        if (string.IsNullOrWhiteSpace(jobIdentifier))
            return;
        jobManager.Cancel(jobIdentifier);
        this.Status = $"Cancelled {jobIdentifier}";
        this.LoadJobs();
    }

    [RelayCommand]
    void CancelAll()
    {
        jobManager.CancelAll();
        this.Status = "All jobs cancelled";
        this.LoadJobs();
    }

    [RelayCommand]
    void ClearLog() => this.RunLog.Clear();

    void Trim()
    {
        while (this.RunLog.Count > 100)
            this.RunLog.RemoveAt(this.RunLog.Count - 1);
    }
}

public partial class JobItemViewModel : ObservableObject
{
    readonly JobsViewModel parent;
    public JobItemViewModel(JobsViewModel parent) => this.parent = parent;

    [ObservableProperty] string identifier = string.Empty;
    [ObservableProperty] string jobType = string.Empty;
    [ObservableProperty] string requirements = string.Empty;
    [ObservableProperty] bool isSystemJob;

    public IRelayCommand<string> RunCommand => this.parent.RunJobCommand;
    public IRelayCommand<string> CancelCommand => this.parent.CancelJobCommand;
}

public record JobRunEntry(DateTime Timestamp, string Identifier, string Outcome, string? Error);
