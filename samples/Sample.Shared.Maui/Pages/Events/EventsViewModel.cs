using System.Text.Json;
using Sample.Shared.Maui.Services;

namespace Sample.Shared.Maui.Pages.Events;

[ShellMap<EventsPage>("events")]
public partial class EventsViewModel(IEventStore events) : ObservableObject, IPageLifecycleAware
{
    const int PageSize = 50;

    public List<string> Categories { get; } = ["All", "GPS", "Geofence", "MotionActivity", "Notification", "Push", "Job", "HttpTransfer"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCategory))]
    int selectedCategoryIndex;

    [ObservableProperty] string status = string.Empty;
    [ObservableProperty] bool hasMore;

    [ObservableProperty] bool isMetadataPanelOpen;
    [ObservableProperty] string selectedTitle = string.Empty;
    [ObservableProperty] string selectedMetadata = string.Empty;

    public List<EventItemViewModel> Items
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = [];

    public string SelectedCategory => this.Categories[this.SelectedCategoryIndex];

    EventHandler<EventRecord>? eventHandler;
    bool loading;

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
        if (this.loading) return;
        this.loading = true;
        try
        {
            var category = this.SelectedCategoryIndex == 0 ? null : this.SelectedCategory;
            var records = await events.GetAll(category, beforeId: null, limit: PageSize);
            var items = records.Select(r => new EventItemViewModel(r)).ToList();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                this.Items = items;
                this.HasMore = records.Count >= PageSize;
                this.Status = $"Loaded {items.Count} event(s)";
            });
        }
        finally
        {
            this.loading = false;
        }
    }

    [RelayCommand]
    async Task LoadMore()
    {
        if (this.loading || !this.HasMore) return;
        var cursor = this.Items.Count == 0 ? (long?)null : this.Items[^1].Id;
        if (cursor is null) return;

        this.loading = true;
        try
        {
            var category = this.SelectedCategoryIndex == 0 ? null : this.SelectedCategory;
            var records = await events.GetAll(category, beforeId: cursor, limit: PageSize);
            if (records.Count == 0)
            {
                await MainThread.InvokeOnMainThreadAsync(() => this.HasMore = false);
                return;
            }

            var next = new List<EventItemViewModel>(this.Items.Count + records.Count);
            next.AddRange(this.Items);
            foreach (var r in records)
                next.Add(new EventItemViewModel(r));

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                this.Items = next;
                this.HasMore = records.Count >= PageSize;
                this.Status = $"Loaded {next.Count} event(s)";
            });
        }
        finally
        {
            this.loading = false;
        }
    }

    [RelayCommand]
    async Task Clear()
    {
        await events.Clear();
        await this.Refresh();
    }

    [RelayCommand]
    void ShowMetadata(EventItemViewModel? item)
    {
        if (item is null)
            return;

        this.SelectedTitle = item.TitleDisplay;
        this.SelectedMetadata = FormatMetadata(item.Metadata);
        this.IsMetadataPanelOpen = true;
    }

    static string FormatMetadata(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "(no metadata)";
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return raw;
        }
    }

    async Task OnEventAdded(EventRecord record)
    {
        var category = this.SelectedCategoryIndex == 0 ? null : this.SelectedCategory;
        if (category is not null && !string.Equals(category, record.Category, StringComparison.Ordinal))
            return;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            this.Items = [new EventItemViewModel(record), .. this.Items];
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
