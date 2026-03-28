namespace Sample.Maui.Pages;

[ShellMap<JobsPage>("jobs")]
public partial class JobsViewModel(IJobManager jobManager) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string status = string.Empty;

    public List<JobViewModel> Jobs
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];

    public void OnAppearing() => this.LoadJobs();
    public void OnDisappearing() { }

    void LoadJobs()
    {
        this.Jobs = jobManager
            .GetJobs()
            .Select(job => new JobViewModel
            {
                Identifier = job.Identifier,
                LastRunTime = job.Identifier
            })
            .ToList();
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
        this.LoadJobs();
    }

    [RelayCommand]
    void CancelAll()
    {
        jobManager.CancelAll();
        this.Status = "All jobs cancelled";
        this.LoadJobs();
    }
}

public partial class JobViewModel : ObservableObject
{
    [ObservableProperty] string identifier = string.Empty;
    [ObservableProperty] string lastRunTime = string.Empty;
}
