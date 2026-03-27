namespace Sample.Maui.Pages;

[ShellMap<JobsPage>("jobs")]
public partial class JobsViewModel(IJobManager jobManager) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string status = string.Empty;
    public ObservableCollection<JobViewModel> Jobs { get; } = [];

    public void OnAppearing() => LoadJobs();
    public void OnDisappearing() { }

    void LoadJobs()
    {
        Jobs.Clear();
        foreach (var job in jobManager.GetJobs())
        {
            Jobs.Add(new JobViewModel
            {
                Identifier = job.Identifier,
                LastRunTime = job.Identifier
            });
        }
    }

    [RelayCommand]
    async Task RunAll()
    {
        Status = "Running all jobs...";
        var access = await jobManager.RequestAccess();
        if (access != AccessState.Available)
        {
            Status = $"Access: {access}";
            return;
        }

        var results = await jobManager.RunAll();
        Status = $"Completed {results.Count()} jobs";
        LoadJobs();
    }

    [RelayCommand]
    void CancelAll()
    {
        jobManager.CancelAll();
        Status = "All jobs cancelled";
        LoadJobs();
    }
}

public partial class JobViewModel : ObservableObject
{
    [ObservableProperty] string identifier = string.Empty;
    [ObservableProperty] string lastRunTime = string.Empty;
}
