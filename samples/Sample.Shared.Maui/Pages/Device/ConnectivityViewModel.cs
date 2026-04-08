using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.Device;

[ShellMap<ConnectivityPage>("connectivity")]
public partial class ConnectivityViewModel(
    Shiny.Net.IConnectivity connectivity,
    IMainThread mainThread
) : ObservableObject, IPageLifecycleAware
{
    IDisposable? sub;

    [ObservableProperty] string access = "Unknown";
    [ObservableProperty] string connectionTypes = "Unknown";

    public ObservableCollection<ConnectivityHistoryEntry> History { get; } = new();

    public void OnAppearing()
    {
        // Seed with the current state so the page isn't blank until the first change
        this.Access = connectivity.Access.ToString();
        this.ConnectionTypes = connectivity.ConnectionTypes.ToString();
        this.AddHistory(this.Access, this.ConnectionTypes);

        this.sub = connectivity
            .WhenChanged()
            .Subscribe(c => mainThread.InvokeOnMainThreadAsync(() =>
            {
                var access = c.Access.ToString();
                var types = c.ConnectionTypes.ToString();
                this.Access = access;
                this.ConnectionTypes = types;
                this.AddHistory(access, types);
            }));
    }

    public void OnDisappearing()
    {
        this.sub?.Dispose();
        this.sub = null;
        this.History.Clear();
    }

    [RelayCommand]
    void ClearHistory() => this.History.Clear();

    void AddHistory(string access, string connectionTypes)
    {
        this.History.Insert(0, new ConnectivityHistoryEntry(DateTime.Now, access, connectionTypes));
        while (this.History.Count > 100)
            this.History.RemoveAt(this.History.Count - 1);
    }
}

public record ConnectivityHistoryEntry(DateTime Timestamp, string Access, string ConnectionTypes);
