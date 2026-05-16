namespace Sample.Shared.Maui.Pages.Jobs;

[ShellMap<JobsPage>("jobs")]
public partial class JobsViewModel(IJobManager jobManager) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string status = string.Empty;

    public ObservableCollection<JobItemViewModel> Jobs { get; } = new();
    public ObservableCollection<JobRunEntry> RunLog { get; } = new();

    public void OnAppearing() => this.LoadJobs();
    public void OnDisappearing() => this.RunLog.Clear();

    void LoadJobs()
    {
        this.Jobs.Clear();
        foreach (var (type, reg) in jobManager.GetJobs())
        {
            this.Jobs.Add(new JobItemViewModel(this)
            {
                Identifier = reg.Identifier,
                JobType = type.Name,
                Requirements = FormatRequirements(reg)
            });
        }
    }

    static string FormatRequirements(JobRegistration reg)
    {
        var parts = new List<string>();
        if (reg.RunOnForeground) parts.Add("Foreground");
        if (reg.DeviceCharging) parts.Add("Charging");
        if (reg.BatteryNotLow) parts.Add("Battery OK");
        if (reg.RequiredInternetAccess != InternetAccess.None) parts.Add($"Net: {reg.RequiredInternetAccess}");
        return parts.Count == 0 ? "No requirements" : string.Join(" · ", parts);
    }

    [RelayCommand]
    async Task RunAll()
    {
        this.Status = "Running all jobs...";
        var results = await jobManager.RunAll();
        var list = results.ToList();
        var failed = list.Count(x => !x.Success);
        this.Status = failed == 0
            ? $"Completed {list.Count} job(s)"
            : $"Completed {list.Count} job(s), {failed} failed";

        foreach (var result in list)
        {
            var id = result.Job?.Identifier ?? "(unknown)";
            var outcome = result.Success ? "Success" : "Failed";
            this.RunLog.Insert(0, new JobRunEntry(DateTime.Now, id, outcome, result.Exception?.Message));
        }
        Trim();
    }

    [RelayCommand]
    async Task RunJob(string jobIdentifier)
    {
        if (string.IsNullOrWhiteSpace(jobIdentifier))
            return;

        this.Status = $"Running {jobIdentifier}...";
        try
        {
            await jobManager.RunJobAsTask(jobIdentifier);
            this.Status = $"{jobIdentifier}: success";
            this.RunLog.Insert(0, new JobRunEntry(DateTime.Now, jobIdentifier, "Success", null));
        }
        catch (Exception ex)
        {
            this.Status = $"{jobIdentifier}: {ex.Message}";
            this.RunLog.Insert(0, new JobRunEntry(DateTime.Now, jobIdentifier, "Failed", ex.Message));
        }
        Trim();
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

    public IRelayCommand<string> RunCommand => this.parent.RunJobCommand;
}

public record JobRunEntry(DateTime Timestamp, string Identifier, string Outcome, string? Error);
