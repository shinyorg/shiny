using Sample.Shared.Maui.Services;

namespace Sample.Shared.Maui.Pages.Events;

[ShellMap<EventsPage>("events")]
public partial class EventsViewModel(IEventStore events) : ObservableObject, IPageLifecycleAware
{
    public List<string> Categories { get; } = ["All", "GPS", "Geofence", "MotionActivity", "Notification", "Push", "Job", "HttpTransfer"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCategory))]
    int selectedCategoryIndex;

    [ObservableProperty] string status = string.Empty;

    public ObservableCollection<EventItemViewModel> Items { get; } = new();

    public string SelectedCategory => this.Categories[this.SelectedCategoryIndex];

    EventHandler<EventRecord>? eventHandler;

    public void OnAppearing()
    {
        this.eventHandler = async (_, record) => await this.OnEventAdded(record);
        events.EventAdded += this.eventHandler;
        _ = this.Refresh();
    }

    public void OnDisappearing()
    {
        if (this.eventHandler is not null)
            events.EventAdded -= this.eventHandler;
        this.eventHandler = null;
    }

    partial void OnSelectedCategoryIndexChanged(int value) => _ = this.Refresh();

    [RelayCommand]
    async Task Refresh()
    {
        var category = this.SelectedCategoryIndex == 0 ? null : this.SelectedCategory;
        var records = await events.GetAll(category);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            this.Items.Clear();
            foreach (var r in records)
                this.Items.Add(new EventItemViewModel(r));
            this.Status = $"Loaded {records.Count} event(s)";
        });
    }

    [RelayCommand]
    async Task Clear()
    {
        await events.Clear();
        await this.Refresh();
    }

    async Task OnEventAdded(EventRecord record)
    {
        var category = this.SelectedCategoryIndex == 0 ? null : this.SelectedCategory;
        if (category is not null && !string.Equals(category, record.Category, StringComparison.Ordinal))
            return;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            this.Items.Insert(0, new EventItemViewModel(record));
            this.Status = $"Loaded {this.Items.Count} event(s)";
        });
    }
}

public partial class EventItemViewModel(EventRecord record) : ObservableObject
{
    public long Id => record.Id;
    public string Category => record.Category;
    public string Description => record.Description;
    public string TimestampDisplay => record.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string TitleDisplay => $"{record.Category}  •  {this.TimestampDisplay}";
    public string ValueText => record.Metadata is null ? string.Empty : "metadata";
    public string? Metadata => record.Metadata;
}
