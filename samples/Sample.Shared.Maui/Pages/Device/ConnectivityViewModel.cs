using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.Device;

[ShellMap<ConnectivityPage>("connectivity")]
public partial class ConnectivityViewModel(
    Shiny.Net.IConnectivity connectivity,
    IMainThread mainThread
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string access = "Unknown";
    [ObservableProperty] string connectionTypes = "Unknown";

    public ObservableCollection<ConnectivityHistoryEntry> History { get; } = new();

    public void OnAppearing()
    {
        this.Access = connectivity.Access.ToString();
        this.ConnectionTypes = connectivity.ConnectionTypes.ToString();
        this.AddHistory(this.Access, this.ConnectionTypes);

        connectivity.Changed += this.OnChanged;
    }

    public void OnDisappearing()
    {
        connectivity.Changed -= this.OnChanged;
        this.History.Clear();
    }

    void OnChanged(object? sender, EventArgs e)
    {
        mainThread.InvokeOnMainThreadAsync(() =>
        {
            var access = connectivity.Access.ToString();
            var types = connectivity.ConnectionTypes.ToString();
            this.Access = access;
            this.ConnectionTypes = types;
            this.AddHistory(access, types);
        });
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
