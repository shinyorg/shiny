using Shiny.Net;

namespace Sample.MacOS.Pages.Connectivity;

[ShellMap<ConnectivityPage>("connectivity")]
public partial class ConnectivityViewModel(Shiny.Net.IConnectivity connectivity) : ObservableObject, IPageLifecycleAware
{
    IDisposable? sub;

    [ObservableProperty] string access = "Unknown";
    [ObservableProperty] string connectionTypes = "Unknown";

    public void OnAppearing()
    {
        this.sub = connectivity
            .WhenChanged()
            .Subscribe(c => MainThread.BeginInvokeOnMainThread(() =>
            {
                this.Access = c.Access.ToString();
                this.ConnectionTypes = c.ConnectionTypes.ToString();
            }));
    }

    public void OnDisappearing() => this.sub?.Dispose();
}
