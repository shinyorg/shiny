namespace Sample.Linux.Pages.Device;

[ShellMap<ConnectivityPage>("connectivity")]
public partial class ConnectivityViewModel(Shiny.Net.IConnectivity connectivity) : ObservableObject, IDisposable
{
    IDisposable? sub;

    [ObservableProperty] string access = "—";
    [ObservableProperty] string types = "—";
    [ObservableProperty] string lastChange = "—";


    [RelayCommand]
    void Refresh()
    {
        this.Access = connectivity.Access.ToString();
        this.Types = connectivity.ConnectionTypes.ToString();
    }


    [RelayCommand]
    void Start()
    {
        this.sub?.Dispose();
        this.Refresh();
        this.sub = connectivity
            .WhenChanged()
            .Subscribe(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    this.Refresh();
                    this.LastChange = DateTime.Now.ToString("HH:mm:ss");
                });
            });
    }


    [RelayCommand]
    void Stop()
    {
        this.sub?.Dispose();
        this.sub = null;
    }


    public void Dispose() => this.sub?.Dispose();
}
