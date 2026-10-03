using Shiny.LiveActivities;

namespace Sample.Shared.Maui.Pages.LiveActivities;

/// <summary>
/// Starts, updates and ends a delivery-style live activity. On iOS the Lock Screen / Dynamic Island view is the stock
/// Shiny widget, built into the app by ShinyLiveActivityWidget=true in Sample.Maui.csproj - there is no Xcode project.
/// </summary>
[ShellMap<LiveActivityPage>("liveactivities")]
public partial class LiveActivityViewModel(ILiveActivityManager manager, IDialogs dialogs) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string access = "Unknown";
    [ObservableProperty] string title = "Order on its way";
    [ObservableProperty] string body = "2 stops away";
    [ObservableProperty] string shortStatus = "12 min";
    [ObservableProperty] double progress = 0.25;
    [ObservableProperty] bool useTimer = true;

    public bool IsSupported => manager.IsSupported;

    public List<LiveActivity> Activities
    {
        get;
        set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];


    public async void OnAppearing()
    {
        this.Access = (await manager.GetCurrentAccess()).ToString();
        this.Refresh();
    }

    public void OnDisappearing() { }


    [RelayCommand]
    async Task Start()
    {
        var access = await manager.RequestAccess();
        this.Access = access.ToString();
        if (access != AccessState.Available)
        {
            await dialogs.Alert("Live Activities", $"Live activities are {access}", "OK");
            return;
        }

        try
        {
            await manager.Start(new LiveActivityRequest
            {
                Kind = "delivery",
                Attributes = new Dictionary<string, string> { ["orderId"] = "A-1234" },
                Content = this.BuildContent()
            });
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Live Activities", ex.Message, "OK");
        }
        this.Refresh();
    }


    [RelayCommand]
    async Task UpdateAll()
    {
        var content = this.BuildContent();
        foreach (var activity in manager.GetAll())
            await manager.Update(activity.Id, content);

        this.Refresh();
    }


    [RelayCommand]
    async Task EndAll()
    {
        await manager.EndAll();
        this.Refresh();
    }


    [RelayCommand]
    void Refresh() => this.Activities = manager.GetAll().ToList();


    LiveActivityContent BuildContent() => new()
    {
        Title = this.Title,
        Body = this.Body,
        ShortStatus = this.ShortStatus,
        // a time range animates on its own; a fraction only moves when the app sends an update
        Progress = this.UseTimer
            ? LiveActivityProgress.FromRange(DateTimeOffset.Now, DateTimeOffset.Now.AddMinutes(12))
            : LiveActivityProgress.FromValue(this.Progress)
    };
}
